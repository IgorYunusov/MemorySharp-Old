﻿using System;
using System.Linq;
using Binarysharp.MemoryManagement.Hooks.Input;
using Binarysharp.MemoryManagement.Hooks.WndProc;
using Binarysharp.MemoryManagement.Internals;
using Binarysharp.MemoryManagement.Logging.Defaults;
using Binarysharp.MemoryManagement.Logging.Interfaces;

namespace Binarysharp.MemoryManagement.Hooks
{
    /// <summary>
    ///     Class to manage hooks that implement the <see cref="INamedElement" /> Interface.
    /// </summary>
    public class HookFactory : Manager<IHook>, IFactory
    {
        #region Constructors, Destructors
        /// <summary>
        ///     Initializes a new instance of the <see cref="HookFactory" /> class.
        /// </summary>
        /// <param name="logger">The <see cref="IManagedLog" /> instance to use..</param>
        public HookFactory() : base(new DebugLog())
        {
        }
        #endregion

        #region Public Properties, Indexers
        /// <summary>
        ///     Gets or sets the instance reference for the <see cref="MemoryManagement.MemoryPlus" /> class.
        /// </summary>
        /// <value>
        ///     The instance reference for the <see cref="MemoryManagement.MemoryPlus" /> class.
        /// </value>
        public MemoryPlus MemoryPlus { get; set; }

        /// <summary>
        ///     Gets a hook instance from the given name from the managers dictonary of current hooks.
        /// </summary>
        /// <param name="name">The name of the hook.</param>
        public IHook this[string name] => InternalItems[name];
        #endregion

        #region Interface Implementations
        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            foreach (var hookValue in InternalItems.Values.Where(hook => hook.IsEnabled))
            {
                hookValue.Disable();
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        ///     Creates the WND proc hook.
        /// </summary>
        /// <param name="name">The name representingthe hook Instance.</param>
        /// <param name="windowHandle">The window handle.</param>
        /// <param name="engine">
        ///     ///     The instance of the <see cref="IWindowEngine" /> to use. The <code>StartUp()</code> and
        ///     <code>ShutDown</code> methods inside the defined interface given will be called when the corresponding
        ///     <see cref="UserMessage" /> is invoked and sent.
        /// </param>
        /// <returns>WindowHook.</returns>
        public WindowHook CreateWndProcHook(string name, IntPtr windowHandle, ref IWindowEngine engine)
        {
            InternalItems[name] = new WindowHook(windowHandle, name, ref engine);
            return (WindowHook) InternalItems[name];
        }

        /// <summary>
        ///     Creates an instance of a low level mouse hook. This instance is also added to the manager and returned as the
        ///     result.
        /// </summary>
        /// <param name="name">The name of the low level mouse hook.</param>
        /// <param name="mustBeDisposed">
        ///     if set to <c>true</c> the hook must be disposed], else it will not be disposed on garbage
        ///     collection.
        /// </param>
        /// <returns>MouseHook.</returns>
        public MouseHook CreateKMouseHook(string name, bool mustBeDisposed = true)
        {
            InternalItems[name] = new MouseHook(name, mustBeDisposed);
            return (MouseHook) InternalItems[name];
        }

        /// <summary>
        ///     Creates an instance of a low level keyboard hook. This instance is also added to the manager and returned as the
        ///     result.
        /// </summary>
        /// <param name="name">The name of the low level keyboard hook.</param>
        /// <param name="mustBeDisposed">
        ///     if set to <c>true</c> the hook must be disposed], else it will not be disposed on garbage
        ///     collection.
        /// </param>
        /// <returns>MouseHook.</returns>
        public KeyboardHook CreateKeyboardHook(string name, bool mustBeDisposed = true)
        {
            InternalItems[name] = new KeyboardHook(name, mustBeDisposed);
            return (KeyboardHook) InternalItems[name];
        }
        #endregion
    }
}