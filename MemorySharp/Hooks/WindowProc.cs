using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using Binarysharp.MemoryManagement.Common;
using Binarysharp.MemoryManagement.Common.Builders;
using Binarysharp.MemoryManagement.Hooks.WndProc;
using Binarysharp.MemoryManagement.Management;
using Binarysharp.MemoryManagement.Native;

namespace Binarysharp.MemoryManagement.Hooks
{

  

    /// <summary>
    ///     Custom windows user messages that represent values to intercept and react on when detected in the windows message
    ///     queue, normally via a <code>WindowProc</code> hook.
    /// </summary>
    public enum UserMessage
    {
        /// <summary>
        ///     A delegate that has a return value.
        /// </summary>
        RunFunc = 0,

        /// <summary>
        ///     A delegate with no return value (aka <code>void</code>).
        /// </summary>
        RunAction = 1,

        /// <summary>
        ///     A delegate with a return value.
        /// </summary>
        RunDelegateWithReturn,

        /// <summary>
        /// The message to intercept in the WndProc hook to invoke the <see cref="IWindowProcHookEngine.StartUp()"/> method.
        /// </summary>
        StartUp,

        /// <summary>
        /// The message to intercept in the WndProc hook to invoke the <see cref="IWindowProcHookEngine.ShutDown()"/> method.
        /// </summary>
        ShutDown
    }

    /// <summary>
    /// </summary>
    public class InvokeTarget
    {
        #region Fields, Private Properties
        /// <summary>
        ///     The arguments
        /// </summary>
        public object[] Args;

        /// <summary>
        ///     The completed
        /// </summary>
        public bool Completed;

        /// <summary>
        ///     The result
        /// </summary>
        public object Result;

        /// <summary>
        ///     The target
        /// </summary>
        public Delegate Target;
        #endregion
    }

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
    public class WindowProc : IHook
    {
        #region Fields, Private Properties
        private int GwlWndproc { get; } = -4;
        private WindowProcDel OurCallBackFunc { get; set; }
        private IntPtr OurCallBackPointer { get; set; }
        private IntPtr OriginalCallbackPointer { get; set; }

        // Collections.
        private GenericDictionary PendingFuncInvokes { get; } = new GenericDictionary();
        private List<Action> PendingActionInvokes { get; } = new List<Action>();
        private ConcurrentQueue<InvokeTarget> InvokeQueue { get; } = new ConcurrentQueue<InvokeTarget>();
        #endregion

        #region Constructors, Destructors
        /// <summary>
        ///     Initializes a new instance of the <see cref="Hooks.WindowProc" /> class.
        /// </summary>
        /// <param name="windowHandle">A handle to the window and, indirectly, the class to which the window belongs.</param>
        /// <param name="instanceName">The unique name representing this instance.</param>
        /// <param name="mustMeDisposed"></param>
        public WindowProc(IntPtr windowHandle, string instanceName, bool mustMeDisposed = true)
        {
            Handle = windowHandle;
            Name = instanceName;
            MustBeDisposed = mustMeDisposed;
            IsEnabled = false;
        }

        /// <summary>
        ///     Allows an object to try to free resources and perform other cleanup operations before it is reclaimed by garbage
        ///     collection.
        /// </summary>
        ~WindowProc()
        {
            Dispose();
        }
        #endregion

        #region Public Properties, Indexers
        /// <summary>
        ///     Gets or sets the main window handle of the process being hooked.
        /// </summary>
        /// <value>
        ///     The main window handle.
        /// </value>
        public IntPtr Handle { get; set; }

        /// <summary>
        ///     Gets or sets the WM_USER message code for this instance. It can be defined as anything between 0x0400 and 0x7FFF.
        /// </summary>
        /// <value>The custom WM_USER message code intercepted in the hook for this instance.</value>
        public int WmUser { get; set; } = 0x0400;

        /// <summary>
        ///     Gets the unique name that represents this instance.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; }

        /// <summary>
        ///     States if the <code>WndProc</code> hook is currently enabled.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        ///     Gets a value indicating whether the this instance disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether the instance must be disposed when the Garbage Collector collects the object. The
        ///     default value is true.
        /// </summary>
        public bool MustBeDisposed { get; }

        /// <summary>
        ///     Checks if the current managed thread id matches the local processes first thread id, indicating if the current
        ///     managed thread id is the main thread or not.
        /// </summary>
        /// <value>True if this instance is in the main thread, else, false.</value>
        public static bool InvokeRequired => Thread.CurrentThread.ManagedThreadId != Process.GetCurrentProcess().Threads[0].Id;
        #endregion

        #region Interface Implementations
        /// <summary>
        ///     Enables the <code>WndProc</code> hook.
        /// </summary>
        public void Enable()
        {
            if (IsEnabled)
            {
                Disable();
            }

            // Pins WndProc - will not be garbage collected.
            OurCallBackFunc = WndProc;
            // Store the call back pointer. Storing the result is not needed, however. 
            OurCallBackPointer = Marshal.GetFunctionPointerForDelegate(OurCallBackFunc);
            // This helper method will work with x32 or x64.
            OriginalCallbackPointer = NativeMethods.SetWindowLongPtr(Handle, GwlWndproc, OurCallBackPointer);

            // Just to be sure.
            if (OriginalCallbackPointer == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            IsEnabled = true;
        }

        /// <summary>
        ///     Disables the <code>WndProc</code> hook.
        /// </summary>
        public void Disable()
        {
            try
            {
                // We have not successfully enabled the hook yet in this case, so no need to disable.
                if (OurCallBackFunc == null)
                {
                    IsEnabled = false;
                    return;
                }
                // Sets the call back to the original. This helper method will work with x32 or x64.
                NativeMethods.SetWindowLongPtr(Handle, GwlWndproc, OriginalCallbackPointer);
                OurCallBackFunc = null;
                IsEnabled = false;
            }
            catch (Exception ex)
            {
                LogManager.Instance.ItemsAsDictionary["Debug"].Write(ex.ToString());
            }
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (!MustBeDisposed)
            {
                return;
            }
            // Else.
            Disable();
            IsDisposed = true;
            GC.SuppressFinalize(this);
        }
        #endregion

        #region Public Methods
        /// <summary>
        ///     Used to send the custom user message to be intercepted in the WindowProc hook call back.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="lParam">The lpParam.</param>
        public void SendUserMessage(UserMessage message, IntPtr lParam)
        {
            NativeMethods.SendMessage(Handle, (uint) WmUser, (UIntPtr) message, lParam);
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
            PendingFuncInvokes.Add(invokedValueContainer.FuncHashCode, invokedValueContainer);
            // Pass the hash code (which is the dict key to the invoked value container object)
            // Through the user message.
            SendUserMessage(UserMessage.RunFunc, (IntPtr) invokedValueContainer.FuncHashCode);
            // Get the resut casted to the type given.
            var result =
                PendingFuncInvokes.GetValue<GenericValueProxy<T>>(invokedValueContainer.FuncHashCode).Result;
            // Remove the container and return the result.
            PendingFuncInvokes.Remove(invokedValueContainer.FuncHashCode);
            return (T) result;
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

            var callback = new InvokeTarget {Target = targetDelegate, Args = args};
            InvokeQueue.Enqueue(callback);
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
            PendingActionInvokes.Add(action);
            SendUserMessage(UserMessage.RunAction, IntPtr.Zero);
        }
        #endregion

        #region Private Methods
        // TODO: Document
        [SuppressMessage("ReSharper", "SwitchStatementMissingSomeCases")]
        private int WndProc(IntPtr hWnd, int msg, int wParam, int lpParam)
        {
            if (msg != WmUser)
            {
                return NativeMethods.CallWindowProc(OriginalCallbackPointer, hWnd, msg, wParam, lpParam);
            }
            var tmpMsg = (UserMessage) wParam;
            switch (tmpMsg)
            {
                // LpParam is used to pass a hash code sometimes.
                case UserMessage.RunFunc:
                    ((IGenericValue) PendingFuncInvokes.ItemsAsDictionary[lpParam]).SetValue();
                    break;

                case UserMessage.RunAction:
                    var number = PendingActionInvokes.Count - 1;
                    PendingActionInvokes[number].Invoke();
                    PendingActionInvokes.RemoveAt(number);
                    break;

                case UserMessage.RunDelegateWithReturn:
                    InvokeLastQueue();
                    break;
            }
            return NativeMethods.CallWindowProc(OriginalCallbackPointer, hWnd, msg, wParam, lpParam);
        }

        private void InvokeLastQueue()
        {
            InvokeTarget callback;
            while (InvokeQueue.TryDequeue(out callback))
            {
                callback.Result = callback.Target.DynamicInvoke(callback.Args);
                callback.Completed = true;
            }
        }
        #endregion

        //TODO: Document
        private delegate int WindowProcDel(IntPtr hWnd, int msg, int wParam, int lParam);
    }
}