using System;
using MinDbg.NativeApi;

namespace MinDbg.CorDebug
{
    public class CorException : WrapperBase
    {
        internal CorException(CorAppDomain appDomain, ICorDebugValue exception, CorDebuggerOptions options)
            : base(exception, options)
        {
        }

        public string TypeName { get; }

        public string Message { get; }

    }
}
