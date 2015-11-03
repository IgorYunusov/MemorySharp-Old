﻿namespace Binarysharp.MemoryManagement.Logging
{
    /// <summary>
    ///     An interface for creating very basic logger classes.
    /// </summary>
    public interface ILog
    {
        #region Methods

        /// <summary>
        ///     LogNormal a warning log.
        /// </summary>
        /// <param name="message">The message to write.</param>
        void LogWarning(string message);

        /// <summary>
        ///     LogNormal a normal log.
        /// </summary>
        /// <param name="message">The message to write.</param>
        void LogNormal(string message);

        /// <summary>
        ///     LogNormal an error log.
        /// </summary>
        /// <param name="message">The message to write.</param>
        void LogError(string message);

        /// <summary>
        ///     LogNormal an information log.
        /// </summary>
        /// <param name="message">The message to write.</param>
        void LogInfo(string message);

        #endregion
    }
}