using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using MinDbg.CorMetadata;
using MinDbg.NativeApi;

namespace MinDbg.CorDebug
{
    /// <summary>
    /// ICorDebugProcess wrapper.
    /// </summary>
    public sealed class CorProcess : CorController
    {
        private static readonly Dictionary<ICorDebugProcess, CorProcess> processes = new Dictionary<ICorDebugProcess, CorProcess>();

        private HashSet<CorModule> modules = new HashSet<CorModule>();
        
        /// <summary>
        /// Creates a new ICorDebugProcess wrapper.
        /// </summary>
        /// <param name="coprocess">COM process object</param>
        /// <param name="options">The options.</param>
        private CorProcess(ICorDebugProcess coprocess, CorDebuggerOptions options)
            : base(coprocess, options)
        {
        }

        /// <summary>
        /// Gets the process.
        /// </summary>
        /// <value>The process.</value>
        private ICorDebugProcess ComProcess => (ICorDebugProcess)this.cocntrl;

        /// <summary>
        /// Returns an enumerator for all modules
        /// loaded for a given process.
        /// </summary>
        public IEnumerable<CorModule> Modules => modules;

        /* *** Events logic *** */

        public override void Continue(bool outOfBand)
        {
            // FIXME  create stop reset event so we will wait until all callbacks are initialized

            base.Continue(outOfBand);
        }

        /// <summary>
        /// Handler for CorBreakpoint event.
        /// </summary>
        public delegate void CorBreakpointEventHandler(CorBreakpointEventArgs ev);

        /// <summary>
        /// Occurs when breakpoint is hit.
        /// </summary>
        public event CorBreakpointEventHandler OnBreakpoint;

        internal void DispatchEvent(CorBreakpointEventArgs ev)
        {
            // stops executing by default (further handlers may change this)
            ev.Continue = false;

            // calls external handlers
            OnBreakpoint?.Invoke(ev);
        }

        /// <summary>
        /// Handler for CorModuleLoad event.
        /// </summary>
        /// <param name="ev">Event args</param>
        public delegate void CorModuleLoadEventHandler(CorModuleLoadEventArgs ev);

        /// <summary>
        /// Occurs when module is loaded
        /// </summary>
        public event CorModuleLoadEventHandler OnModuleLoad;

        internal void DispatchEvent(CorModuleLoadEventArgs ev)
        {
            if (!options.IsAttaching)
            {
                var symreader = ev.Module.GetSymbolReader();
                if (symreader != null)
                {
                    // we will set breakpoint on the user entry code
                    // when debugger creates the debuggee process
                    Int32 token = symreader.UserEntryPoint.GetToken();
                    if (token != 0)
                    {
                        // FIXME should be better written (control over this breakpoint)
                        CorFunction func = ev.Module.GetFunctionFromToken(token);
                        CorBreakpoint breakpoint = func.CreateBreakpoint();
                        breakpoint.Activate(true);
                    }
                }
            }

            // we need to save the new module in the modules set
            modules.Add(ev.Module);

            ev.Continue = true;

            OnModuleLoad?.Invoke(ev);
        }

        public delegate void CorExceptionEventHandler(CorExceptionEventArgs ev);

        /// <summary>
        /// Occurs when module is loaded
        /// </summary>
        public event CorExceptionEventHandler OnException;

        internal void DispatchEvent(CorExceptionEventArgs ev)
        {
            // TODO What do we need here? Continue it true/false?
            //ev.Continue = false;

            OnException?.Invoke(ev);
        }


        internal void DispatchEvent(CorEventArgs ev)
        {
            Console.WriteLine($"Debugger Event: {ev.EventInfo}");
            // by default do nothing
            ev.Continue = true;
        }

        /// <summary>
        /// Gets the process from process collection (if it was already
        /// created) or creates a new instance of the CorProcess class.
        /// </summary>
        /// <param name="coproc">The coproc.</param>
        /// <param name="options">The options.</param>
        /// <returns>CorProcess instance.</returns>
        internal static CorProcess GetOrCreateCorProcess(ICorDebugProcess coproc, CorDebuggerOptions options)
        {
            lock (processes)
            {
                processes.TryGetValue(coproc, out var proc);
                if (proc == null)
                {
                    proc = new CorProcess(coproc, options);
                    processes.Add(coproc, proc);
                }
                return proc;
            }
        }

        // finds module by name (may be whole path to the module assembly
        private CorModule FindModuleByName(String moduleName)
        {
            foreach (CorModule m in Modules)
            {
                String mn = Path.GetFileName(m.GetName());
                if (String.Equals(mn, moduleName, StringComparison.Ordinal))
                {
                    return m;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns process ID.
        /// </summary>
        /// <returns>process ID</returns>
        public Int32 GetId()
        {
            this.ComProcess.GetID(out var id);

            return (Int32)id;
        }

        /// <summary>
        /// Resolves function from its name.
        /// </summary>
        /// <param name="moduleName">module name</param>
        /// <param name="className">class name</param>
        /// <param name="functionName">wanted function name</param>
        /// <returns></returns>
        public CorFunction ResolveFunctionName(String moduleName, String className, String functionName)
        {
            // find module
            CorModule module = FindModuleByName(moduleName);
            if (module == null)
            {
                return null;
            }

            Int32 typeToken = module.GetTypeTokenFromName(className);
            if (typeToken == CorConstants.TokenNotFound)
                return null;

            Type t = new MetadataType(module.GetMetadataInterface<IMetadataImport>(), typeToken);
            CorFunction func = null;
            foreach (MethodInfo mi in t.GetMethods())
            {
                if (String.Equals(mi.Name, functionName, StringComparison.Ordinal))
                {
                    func = module.GetFunctionFromToken(mi.MetadataToken);
                    break;
                }
            }
            return func;
        }

        /// <summary>
        /// Resolves code location after the source file name and the code line number.
        /// </summary>
        /// <param name="fileName">source file name</param>
        /// <param name="lineNumber">line number in the source file</param>
        /// <param name="iloffset">returns the offset in the il code (based on the line number)</param>
        /// <returns></returns>
        public CorCode ResolveCodeLocation(String fileName, Int32 lineNumber, out Int32 iloffset)
        {
            // find module
            foreach (CorModule module in Modules)
            {
                ISymbolReader symreader = module.GetSymbolReader();
                if (symreader == null) 
                    continue;

                foreach (ISymbolDocument symdoc in symreader.GetDocuments())
                {
                    if (String.Compare(symdoc.URL, fileName, true, CultureInfo.InvariantCulture) != 0 &&
                        String.Compare(System.IO.Path.GetFileName(symdoc.URL), fileName, true, CultureInfo.InvariantCulture) != 0) 
                        continue;

                    Int32 line = 0;
                    try
                    {
                        line = symdoc.FindClosestLine(lineNumber);
                    }
                    catch (COMException ex)
                    {
                        if (ex.ErrorCode == (Int32)HResult.E_FAIL)
                            continue; // it's not this document
                    }
                    ISymbolMethod symmethod = symreader.GetMethodFromDocumentPosition(symdoc, line, 0);
                    CorFunction func = module.GetFunctionFromToken(symmethod.Token.GetToken());
                    // IL offset in function code
                    iloffset = func.GetIPFromPosition(symdoc, line);
                    // finally return the code
                    return iloffset == -1 ? null : func.GetILCode();
                }
            }
            iloffset = -1;
            return null;
        }
    }
}
