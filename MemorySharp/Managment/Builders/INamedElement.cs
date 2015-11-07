﻿namespace Binarysharp.MemoryManagement.Managment.Builders
{
    /// <summary>
    ///     Defines a element with a name.
    /// </summary>
    public interface INamedElement : IApplicableElement
    {
        #region Public Properties, Indexers
        /// <summary>
        ///     The name of the element.
        /// </summary>
        string Name { get; }
        #endregion
    }
}