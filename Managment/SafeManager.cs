﻿using System;
using Binarysharp.MemoryManagement.Common.Extensions;
using Binarysharp.MemoryManagement.Common.Logging.Core;
using Binarysharp.MemoryManagement.Managment.Builders;

namespace Binarysharp.MemoryManagement.Managment
{
    /// <summary>
    ///     Class managing objects implementing <see cref="INamedElement" /> interface.
    ///     <remarks>
    ///         Ths 'safe' manager catches exceptions, provides detailed, formatted exception logs to review and then
    ///         throws the exception still.
    ///     </remarks>
    /// </summary>
    public abstract class SafeManager<T> : BaseManager<T> where T : INamedElement
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SafeManager{T}" /> class.
        /// </summary>
        /// <param name="log">The log to use.</param>
        protected SafeManager(IManagedLog log)
        {
            Log = log;
        }

        /// <summary>
        ///     The ILog implementation to use for the manager.
        /// </summary>
        protected IManagedLog Log { get; set; }

        /// <summary>
        ///     Disables all elements.
        ///     <remarks>Logs detailed exceptions to the managers ILog instance.</remarks>
        /// </summary>
        public override void DisableAll()
        {
            try
            {
                foreach (var item in InternalItems)
                {
                    item.Value.Disable();
                }
            }
            catch (Exception exception)
            {
                Log.LogError(exception.ExtractDetailedInformation());
                throw;
            }
        }

        /// <summary>
        ///     Enables all items in the manager.
        /// </summary>
        public override void EnableAll()
        {
            try
            {
                foreach (var item in InternalItems)
                {
                    item.Value.Enable();
                }
            }
            catch (Exception exception)
            {
                Log.LogError(exception.ExtractDetailedInformation());
                throw;
            }
        }

        /// <summary>
        ///     Removes an element by its name in the manager.
        /// </summary>
        /// <param name="name">The name of the element to remove.</param>
        public override void Remove(string name)
        {
            // Check if the element exists in the dictionary
            if (!InternalItems.ContainsKey(name))
            {
                return;
            }

            try
            {
                InternalItems[name].Dispose();
            }

            catch (Exception exception)
            {
                Log.LogError(exception.ExtractDetailedInformation());
            }

            finally
            {
                InternalItems.Remove(name);
            }
        }

        /// <summary>
        ///     Remove a given element.
        /// </summary>
        /// <param name="item">The element to remove.</param>
        public override void Remove(T item)
        {
            Remove(item.Name);
        }

        /// <summary>
        ///     Removes all the elements in the manager.
        /// </summary>
        public override void RemoveAll()
        {
            // For each element
            try
            {
                foreach (var item in InternalItems)
                {
                    // Dispose it.
                    item.Value.Dispose();
                }
            }
            catch (Exception exception)
            {
                Log.LogError(exception.ExtractDetailedInformation());
                throw;
            }
        }
    }
}