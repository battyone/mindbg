﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.InteropServices;
using MinDbg.NativeApi;

namespace MinDbg.CorDebug
{
    /// <summary>
    /// ICorDebug managed wrapper.
    /// </summary>
    public sealed class CorDebugger
    {
        private readonly ICorDebug codebugger;
        private readonly CorDebuggerOptions options;
        private static ConcurrentDictionary<ICorDebugAppDomain, CorAppDomain> appDomainCache =
            new ConcurrentDictionary<ICorDebugAppDomain, CorAppDomain>();

        /// <summary>
        /// Creates a new ICorDebug instance, initializes it
        /// and sets the managed callback listener.
        /// </summary>
        /// <param name="codebugger">ICorDebug COM object.</param>
        /// <param name="options">The options.</param>
        internal CorDebugger(ICorDebug codebugger, CorDebuggerOptions options)
        {
            this.codebugger = codebugger;
            this.options = options;

            this.codebugger.Initialize();
            this.codebugger.SetManagedHandler(new ManagedCallback(options));
        }

        /// <summary>
        /// Creates a new managed process. 
        /// </summary>
        /// <param name="exepath">application executable file</param>
        /// <param name="currdir">starting directory</param>
        /// <returns></returns>
        public CorProcess CreateProcess(String exepath, String currdir = ".")
        {
            STARTUPINFO si = new STARTUPINFO();
            si.cb = Marshal.SizeOf(si);

            // initialize safe handles 
            si.hStdInput = new Microsoft.Win32.SafeHandles.SafeFileHandle(new IntPtr(0), false);
            si.hStdOutput = new Microsoft.Win32.SafeHandles.SafeFileHandle(new IntPtr(0), false);
            si.hStdError = new Microsoft.Win32.SafeHandles.SafeFileHandle(new IntPtr(0), false);

            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();

            codebugger.CreateProcess(
                                exepath,
                                exepath,
                                null,
                                null,
                                1, // inherit handles
                                (UInt32)CreateProcessFlags.CREATE_NEW_CONSOLE,
                                new IntPtr(0),
                                ".",
                                si,
                                pi,
                                CorDebugCreateProcessFlags.DEBUG_NO_SPECIAL_OPTIONS,
                                out var proc);
            // FIXME close handles (why?)
            options.IsAttaching = false;

            return CorProcess.GetOrCreateCorProcess(proc, options);
        }

        /// <summary>
        /// Attaches to the process with the given pid.
        /// </summary>
        /// <param name="pid">active process id</param>
        /// <param name="win32Attach"></param>
        /// <returns></returns>
        public CorProcess DebugActiveProcess(Int32 pid, Boolean win32Attach = false)
        {
            codebugger.DebugActiveProcess(Convert.ToUInt32(pid), win32Attach ? 1 : 0, out var coproc);
            options.IsAttaching = true;

            return CorProcess.GetOrCreateCorProcess(coproc, options);
        }

        #region ICorDebugManagedCallback events

        private sealed class ManagedCallback : ICorDebugManagedCallback, ICorDebugManagedCallback2
        {
            private readonly CorDebuggerOptions p_options;

            internal ManagedCallback(CorDebuggerOptions options)
            {
                this.p_options = options;
            }

            CorProcess GetOwner(CorController controller)
            {
                if (controller is CorAppDomain domain)
                    return domain.GetProcess();
                 
                return (CorProcess)controller;
            }

            void FinishEvent(CorEventArgs ev)
            {
                if (ev.Continue)
                    ev.Controller.Continue(false);
            }

            private CorAppDomain GetCachedAppDomain(ICorDebugAppDomain pAppDomain)
            {
                if (appDomainCache.TryGetValue(pAppDomain, out var appDomain))
                    return appDomain;

                // Erm what to do here? new-up a CorAppDomain()??
                Console.WriteLine($"Error, unable to find an AppDomain in the cache for {pAppDomain}");
                throw new InvalidOperationException("Erm, not sure what to do?!");
            }

            void ICorDebugManagedCallback.Breakpoint(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugBreakpoint pBreakpoint)
            {
                var ev = new CorBreakpointEventArgs(GetCachedAppDomain(pAppDomain),
                                                    new CorThread(pThread, p_options), 
                                                    new CorFunctionBreakpoint((ICorDebugFunctionBreakpoint)pBreakpoint, p_options));
                
                GetOwner(ev.Controller).DispatchEvent(ev);
                
                FinishEvent(ev);
            }

            void ICorDebugManagedCallback.StepComplete(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugStepper pStepper, CorDebugStepReason reason)
            {
                var ev = new CorEventArgs(GetCachedAppDomain(pAppDomain), "StepComplete");

                GetOwner(ev.Controller).DispatchEvent(ev);

                FinishEvent(ev);
            }

            void ICorDebugManagedCallback.Break(ICorDebugAppDomain pAppDomain, ICorDebugThread thread)
            {
                var ev = new CorEventArgs(GetCachedAppDomain(pAppDomain), "Break");

                GetOwner(ev.Controller).DispatchEvent(ev);

                FinishEvent(ev);
            }

            void ICorDebugManagedCallback.Exception(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, int unhandled)
            {
                var ev = new CorEventArgs(new CorAppDomain(pAppDomain, p_options), "Exception");

                GetOwner(ev.Controller).DispatchEvent(ev);

                FinishEvent(ev);
            }

            void ICorDebugManagedCallback.EvalComplete(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugEval pEval)
            {
                var ev = new CorEventArgs(GetCachedAppDomain(pAppDomain), "EvalComplete");

                GetOwner(ev.Controller).DispatchEvent(ev);

                FinishEvent(ev);
            }

            void ICorDebugManagedCallback.EvalException(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugEval pEval)
            {
                var ev = new CorEventArgs(GetCachedAppDomain(pAppDomain), "EvalException");

                GetOwner(ev.Controller).DispatchEvent(ev);

                FinishEvent(ev);
            }

            void ICorDebugManagedCallback.CreateProcess(ICorDebugProcess pProcess)
            {
                var ev = new CorEventArgs(CorProcess.GetOrCreateCorProcess(pProcess, p_options), "CreateProcess");

                GetOwner(ev.Controller).DispatchEvent(ev);

                FinishEvent(ev);
            }

            void ICorDebugManagedCallback.ExitProcess(ICorDebugProcess pProcess)
            {
                var ev = new CorEventArgs(CorProcess.GetOrCreateCorProcess(pProcess, p_options), "ExitProcess");

                GetOwner(ev.Controller).DispatchEvent(ev);

                ev.Continue = false;
                FinishEvent(ev);
            }

            void ICorDebugManagedCallback.CreateThread(ICorDebugAppDomain pAppDomain, ICorDebugThread thread)
            {
                var ev = new CorEventArgs(GetCachedAppDomain(pAppDomain), "CreateThread");

                GetOwner(ev.Controller).DispatchEvent(ev);

                FinishEvent(ev);
            }

            void ICorDebugManagedCallback.ExitThread(ICorDebugAppDomain pAppDomain, ICorDebugThread thread)
            {
                var ev = new CorEventArgs(GetCachedAppDomain(pAppDomain), "ExitThread");

                GetOwner(ev.Controller).DispatchEvent(ev);

                FinishEvent(ev);
            }

            void ICorDebugManagedCallback.LoadModule(ICorDebugAppDomain pAppDomain, ICorDebugModule pModule)
            {
                var ev = new CorModuleLoadEventArgs(GetCachedAppDomain(pAppDomain), new CorModule(pModule, p_options));

                GetOwner(ev.Controller).DispatchEvent(ev);

                FinishEvent(ev);
            }

            void ICorDebugManagedCallback.UnloadModule(ICorDebugAppDomain pAppDomain, ICorDebugModule pModule)
            {
                var ev = new CorEventArgs(GetCachedAppDomain(pAppDomain), "UnloadModule");

                GetOwner(ev.Controller).DispatchEvent(ev);

                FinishEvent(ev);

                appDomain.UnloadModule(pModule);
            }

            void ICorDebugManagedCallback.LoadClass(ICorDebugAppDomain pAppDomain, ICorDebugClass c)
            {
                var ev = new CorEventArgs(GetCachedAppDomain(pAppDomain), "LoadClass");

                GetOwner(ev.Controller).DispatchEvent(ev);

                FinishEvent(ev);
            }

            void ICorDebugManagedCallback.UnloadClass(ICorDebugAppDomain pAppDomain, ICorDebugClass c)
            {
                var ev = new CorEventArgs(GetCachedAppDomain(pAppDomain), "UnloadClass");

                GetOwner(ev.Controller).DispatchEvent(ev);

                FinishEvent(ev);
            }

            void ICorDebugManagedCallback.DebuggerError(ICorDebugProcess pProcess, int errorHR, uint errorCode)
            {
                var ev = new CorEventArgs(CorProcess.GetOrCreateCorProcess(pProcess, p_options), $"DebuggerError: 0x{errorHR:x8}, {errorCode}");

                GetOwner(ev.Controller).DispatchEvent(ev);

                FinishEvent(ev);
            }

            void ICorDebugManagedCallback.LogMessage(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, int lLevel, string pLogSwitchName, string pMessage)
            {
                var ev = new CorEventArgs(GetCachedAppDomain(pAppDomain), $"LogMessage: {pLogSwitchName} - {pMessage}");

                GetOwner(ev.Controller).DispatchEvent(ev);

                FinishEvent(ev);
            }

            void ICorDebugManagedCallback.LogSwitch(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, int lLevel, uint ulReason, string pLogSwitchName, string pParentName)
            {
                var ev = new CorEventArgs(GetCachedAppDomain(pAppDomain), $"LogSwitch");

                GetOwner(ev.Controller).DispatchEvent(ev);

                FinishEvent(ev);
            }

            void ICorDebugManagedCallback.CreateAppDomain(ICorDebugProcess pProcess, ICorDebugAppDomain pAppDomain)
            {
                // attach to the appdomain
                pAppDomain.Attach();

                if (appDomainCache.TryAdd(pAppDomain, new CorAppDomain(pAppDomain, p_options)) == false)
                    Console.WriteLine("Unable to add AppDomain to cache during CreateAppDomain callback?!");

                var ev = new CorEventArgs(CorProcess.GetOrCreateCorProcess(pProcess, p_options), "CreateAppDomain");

                GetOwner(ev.Controller).DispatchEvent(ev);

                FinishEvent(ev);
            }

            void ICorDebugManagedCallback.ExitAppDomain(ICorDebugProcess pProcess, ICorDebugAppDomain pAppDomain)
            {
                var ev = new CorEventArgs(CorProcess.GetOrCreateCorProcess(pProcess, p_options), "ExitAppDomain");

                GetOwner(ev.Controller).DispatchEvent(ev);

                FinishEvent(ev);

                if (appDomainCache.TryRemove(pAppDomain, out _) == false)
                    Console.WriteLine("Unable to remove AppDomain from cache during ExitAppDomain callback?!");
            }

            void ICorDebugManagedCallback.LoadAssembly(ICorDebugAppDomain pAppDomain, ICorDebugAssembly pAssembly)
            {
                var ev = new CorEventArgs(GetCachedAppDomain(pAppDomain), "LoadAssembly");

                GetOwner(ev.Controller).DispatchEvent(ev);

                FinishEvent(ev);
            }

            void ICorDebugManagedCallback.UnloadAssembly(ICorDebugAppDomain pAppDomain, ICorDebugAssembly pAssembly)
            {
                var ev = new CorEventArgs(GetCachedAppDomain(pAppDomain), "UnloadAssembly");

                GetOwner(ev.Controller).DispatchEvent(ev);

                FinishEvent(ev);
            }

            void ICorDebugManagedCallback.ControlCTrap(ICorDebugProcess pProcess)
            {
                var ev = new CorEventArgs(CorProcess.GetOrCreateCorProcess(pProcess, p_options), "ControlCTrap");

                GetOwner(ev.Controller).DispatchEvent(ev);

                FinishEvent(ev);
            }

            void ICorDebugManagedCallback.NameChange(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread)
            {
                var ev = new CorEventArgs(GetCachedAppDomain(pAppDomain), "NameChange");

                GetOwner(ev.Controller).DispatchEvent(ev);

                FinishEvent(ev);
            }

            void ICorDebugManagedCallback.UpdateModuleSymbols(ICorDebugAppDomain pAppDomain, ICorDebugModule pModule, System.Runtime.InteropServices.ComTypes.IStream pSymbolStream)
            {
                var ev = new CorEventArgs(GetCachedAppDomain(pAppDomain), "UpdateModuleSymbols");

                GetOwner(ev.Controller).DispatchEvent(ev);

                FinishEvent(ev);
            }

            void ICorDebugManagedCallback.EditAndContinueRemap(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugFunction pFunction, int fAccurate)
            {
                // TODO HandleEvent(ManagedCallbackType.On new CorEventArgs(new CorAppDomain(pAppDomain)));
                var ev = new CorEventArgs(GetCachedAppDomain(pAppDomain), "EditAndContinueRemap");

                GetOwner(ev.Controller).DispatchEvent(ev);

                FinishEvent(ev);
            }

            void ICorDebugManagedCallback.BreakpointSetError(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugBreakpoint pBreakpoint, uint dwError)
            {
                var ev = new CorEventArgs(GetCachedAppDomain(pAppDomain), "BreakpointSetError");

                GetOwner(ev.Controller).DispatchEvent(ev);

                FinishEvent(ev);
            }

            void ICorDebugManagedCallback2.FunctionRemapOpportunity(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugFunction pOldFunction, ICorDebugFunction pNewFunction, uint oldILOffset)
            {
                var ev = new CorEventArgs(GetCachedAppDomain(pAppDomain), "FunctionRemapOpportunity");

                GetOwner(ev.Controller).DispatchEvent(ev);

                FinishEvent(ev);
            }

            void ICorDebugManagedCallback2.CreateConnection(ICorDebugProcess pProcess, uint dwConnectionId, ref ushort pConnName)
            {
                // TODO HandleEvent(ManagedCallbackType.On new CorEventArgs(new CorProcess(pProcess)));
            }

            void ICorDebugManagedCallback2.ChangeConnection(ICorDebugProcess pProcess, uint dwConnectionId)
            {
                // TODO HandleEvent(ManagedCallbackType.oncha new CorEventArgs(new CorProcess(pProcess)));
            }

            void ICorDebugManagedCallback2.DestroyConnection(ICorDebugProcess pProcess, uint dwConnectionId)
            {
                // TODO HandleEvent(new CorEventArgs(new CorProcess(pProcess)));
            }

            void ICorDebugManagedCallback2.Exception(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugFrame pFrame, uint nOffset, CorDebugExceptionCallbackType dwEventType, uint dwFlags)
            {
                var ev = new CorEventArgs(GetCachedAppDomain(pAppDomain), $"Exception (2) - {dwEventType}");

                GetOwner(ev.Controller).DispatchEvent(ev);

                FinishEvent(ev);
            }

            void ICorDebugManagedCallback2.ExceptionUnwind(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, CorDebugExceptionUnwindCallbackType dwEventType, uint dwFlags)
            {
                var ev = new CorEventArgs(GetCachedAppDomain(pAppDomain), "ExceptionUnwind (2)");

                GetOwner(ev.Controller).DispatchEvent(ev);

                FinishEvent(ev);
            }

            void ICorDebugManagedCallback2.FunctionRemapComplete(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugFunction pFunction)
            {
                // TODO HandleEvent(<new CorEventArgs(GetCachedAppDomain(pAppDomain));
            }

            void ICorDebugManagedCallback2.MDANotification(ICorDebugController pController, ICorDebugThread pThread, ICorDebugMDA pMDA)
            {
                // TODO nothing for now
            }
        }

        #endregion
    }
}
