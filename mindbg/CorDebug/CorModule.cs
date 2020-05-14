using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Samples.Debugging.CorSymbolStore;
using MinDbg.NativeApi;

namespace MinDbg.CorDebug
{
    /// <summary>
    /// Represents ICorDebugModule interface.
    /// </summary>
    public sealed class CorModule : WrapperBase
    {
        private readonly ICorDebugModule comodule;
        private ISymbolReader symbolReader;
        private bool isSymbolReaderInitialized;
        
        private static ISymbolBinder1 symbolBinder;

        /// <summary>
        /// Creates new instance of the CorModule class.
        /// </summary>
        /// <param name="comodule">ICorDebugModule instance</param>
        internal CorModule(ICorDebugModule comodule, CorDebuggerOptions options)
            : base(comodule, options)
        {
            this.comodule = comodule;
        }

        /// <summary>
        /// Gets the symbol reader for a given module.
        /// </summary>
        /// <returns>A symbol reader for a given module.</returns>
        public ISymbolReader GetSymbolReader()
        {
            if (!isSymbolReaderInitialized)
            {
                isSymbolReaderInitialized = true;
                symbolReader = (GetSymbolBinder() as ISymbolBinder2).GetReaderForFile(
                                        GetMetadataInterface<IMetadataImport>(),
                                        GetName(),
                                        options.SymbolPath);
            }
            return symbolReader;
        }

        private static ISymbolBinder1 GetSymbolBinder()
        {
            if (symbolBinder == null)
            {
                symbolBinder = new SymbolBinder();
                Debug.Assert(symbolBinder != null);
            }
            return symbolBinder;
        }

        /// <summary>
        /// Gets the process that has the module loaded.
        /// </summary>
        /// <returns>The process that hosts the module.</returns>
        public CorProcess GetProcess()
        {
            comodule.GetProcess(out var coproc);
            return CorProcess.GetOrCreateCorProcess(coproc, options);
        }

        /// <summary>
        /// Gets the function from token.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        public CorFunction GetFunctionFromToken(Int32 token)
        {
            comodule.GetFunctionFromToken((UInt32)token, out var cofunc);
            return new CorFunction(cofunc, options);
        }

        /// <summary>
        /// Gets the module's name.
        /// </summary>
        /// <returns></returns>
        public String GetName()
        {
            Char[] name = new Char[300];
            comodule.GetName((UInt32)name.Length, out var fetched, name);

            // fetched - 1 because of the ending 0
            return new String(name, 0, (Int32)fetched - 1);
        }

        /// <summary>
        /// Returns a requested metadata interface instance.
        /// </summary>
        /// <typeparam name="T">Metadata interface</typeparam>
        /// <returns>Metadata interface instance</returns>
        public T GetMetadataInterface<T>()
        {
            Guid guid = typeof(T).GUID;
            comodule.GetMetaDataInterface(ref guid, out var res);
            return (T)res;
        }

        // Brilliantly written taken from mdbg source code.
        // returns a type token from name
        // when the function fails, we return token TokenNotFound value.
        public int GetTypeTokenFromName(string name)
        {
            IMetadataImport importer = GetMetadataInterface<IMetadataImport>();

            int token = CorConstants.TokenNotFound;
            if (name.Length == 0)
            {
                // this is special global type (we'll return token 0)
                token = CorConstants.TokenGlobalNamespace;
            }
            else
            {
                try
                {
                    importer.FindTypeDefByName(name, 0, out token);
                }
                catch (COMException e)
                {
                    token = CorConstants.TokenNotFound;
                    if ((HResult)e.ErrorCode == HResult.CLDB_E_RECORD_NOTFOUND)
                    {
                        int i = name.LastIndexOf('.');
                        if (i > 0)
                        {
                            int parentToken = GetTypeTokenFromName(name.Substring(0, i));
                            if (parentToken != CorConstants.TokenNotFound)
                            {
                                try
                                {
                                    importer.FindTypeDefByName(name.Substring(i + 1), parentToken, out token);
                                }
                                catch (COMException e2)
                                {
                                    token = CorConstants.TokenNotFound;
                                    if ((HResult)e2.ErrorCode != HResult.CLDB_E_RECORD_NOTFOUND)
                                        throw;
                                }
                            }
                        }
                    }
                    else
                        throw;
                }
            }
            return token;
        }

        // TypeDef are Types that are 'Defined' in the current Module
        public IEnumerable<Tuple<uint, string>> TypeDefs()
        {
            IMetadataImport importer = GetMetadataInterface<IMetadataImport>();
            IntPtr enumPtr = IntPtr.Zero;
            var typeDefs = new List<Tuple<uint, string>>();
            try
            {
                while (true)
                {
                    // TODO fix this so that we fetch more than one at a time
                    importer.EnumTypeDefs(ref enumPtr, out uint typeDef, 1, out uint fetched);
                    if (fetched == 0)
                        break;

                    comodule.GetClassFromToken(typeDef, out ICorDebugClass _);
                    var nameBuilder = new StringBuilder(capacity: 256);
                    GetMetaDataTypeDefName(importer, (int)typeDef, nameBuilder);
                    typeDefs.Add(Tuple.Create(typeDef, nameBuilder.ToString()));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                if (enumPtr != IntPtr.Zero)
                    importer.CloseEnum(enumPtr);
            }

            return typeDefs;
        }

        public Tuple<uint, string> GetTypeDef(uint typeDef)
        {
            // TODO when caching is implemented, use that instead of calling TypeDefs()!!
            return TypeDefs().FirstOrDefault(t => t.Item1 == typeDef);
        }

        // TypeRef Are Type 'References' that point to a TypeDef that is defined in another module
        public IEnumerable<Tuple<uint, string>> TypeRefs()
        {
            IMetadataImport importer = GetMetadataInterface<IMetadataImport>();
            IntPtr enumPtr = IntPtr.Zero;
            var typeRefs = new List<Tuple<uint, string>>();
            try
            {
                while (true)
                {
                    // TODO fix this so that we fetch more than one at a time
                    importer.EnumTypeRefs(ref enumPtr, out uint typeRef, 1, out uint fetched);
                    if (fetched == 0)
                        break;

                    var nameBuilder = new StringBuilder(capacity: 256);
                    importer.GetTypeRefProps((int)typeRef, out int ptkResolutionScope, nameBuilder, 256, out int nameLength);
                    typeRefs.Add(Tuple.Create(typeRef, nameBuilder.ToString()));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                if (enumPtr != IntPtr.Zero)
                    importer.CloseEnum(enumPtr);
            }

            return typeRefs;
        }

        public Tuple<uint, string> GetTypeRef(uint typeDef)
        {
            // TODO when caching is implemented, use that instead of calling TypeRefs()!!
            return TypeRefs().FirstOrDefault(t => t.Item1 == typeDef);
        }

        // Copied from the PerfView source code https://github.com/microsoft/perfview/blob/a3f805dca8a3bb3fce2bb0c7a7524615e78bf525/src/HeapDump/GCHeapDumper.cs#L3320-L3339
        /// <summary>
        /// This version does not give type parameters for a generic type.  It also has the '`\d* suffix for generic types.  
        /// </summary>
        private static string GetMetaDataTypeDefName(IMetadataImport metaData, int typeToken, StringBuilder buffer)
        {
            metaData.GetTypeDefProps(typeToken, buffer, buffer.Capacity, out var typeNameLen, out var typeAttr, out var extendsToken);
            string className = buffer.ToString();

            if ((typeAttr & TypeAttributes.VisibilityMask) >= TypeAttributes.NestedPublic)
            {
                metaData.GetNestedClassProps(typeToken, out var enclosingClassToken);
                string enclosingClassName = GetMetaDataTypeDefName(metaData, enclosingClassToken, buffer);
                className = enclosingClassName + "." + className;
            }
            return className;
        }
    }
}
