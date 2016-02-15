using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Binarysharp.MemoryManagement.Common;
using Binarysharp.MemoryManagement.Common.Builders;
using Binarysharp.MemoryManagement.Management;
using Binarysharp.MemoryManagement.Native;

namespace Binarysharp.MemoryManagement.Hooks.WndProc
{
    /// <summary>
    ///     Class containing operations and properties to hook the <code>WndProc</code> method and allow the user to invoke
    ///     actions and methods with return values inside the main thread of the window the instance is attatched to.
    ///     <remarks>
    ///         All windows messages are sent to the WndProc method after getting filtered through the PreProcessMessage
    ///         method. This means we can hook this method, and intercept a custom windows message and handle it. For more
    ///         information on this method, refer to:
    ///         https://msdn.microsoft.com/en-us/library/system.windows.forms.control.wndproc(v=vs.110).aspx.
    ///     </remarks>
    /// </summary>
    public class WindowProcHook : IHook
    {
        #region Fields, Private Properties
        private const int GwlWndproc = -4;
        private readonly IWindowProcHookEngine _engine;
        private readonly IntPtr _handle;
        private readonly ConcurrentQueue<InvokeTarget> _invokeQueue = new ConcurrentQueue<InvokeTarget>();
        private readonly List<Action> _pendingActionInvokes = new List<Action>();
        private readonly GenericDictionary _pendingFuncInvokes = new GenericDictionary();

        /// <summary>
        ///     Class to manage <see cref="IWindowProcHookPulse" /> elements that will be executed inside the thread that the
        ///     window this instances WndProc hook is attatched to, when the <see cref="UserMessage.StartUp" /> message is sent.
        /// </summary>
        public readonly WindowProcPulseEngine Pulses = new WindowProcPulseEngine();

        private IntPtr _originalCallbackPointer;

        private WindowProcDel _ourCallBackFunc;
        private IntPtr _ourCallBackPointer;
        #endregion

        #region Constructors, Destructors
        /// <summary>
        ///     Initializes a new instance of the <see cref="WindowProcHook" /> class.
        /// </summary>
        /// <param name="instanceName">Name of the instance.</param>
        /// <param name="windowHandle">The window handle.</param>
        /// <param name="engine">The engine.</param>
        /// <param name="mustBeDispose">if set to <c>true</c> [must be disposed].</param>
        public WindowProcHook(string instanceName, IntPtr windowHandle, ref IWindowProcHookEngine engine,
            bool mustBeDispose = true)
        {
            Name = instanceName;
            _handle = windowHandle;
            _engine = engine;
            IsDisposed = false;
            MustBeDisposed = mustBeDispose;
            IsEnabled = false;
        }
        #endregion

        #region Public Properties, Indexers
        /// <summary>
        ///     Gets or sets the WM_USER message code for this instance. It can be defined as anything between 0x0400 and 0x7FFF.
        /// </summary>
        /// <value>The custom WM_USER message code intercepted in the hook for this instance.</value>
        public int WmUser { get; set; } = 0x0400;

        /// <summary>
        ///     Gets a value indicating whether the element is disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether the instance must be disposed when the Garbage Collector collects the object.
        /// </summary>
        public bool MustBeDisposed { get; }

        /// <summary>
        ///     States if the WindowProc hook is enabled.
        /// </summary>
        public bool IsEnabled { get; private set; }

        /// <summary>
        ///     Checks if the current managed thread id matches the local processes first thread id, indicating if the current
        ///     managed thread id is the main thread or not.
        /// </summary>
        /// <value>True if this instance is in the main thread, else, false.</value>
        public bool InvokeRequired
            => Thread.CurrentThread.ManagedThreadId != Process.GetCurrentProcess().Threads[0].Id;

        /// <summary>
        ///     The name that represents this instance.
        /// </summary>
        public string Name { get; }
        #endregion

        #region Interface Implementations
        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (!MustBeDisposed)
            {
                return;
            }

            Disable();
            IsDisposed = true;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Disables the WndProc hook.
        /// </summary>
        public void Disable()
        {
            try
            {
                // We have not successfully enabled the hook yet in this case, so no need to disable.
                if (_ourCallBackFunc == null)
                {
                    IsEnabled = false;
                    return;
                }
                // Sets the call back to the original. This helper method will work with x32 or x64.
                NativeMethods.SetWindowLongPtr(_handle, GwlWndproc, _originalCallbackPointer);
                _ourCallBackFunc = null;
                IsEnabled = false;
            }
            catch (Exception ex)
            {
                LogManager.Instance.ItemsAsDictionary["Debug"].Write(ex.ToString());
            }
        }

        /// <summary>
        ///     Enables the WndProc hook.
        /// </summary>
        public void Enable()
        {
            if (IsEnabled)
            {
                Disable();
            }

            // Pins WndProc - will not be garbage collected.
            _ourCallBackFunc = WndProc;
            // Store the call back pointer. Storing the result is not needed, however. 
            _ourCallBackPointer = Marshal.GetFunctionPointerForDelegate(_ourCallBackFunc);
            // This helper method will work with x32 or x64.
            _originalCallbackPointer = NativeMethods.SetWindowLongPtr(_handle, GwlWndproc, _ourCallBackPointer);

            // Just to be sure.
            if (_originalCallbackPointer == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            IsEnabled = true;
        }
        #endregion
        /// <summary>
        ///     Used to send the custom user message to be intercepted in the WindowProc hook call back.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="lParam">The lpParam.</param>
        public void SendUserMessage(UserMessage message, IntPtr lParam)
        {
            NativeMethods.SendMessage(_handle, (uint)WmUser, (UIntPtr)message, lParam);
        }

        /// <summary>
        ///     Invokes the specified <see cref="Action" /> inside the main thread of process this instance is currently attatched
        ///     to.
        /// </summary>
        /// <param name="action">The action to invoke.</param>
        public void InvokeAction(Action action)
        {
            if (!InvokeRequired)
            {
                // In the main thread already.
                action.Invoke();
            }
            // Else, add to the queue to invoke later.
            _pendingActionInvokes.Add(action);
            SendUserMessage(UserMessage.RunAction, IntPtr.Zero);
        }
        /// <summary>
        ///     Invokes the specified function inside the main thread of the process this instance is currently attatched to.
        /// </summary>
        /// <typeparam name="T">The type of value.</typeparam>
        /// <param name="func">The function that invokes the value.</param>
        /// <returns>A value.</returns>
        public T InvokeFunc<T>(Func<T> func) where T : struct
        {
            if (!InvokeRequired)
            {
                // We're in the main thread.
                return func();
            }
            var invokedValueContainer = GenericValueProxy<T>.Create(func);
            _pendingFuncInvokes.Add(invokedValueContainer.FuncHashCode, invokedValueContainer);
            // Pass the hash code (which is the dict key to the invoked value container object)
            // Through the user message.
            SendUserMessage(UserMessage.RunFunc, (IntPtr)invokedValueContainer.FuncHashCode);
            // Get the resut casted to the type given.
            var result =
                _pendingFuncInvokes.GetValue<GenericValueProxy<T>>(invokedValueContainer.FuncHashCode).Result;
            // Remove the container and return the result.
            _pendingFuncInvokes.Remove(invokedValueContainer.FuncHashCode);
            return (T)result;
        }

        /// <summary>
        ///     Invokes the <see cref="Delegate"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="delegate">The delegate.</param>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        public object InvokeDel<T>(T @delegate, params object[] args) where T : class
        {
            var targetDelegate = @delegate as Delegate;

            if (targetDelegate == null)
            {
                throw new ArgumentException("Target method is not a delegate type.");
            }

            if (!InvokeRequired)
            {
                return targetDelegate.DynamicInvoke(args);
            }

            var callback = new InvokeTarget { Target = targetDelegate, Args = args };
            _invokeQueue.Enqueue(callback);
            SendUserMessage(UserMessage.RunDelegateWithReturn, IntPtr.Zero);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            while (!callback.Completed)
            {
                if (stopwatch.ElapsedMilliseconds < 2000)
                {
                    continue;
                }

                throw new TimeoutException(
                    $"Could not invoke {targetDelegate.Method} from main thread (timed out after two seconds.)");
            }

            stopwatch.Stop();
            return callback.Result;
        }

        #region Private Methods
        private int WndProc(IntPtr hWnd, int msg, int wParam, int lpParam)
        {
            if (msg == WmUser)
            {
                HandleUserMessage((UserMessage) msg, lpParam);
            }
            return NativeMethods.CallWindowProc(_originalCallbackPointer, hWnd, msg, wParam, lpParam);
        }

        /// <summary>
        ///     Invokes the last queue.
        /// </summary>
        private void InvokeLastQueue()
        {
            InvokeTarget callback;
            while (_invokeQueue.TryDequeue(out callback))
            {
                callback.Result = callback.Target.DynamicInvoke(callback.Args);
                callback.Completed = true;
            }
        }

        /// <summary>
        ///     Invokes the last function.
        /// </summary>
        /// <param name="lpParam">The lp parameter.</param>
        private void InvokeLastFunc(int lpParam)
        {
            ((IGenericValue) _pendingFuncInvokes.ItemsAsDictionary[lpParam]).SetValue();
        }

        /// <summary>
        ///     Invokes the last action.
        /// </summary>
        private void InvokeLastAction()
        {
            var number = _pendingActionInvokes.Count - 1;
            _pendingActionInvokes[number].Invoke();
            _pendingActionInvokes.RemoveAt(number);
        }
      
        /// <summary>
        ///     Handles the user message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="lpParam">The lp parameter.</param>
        private void HandleUserMessage(UserMessage message, int lpParam)
        {
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (message)
            {
                case UserMessage.StartUp:
                    _engine.StartUp();
                    break;
                case UserMessage.ShutDown:
                    _engine.ShutDown();
                    break;
                case UserMessage.RunFunc:
                    InvokeLastFunc(lpParam);
                    break;

                case UserMessage.RunAction:
                    InvokeLastAction();
                    break;

                case UserMessage.RunDelegateWithReturn:
                    InvokeLastQueue();
                    break;
            }
        }
        #endregion

        private delegate int WindowProcDel(IntPtr hWnd, int msg, int wParam, int lParam);
    }
}