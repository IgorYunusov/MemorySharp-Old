﻿using System;
using ToolsSharp.Native.Enums;
using ToolsSharp.Windows.BaseClasses;

namespace ToolsSharp.Windows.Objects
{
    /// <summary>
    ///     Class defining a virtual keyboard using the API Message.
    /// </summary>
    public class MessageKeyboard : BaseKeyboard
    {
        #region Constructors, Destructors
        /// <summary>
        ///     Initializes a new instance of a child of the <see cref="BaseKeyboard" /> class.
        /// </summary>
        /// <param name="proxyWindow"></param>
        public MessageKeyboard(ProxyWindow proxyWindow) : base(proxyWindow.Handle)
        {
            ProxyWindow = proxyWindow;
        }
        #endregion

        #region Public Properties, Indexers
        /// <summary>
        ///     Gets the proxy window instance.
        /// </summary>
        /// <value>The proxy window instance.</value>
        public ProxyWindow ProxyWindow { get; }
        #endregion

        /// <summary>
        ///     Presses the specified virtual key to the window.
        /// </summary>
        /// <param name="key">The virtual key to press.</param>
        public override void Press(Keys key)
        {
            PostMessage(WindowsMessages.KeyDown, new UIntPtr((uint) key), MakeKeyParameter(key, false));
        }

        /// <summary>
        ///     Releases the specified virtual key to the window.
        /// </summary>
        /// <param name="key">The virtual key to release.</param>
        public override void Release(Keys key)
        {
            // Call the base function
            base.Release(key);
            PostMessage(WindowsMessages.KeyUp, new UIntPtr((uint) key), MakeKeyParameter(key, true));
        }

        /// <summary>
        ///     Writes the specified character to the window.
        /// </summary>
        /// <param name="character">The character to write.</param>
        public override void Write(char character)
        {
            PostMessage(WindowsMessages.Char, new UIntPtr(character), UIntPtr.Zero);
        }

        /// <summary>
        ///     Makes the lParam for a key depending on several settings.
        /// </summary>
        /// <param name="key">
        ///     [16-23 bits] The virtual key.
        /// </param>
        /// <param name="keyUp">
        ///     [31 bit] The transition state.
        ///     The value is always 0 for a <see cref="WindowsMessages.KeyDown" /> message.
        ///     The value is always 1 for a <see cref="WindowsMessages.KeyUp" /> message.
        /// </param>
        /// <param name="fRepeat">
        ///     [30 bit] The previous key state.
        ///     The value is 1 if the key is down before the message is sent, or it is zero if the key is up.
        ///     The value is always 1 for a <see cref="WindowsMessages.KeyUp" /> message.
        /// </param>
        /// <param name="cRepeat">
        ///     [0-15 bits] The repeat count for the current message.
        ///     The value is the number of times the keystroke is autorepeated as a result of the user holding down the key.
        ///     If the keystroke is held long enough, multiple messages are sent. However, the repeat count is not cumulative.
        ///     The repeat count is always 1 for a <see cref="WindowsMessages.KeyUp" /> message.
        /// </param>
        /// <param name="altDown">
        ///     [29 bit] The context code.
        ///     The value is always 0 for a <see cref="WindowsMessages.KeyDown" /> message.
        ///     The value is always 0 for a <see cref="WindowsMessages.KeyUp" /> message.
        /// </param>
        /// <param name="fExtended">
        ///     [24 bit] Indicates whether the key is an extended key, such as the right-hand ALT and CTRL keys that appear on
        ///     an enhanced 101- or 102-key keyboard. The value is 1 if it is an extended key; otherwise, it is 0.
        /// </param>
        /// <returns>The return value is the lParam when posting or sending a message regarding key press.</returns>
        /// <remarks>
        ///     KeyDown resources: http://msdn.microsoft.com/en-us/library/windows/desktop/ms646280%28v=vs.85%29.aspx
        ///     KeyUp resources:  http://msdn.microsoft.com/en-us/library/windows/desktop/ms646281%28v=vs.85%29.aspx
        /// </remarks>
        private static UIntPtr MakeKeyParameter(Keys key, bool keyUp, bool fRepeat, uint cRepeat, bool altDown,
                                                bool fExtended)
        {
            // Create the result and assign it with the repeat count
            var result = cRepeat;
            // Add the scan code with a left shift operation
            result |= WindowCore.MapVirtualKey(key, TranslationTypes.VirtualKeyToScanCode) << 16;
            // Does we need to set the extended flag ?
            if (fExtended)
                result |= 0x1000000;
            // Does we need to set the alt flag ?
            if (altDown)
                result |= 0x20000000;
            // Does we need to set the repeat flag ?
            if (fRepeat)
                result |= 0x40000000;
            // Does we need to set the keyUp flag ?
            if (keyUp)
                result |= 0x80000000;

            return new UIntPtr(result);
        }

        /// <summary>
        ///     Makes the lParam for a key depending on several settings.
        /// </summary>
        /// <param name="key">The virtual key.</param>
        /// <param name="keyUp">
        ///     The transition state.
        ///     The value is always 0 for a <see cref="WindowsMessages.KeyDown" /> message.
        ///     The value is always 1 for a <see cref="WindowsMessages.KeyUp" /> message.
        /// </param>
        /// <returns>The return value is the lParam when posting or sending a message regarding key press.</returns>
        private static UIntPtr MakeKeyParameter(Keys key, bool keyUp)
        {
            return MakeKeyParameter(key, keyUp, keyUp, 1, false, false);
        }
    }
}