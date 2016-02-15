namespace Binarysharp.MemoryManagement.Hooks.WndProc
{
    /// <summary>
    ///     Defines an engine for the <see cref="WindowProcHook" /> class, and the methods defined in this engine can be
    ///     invoked by sending the user message <see cref="UserMessage.StartUp" /> and <see cref="UserMessage.ShutDown" /> to
    ///     the specifed <see cref="WindowProcHook" /> instance.
    /// </summary>
    public interface IWindowProcHookEngine
    {
        #region Public Methods
        /// <summary>
        ///     The method called the <see cref="UserMessage.StartUp" /> message is sent to the <see cref="WindowProcHook" />.
        /// </summary>
        void StartUp();

        /// <summary>
        ///     The method called the <see cref="UserMessage.ShutDown" /> message is sent to the <see cref="WindowProcHook" />.
        /// </summary>
        void ShutDown();
        #endregion
    }
}