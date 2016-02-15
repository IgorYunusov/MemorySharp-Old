using System.Collections.Generic;
using Binarysharp.MemoryManagement.Common.Extensions;

namespace Binarysharp.MemoryManagement.Hooks.WndProc
{
    /// <summary>
    ///     A <see cref="IWindowProcHookEngine" /> implementation that manages a list of <see cref="IWindowProcHookPulse" />
    ///     elements and supports calling the <code>void Pulse();</code> method for each of the the engines
    ///     <see cref="IWindowProcHookPulse" /> instances, by calling the <see cref="StartUp()" /> method.
    /// </summary>
    public class WindowProcPulseEngine : IWindowProcHookEngine
    {
        #region Fields, Private Properties
        /// <summary>
        ///     The _pulse elements
        /// </summary>
        private readonly List<IWindowProcHookPulse> _pulseElements = new List<IWindowProcHookPulse>();
        #endregion

        #region Interface Implementations
        /// <summary>
        ///     Starts up.
        /// </summary>
        void IWindowProcHookEngine.StartUp()
        {
            if (_pulseElements.Count == 0)
                return;
            _pulseElements.ForEach(p => p.Pulse());
        }

        /// <summary>
        ///     Shuts down.
        /// </summary>
        void IWindowProcHookEngine.ShutDown()
        {
            _pulseElements.Clear();
        }
        #endregion

        #region Public Methods
        /// <summary>
        ///     Registers the specified pulsable(s) to be executed on <see cref="StartUp()" />.
        /// </summary>
        /// <param name="pulsable">The <see cref="IWindowProcHookPulse" /> element.</param>
        /// <param name="pulsables">[Optinal] The <see cref="IWindowProcHookPulse" />'s..</param>
        public void Register(IWindowProcHookPulse pulsable, params IWindowProcHookPulse[] pulsables)
        {
            _pulseElements.Add(pulsable);
            if (pulsables != null && pulsables.Length > 1)
            {
                pulsables.ForEach(p => p.Pulse());
            }
        }

        /// <summary>
        ///     Unregisters the specified pulsable.
        /// </summary>
        /// <param name="pulsable">The <see cref="IWindowProcHookPulse" /> element.</param>
        /// <param name="pulsables">[Optinal] The <see cref="IWindowProcHookPulse" />'s..</param>
        public void Unregister(IWindowProcHookPulse pulsable, params IWindowProcHookPulse[] pulsables)
        {
            _pulseElements.Remove(pulsable);

            if (pulsables != null && pulsables.Length > 1)
            {
                pulsables.ForEach(p => _pulseElements.Remove(p));
            }
        }
        #endregion
    }
}