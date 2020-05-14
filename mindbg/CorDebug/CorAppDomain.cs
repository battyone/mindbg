using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using MinDbg.NativeApi;

namespace MinDbg.CorDebug
{
    /// <summary>
    /// Representation of the ICorDebugAppDomain interface.
    /// </summary>
    public sealed class CorAppDomain : CorController
    {
        private readonly ICorDebugAppDomain codomain;
        private readonly String name;
        private readonly ConcurrentDictionary<ICorDebugModule, CorModule> moduleCache 
            = new ConcurrentDictionary<ICorDebugModule, CorModule>();

        internal CorAppDomain(ICorDebugAppDomain codomain, CorDebuggerOptions options) 
            : base(codomain, options) 
        {
            this.codomain = codomain;

            Char[] nameRaw = new Char[300];
            this.codomain.GetName((UInt32)nameRaw.Length, out var fetched, nameRaw);

            // fetched - 1 because of the ending 0
            name = new String(nameRaw, 0, (Int32)fetched - 1);
        }

        /// <summary>
        /// Gets the COM app domain.
        /// </summary>
        /// <value>The COM app domain.</value>
        private ICorDebugAppDomain ComAppDomain => (ICorDebugAppDomain)this.cocntrl;

        /// <summary>
        /// Gets the process.
        /// </summary>
        /// <returns></returns>
        public CorProcess GetProcess()
        {
            ComAppDomain.GetProcess(out var proc);

            return proc != null ? CorProcess.GetOrCreateCorProcess(proc, options) : null;
        }

        // TODO change to use out param? like GetModule(..) and other APIs?
        internal CorModule GetCachedModule(ICorDebugModule pModule)
        {
            if (moduleCache.TryGetValue(pModule, out var module))
                return module;
            return null;
        }

        internal IEnumerable<CorModule> GetCachedModules()
        {
            return moduleCache.Values;
        }

        public String Name => name;

        internal void LoadModule(ICorDebugModule pModule, CorModule module)
        {
            if (moduleCache.TryAdd(pModule, module) == false)
            {
                Console.WriteLine($"Unable to LOAD Module {pModule}, {module} into the cache");
            }
        }

        internal void UnloadModule(ICorDebugModule pModule)
        {
            if (moduleCache.TryRemove(pModule, out _))
            {
                Console.WriteLine($"Unable to UNLOAD Module {pModule} from the cache");
            }
        }
    }
}
