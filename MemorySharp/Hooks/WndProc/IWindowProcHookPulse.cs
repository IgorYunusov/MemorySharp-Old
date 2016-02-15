namespace Binarysharp.MemoryManagement.Hooks.WndProc
{
    /// <summary>
    ///     Defines a <code>void Pulse();</code> method interface to be used to execute a pulse method inside the
    ///     <see cref="WindowProcHook" /> instance.
    /// </summary>
    public interface IWindowProcHookPulse
    {
        #region Public Methods
        /// <summary>
        ///     Pulses this instance.
        /// </summary>
        void Pulse();
        #endregion
    }
}