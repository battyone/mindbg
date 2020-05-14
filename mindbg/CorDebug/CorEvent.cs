
namespace MinDbg.CorDebug
{
    /// <summary>
    /// A base class for all debugging events.
    /// </summary>
    public class CorEventArgs
    {
        /// <summary>
        /// Initializes the event instance.
        /// </summary>
        /// <param name="controller">Controller of the debugging process.</param>
        public CorEventArgs(CorController controller, string eventInfo)
        {
            Controller = controller;
            EventInfo = eventInfo;
        }

        /// <summary>
        /// Gets the controller.
        /// </summary>
        /// <value>The controller.</value>
        public CorController Controller { get; }

        public string EventInfo { get; }

        /// <summary>
        /// Gets or sets a value indicating whether debugging process should continue.
        /// </summary>
        /// <value><c>true</c> if continue; otherwise, <c>false</c>.</value>
        public bool Continue { get; set; }
    }

    /// <summary>
    /// Event args for module load event.
    /// </summary>
    public sealed class CorModuleLoadEventArgs : CorEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CorModuleLoadEventArgs"/> class.
        /// </summary>
        /// <param name="controller">The controller.</param>
        /// <param name="module">The module.</param>
        public CorModuleLoadEventArgs(CorController controller, CorModule module)
            : base(controller, "ModuleLoad")
        {
            Module = module;
        }

        /// <summary>
        /// Gets the module.
        /// </summary>
        /// <value>The module.</value>
        public CorModule Module { get; }
    }

    /// <summary>
    /// Event args for breakpoint event.
    /// </summary>
    public sealed class CorBreakpointEventArgs : CorEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CorBreakpointEventArgs"/> class.
        /// </summary>
        /// <param name="appdomain">The controller.</param>
        /// <param name="thread">The thread.</param>
        /// <param name="breakpoint">The breakpoint.</param>
        public CorBreakpointEventArgs(CorAppDomain appdomain, CorThread thread, CorBreakpoint breakpoint) 
            : base(appdomain, "Breakpoint")
        {
            Thread = thread;
            Breakpoint = breakpoint;
        }

        /// <summary>
        /// Gets the thread.
        /// </summary>
        /// <value>The thread.</value>
        public CorThread Thread { get; }

        /// <summary>
        /// Gets the breakpoint.
        /// </summary>
        /// <value>The breakpoint.</value>
        public CorBreakpoint Breakpoint { get; }

        /// <summary>
        /// Gets the process that the breakpoint was hit on.
        /// </summary>
        /// <value>The app domain.</value>
        public CorProcess GetProcess()
        {
            return ((CorAppDomain)Controller).GetProcess();
        }
    }

    public sealed class CorExceptionEventArgs : CorEventArgs
    {
        public CorExceptionEventArgs(CorAppDomain appdomain, CorException exception, int unhandled)
            : base(appdomain, "Exception")
        {
            Exception = exception;
            Unhandled = unhandled;
        }

        public CorException Exception { get; }

        public int Unhandled { get; }
    }
}
