﻿/*
 * MemorySharp Library
 * http://www.binarysharp.com/
 *
 * Copyright (C) 2012-2014 Jämes Ménétrey (a.k.a. ZenLulz).
 * This library is released under the MIT License.
 * See the file LICENSE for more information.
*/

using System;

namespace Binarysharp.MemoryManagement.RemoteProcess.Threading
{
    /// <summary>
    ///     Class containing a frozen thread. If an instance of this class is disposed, its associated thread is resumed.
    /// </summary>
    public class FrozenThread : IDisposable
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="FrozenThread" /> class.
        /// </summary>
        /// <param name="thread">The frozen thread.</param>
        internal FrozenThread(RemoteThread thread)
        {
            // Save the parameter
            Thread = thread;
        }

        /// <summary>
        ///     The frozen thread.
        /// </summary>
        public RemoteThread Thread { get; }

        #region IDisposable Members

        /// <summary>
        ///     Releases all resources used by the <see cref="RemoteThread" /> object.
        /// </summary>
        public virtual void Dispose()
        {
            // Unfreeze the thread
            Thread.Resume();
        }

        #endregion

        /// <summary>
        ///     Returns a string that represents the current object.
        /// </summary>
        public override string ToString()
        {
            return $"Id = {Thread.Id}";
        }
    }
}