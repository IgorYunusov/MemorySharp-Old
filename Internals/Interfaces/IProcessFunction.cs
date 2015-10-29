﻿using System;
using System.Diagnostics;

namespace MemorySharp.Internals.Interfaces
{
    /// <summary>
    ///     Defines a function inside of a <see cref="Process" />.
    /// </summary>
    public interface IProcessFunction
    {
        #region  Properties
        /// <summary>
        ///     The pointer to the function in memory.
        /// </summary>
        IntPtr Pointer { get; }

        /// <summary>
        ///     The name representing the function.
        /// </summary>
        string Name { get; }
        #endregion

        #region Methods
        /// <summary>
        ///     Get the functions delegate.
        /// </summary>
        /// <typeparam name="T">The delegate.</typeparam>
        /// <returns>A type.</returns>
        T GetDelegate<T>();
        #endregion
    }
}