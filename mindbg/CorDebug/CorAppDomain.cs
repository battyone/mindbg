using System;
using MinDbg.NativeApi;

namespace MinDbg.CorDebug
{
    /// <summary>
    /// Representation of the ICorDebugAppDomain interface.
    /// </summary>
    public sealed class CorAppDomain : CorController
    {
        private readonly ICorDebugAppDomain codomain;

        internal CorAppDomain(ICorDebugAppDomain codomain, CorDebuggerOptions options) 
            : base(codomain, options) 
        {
            this.codomain = codomain;
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

        public String Name => name;
    }
}
