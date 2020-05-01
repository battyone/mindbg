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
        private readonly String name;

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

        public String Name => name;
    }
}
