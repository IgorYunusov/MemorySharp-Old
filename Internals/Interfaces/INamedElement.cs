﻿/*
 * MemorySharp Library
 * http://www.binarysharp.com/
 *
 * Copyright (C) 2012-2014 Jämes Ménétrey (a.k.a. ZenLulz).
 * This library is released under the MIT License.
 * See the file LICENSE for more information.
*/

namespace MemorySharp.Internals.Interfaces
{
    /// <summary>
    ///     Defines a element with a name.
    /// </summary>
    public interface INamedElement : IApplicableElement
    {
        #region  Properties
        /// <summary>
        ///     The name of the element.
        /// </summary>
        string Name { get; }
        #endregion
    }
}