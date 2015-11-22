﻿/*
 * MemorySharp Library
 * http://www.binarysharp.com/
 *
 * Copyright (C) 2012-2014 Jämes Ménétrey (a.k.a. ZenLulz).
 * This library is released under the MIT License.
 * See the file LICENSE for more information.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Binarysharp.MemoryManagement.Assembly;
using Binarysharp.MemoryManagement.Assembly.CallingConvention;
using Binarysharp.MemoryManagement.Helpers;
using Binarysharp.MemoryManagement.Internals;
using Binarysharp.MemoryManagement.Memory;
using Binarysharp.MemoryManagement.Modules;
using Binarysharp.MemoryManagement.Native;
using Binarysharp.MemoryManagement.Native.Enums;
using Binarysharp.MemoryManagement.Threading;
using Binarysharp.MemoryManagement.Windows;

namespace Binarysharp.MemoryManagement
{
    /// <summary>
    ///     Class for memory editing a remote process.
    /// </summary>
    public class MemorySharp : IDisposable, IEquatable<MemorySharp>
    {
        #region Public Delegates/Events
        /// <summary>
        ///     Raises when the <see cref="MemorySharp" /> object is disposed.
        /// </summary>
        public event EventHandler OnDispose;
        #endregion

        #region Fields, Private Properties
        /// <summary>
        ///     The factories embedded inside the library.
        /// </summary>
        protected List<IFactory> Factories;

        /// <summary>
        ///     The Process Environment Block of the process.
        /// </summary>
        /// <remarks>The operation is deferred because it can be potentially slow.</remarks>
        protected Lazy<ManagedPeb> InternalPeb;
        #endregion

        #region Constructors, Destructors
        /// <summary>
        ///     Initializes a new instance of the <see cref="MemorySharp" /> class.
        /// </summary>
        /// <param name="process">Process to open.</param>
        public MemorySharp(Process process)
        {
            // Save the reference of the process
            Native = process;
            // Use the correct API depending on the architecture of the opened process
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (Architecture)
            {
                case ProcessArchitectures.Amd64:
                    NativeDriver = new NativeDriver64();
                    // TODO: PEB 64 ?
                    break;
                default:
                    NativeDriver = new NativeDriver32();
                    // Initialize the PEB
                    InternalPeb = new Lazy<ManagedPeb>(() => new ManagedPeb(this));
                    break;
            }
            // Open the process with all rights
            Handle = NativeDriver.MemoryCore.OpenProcess(ProcessAccessFlags.AllAccess, process.Id);
            // Create instances of the factories
            Factories = new List<IFactory>();
            Factories.AddRange(
                new IFactory[]
                {
                    Assembly = new AssemblyFactory(this),
                    Memory = new MemoryFactory(this),
                    Modules = new ModuleFactory(this),
                    Threads = new ThreadFactory(this),
                    Windows = new WindowFactory(this),
                    Patterns = new PatternFactory(this, Native.MainModule)
                });
            ImageBase = Native.MainModule.BaseAddress;
            MainModule = new RemoteModule(this, Native.MainModule);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MemorySharp" /> class.
        /// </summary>
        /// <param name="processId">Process id of the process to open.</param>
        public MemorySharp(int processId)
            : this(ApplicationFinder.FromProcessId(processId))
        {
        }

        /// <summary>
        ///     Frees resources and perform other cleanup operations before it is reclaimed by garbage collection.
        /// </summary>
        ~MemorySharp()
        {
            Dispose();
        }
        #endregion

        #region Public Properties, Indexers
        /// <summary>
        ///     Gets the architecture of the process.
        /// </summary>
        public ProcessArchitectures Architecture => ArchitectureHelper.GetArchitectureByProcess(Native);

        /// <summary>
        ///     Factory for generating assembly code.
        /// </summary>
        public AssemblyFactory Assembly { get; }

        /// <summary>
        ///     Gets the pattern factory.
        /// </summary>
        /// <value>
        ///     The pattern factory.
        /// </value>
        public PatternFactory Patterns { get; }

        /// <summary>
        ///     Gets whether the process is 32-bit.
        /// </summary>
        public bool Is32BitProcess => Architecture == ProcessArchitectures.Ia32;

        /// <summary>
        ///     Gets whether the process is 64-bit.
        /// </summary>
        public bool Is64BitProcess => Architecture == ProcessArchitectures.Amd64;

        /// <summary>
        ///     Gets whether the process is being debugged.
        /// </summary>
        public bool IsDebugged
        {
            get { return Peb.BeingDebugged; }
            set { Peb.BeingDebugged = value; }
        }

        /// <summary>
        ///     State if the process is running.
        /// </summary>
        public bool IsRunning => !Handle.IsInvalid && !Handle.IsClosed && !Native.HasExited;

        /// <summary>
        ///     Gets the main module as a <see cref="RemoteModule" /> instance.
        /// </summary>
        public RemoteModule MainModule { get; }

        /// <summary>
        ///     Gets the base address of the main <see cref="ProcessModule" />.
        /// </summary>
        /// <value>
        ///     The base address of the main <see cref="ProcessModule" />.
        /// </value>
        public IntPtr ImageBase { get; }

        /// <summary>
        ///     The remote process handle opened with all rights.
        /// </summary>
        public SafeMemoryHandle Handle { get; }

        /// <summary>
        ///     Factory for manipulating memory space.
        /// </summary>
        public MemoryFactory Memory { get; }

        /// <summary>
        ///     Factory for manipulating modules and libraries.
        /// </summary>
        public ModuleFactory Modules { get; }

        /// <summary>
        ///     Provide access to the opened process.
        /// </summary>
        public Process Native { get; }

        /// <summary>
        ///     Gets the native driver to interact with the API system/architecture dependant.
        /// </summary>
        public NativeDriverBase NativeDriver { get; }

        /// <summary>
        ///     The Process Environment Block of the process.
        /// </summary>
        public ManagedPeb Peb => InternalPeb.Value;

        /// <summary>
        ///     Gets the unique identifier for the remote process.
        /// </summary>
        public int Pid => Native.Id;

        /// <summary>
        ///     Gets the specified module in the remote process.
        /// </summary>
        /// <param name="moduleName">The name of module (not case sensitive).</param>
        /// <returns>A new instance of a <see cref="RemoteModule" /> class.</returns>
        public RemoteModule this[string moduleName] => Modules[moduleName];

        /// <summary>
        ///     Gets a pointer to the specified address in the remote process.
        /// </summary>
        /// <param name="address">The address pointed.</param>
        /// <param name="isRelative">[Optional] State if the address is relative to the main module.</param>
        /// <returns>A new instance of a <see cref="RemotePointer" /> class.</returns>
        public RemotePointer this[IntPtr address, bool isRelative = false]
            => new RemotePointer(this, isRelative ? MakeAbsolute(address) : address);

        /// <summary>
        ///     Factory for manipulating threads.
        /// </summary>
        public ThreadFactory Threads { get; }

        /// <summary>
        ///     Factory for manipulating windows.
        /// </summary>
        public WindowFactory Windows { get; }
        #endregion

        #region Interface Implementations
        /// <summary>
        ///     Releases all resources used by the <see cref="MemorySharp" /> object.
        /// </summary>
        public virtual void Dispose()
        {
            // Raise the event OnDispose
            OnDispose?.Invoke(this, new EventArgs());

            // Dispose all factories
            Factories.ForEach(factory => factory.Dispose());

            // Close the process handle
            Handle.Close();

            // Avoid the finalizer
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Returns a value indicating whether this instance is equal to a specified object.
        /// </summary>
        public bool Equals(MemorySharp other)
        {
            if (ReferenceEquals(null, other)) return false;
            return ReferenceEquals(this, other) || Handle.Equals(other.Handle);
        }
        #endregion

        /// <summary>
        ///     Determines whether the specified object is equal to the current object.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((MemorySharp) obj);
        }

        /// <summary>
        ///     Serves as a hash function for a particular type.
        /// </summary>
        public override int GetHashCode()
        {
            return Handle.GetHashCode();
        }

        /// <summary>
        ///     Makes an absolute address from a relative one based on the main module.
        /// </summary>
        /// <param name="address">The relative address.</param>
        /// <returns>The absolute address.</returns>
        public IntPtr MakeAbsolute(IntPtr address)
        {
            // Check if the relative address is not greater than the main module size
            if (address.ToInt64() > MainModule.Size)
                throw new ArgumentOutOfRangeException(nameof(address),
                    "The relative address cannot be greater than the main module size.");
            // Compute the absolute address
            return new IntPtr(ImageBase.ToInt64() + address.ToInt64());
        }

        /// <summary>
        ///     Makes a relative address from an absolute one based on the main module.
        /// </summary>
        /// <param name="address">The absolute address.</param>
        /// <returns>The relative address.</returns>
        public IntPtr MakeRelative(IntPtr address)
        {
            // Check if the absolute address is smaller than the main module base address
            if (address.ToInt64() < ImageBase.ToInt64())
                throw new ArgumentOutOfRangeException(nameof(address),
                    "The absolute address cannot be smaller than the main module base address.");
            // Compute the relative address
            return new IntPtr(address.ToInt64() - ImageBase.ToInt64());
        }


        /// <summary>
        ///     Executes the assembly code in the remote process.
        /// </summary>
        /// <param name="remoteCallParams">
        ///     The <see cref="RemoteCallParams" /> structure that contains the remote calls
        ///     <see cref="CallingConventions" /> type and the <see cref="IntPtr" /> address of where the remote call is located in
        ///     memory.
        /// </param>
        /// <param name="parameters">An array of parameters used to execute the assembly code.</param>
        /// <returns>The return value is the exit code of the thread created to execute the assembly code.</returns>
        public T Execute<T>(RemoteCallParams remoteCallParams, params dynamic[] parameters)
        {
            return Assembly.Execute<T>(remoteCallParams.Address, remoteCallParams.CallingConvention, parameters);
        }


        /// <summary>
        ///     Executes the assembly code in the remote process.
        /// </summary>
        /// <param name="remoteCallParams">
        ///     The <see cref="RemoteCallParams" /> structure that contains the remote calls
        ///     <see cref="CallingConventions" /> type and the <see cref="IntPtr" /> address of where the remote call is located in
        ///     memory.
        /// </param>
        /// <returns>The return value is the exit code of the thread created to execute the assembly code.</returns>
        public IntPtr Execute(RemoteCallParams remoteCallParams, params dynamic[] parameters)
        {
            return Assembly.Execute<IntPtr>(remoteCallParams.Address, remoteCallParams.CallingConvention, parameters);
        }

        /// <summary>
        ///     Executes the assembly code in the remote process.
        /// </summary>
        /// <param name="remoteCallParams">
        ///     The <see cref="RemoteCallParams" /> structure that contains the remote calls
        ///     <see cref="CallingConventions" /> type and the <see cref="IntPtr" /> address of where the remote call is located in
        ///     memory.
        /// </param>
        /// <returns>The return value is the exit code of the thread created to execute the assembly code.</returns>
        public IntPtr Execute(RemoteCallParams remoteCallParams)
        {
            return Assembly.Execute<IntPtr>(remoteCallParams.Address, remoteCallParams.CallingConvention);
        }


        /// <summary>
        ///     Executes the assembly code in the remote process.
        /// </summary>
        /// <param name="remoteCallParams">
        ///     The <see cref="RemoteCallParams" /> structure that contains the remote calls
        ///     <see cref="CallingConventions" /> type and the <see cref="IntPtr" /> address of where the remote call is located in
        ///     memory.
        /// </param>
        /// <returns>The return value is the exit code of the thread created to execute the assembly code.</returns>
        public T Execute<T>(RemoteCallParams remoteCallParams)
        {
            return Assembly.Execute<T>(remoteCallParams.Address, remoteCallParams.CallingConvention);
        }

        /// <summary>
        ///     Executes asynchronously the assembly code located in the remote process at the specified address.
        /// </summary>
        /// <param name="remoteCallParams">
        ///     The <see cref="RemoteCallParams" /> structure that contains the remote calls
        ///     <see cref="CallingConventions" /> type and the <see cref="IntPtr" /> address of where the remote call is located in
        ///     memory.
        /// </param>
        /// <param name="parameters">An array of parameters used to execute the assembly code.</param>
        /// <returns>
        ///     The return value is an asynchronous operation that return the exit code of the thread created to execute the
        ///     assembly code.
        /// </returns>
        public Task<T> ExecuteAsycn<T>(RemoteCallParams remoteCallParams, params dynamic[] parameters)
        {
            return Assembly.ExecuteAsync<T>(remoteCallParams.Address, remoteCallParams.CallingConvention, parameters);
        }


        /// <summary>
        ///     Executes asynchronously the assembly code located in the remote process at the specified address.
        /// </summary>
        /// <param name="parameters">An array of parameters used to execute the assembly code.</param>
        /// <param name="remoteCallParams">
        ///     The <see cref="RemoteCallParams" /> structure that contains the remote calls
        ///     <see cref="CallingConventions" /> type and the <see cref="IntPtr" /> address of where the remote call is located in
        ///     memory.
        /// </param>
        /// <returns>
        ///     The return value is an asynchronous operation that return the exit code of the thread created to execute the
        ///     assembly code.
        /// </returns>
        public Task ExecuteAsycn(RemoteCallParams remoteCallParams, params dynamic[] parameters)
        {
            return Assembly.ExecuteAsync<IntPtr>(remoteCallParams.Address, remoteCallParams.CallingConvention,
                parameters);
        }

        /// <summary>
        ///     Executes asynchronously the assembly code located in the remote process at the specified address.
        /// </summary>
        /// <returns>
        ///     The return value is an asynchronous operation that return the exit code of the thread created to execute the
        ///     assembly code.
        /// </returns>
        public Task ExecuteAsycn(RemoteCallParams remoteCallParams)
        {
            return Assembly.ExecuteAsync(remoteCallParams.Address, remoteCallParams.CallingConvention);
        }

        /// <summary>
        ///     Executes asynchronously the assembly code located in the remote process at the specified address.
        /// </summary>
        /// <returns>
        ///     The return value is an asynchronous operation that return the exit code of the thread created to execute the
        ///     assembly code.
        /// </returns>
        public Task ExecuteAsycn<T>(RemoteCallParams remoteCallParams)
        {
            return Assembly.ExecuteAsync<T>(remoteCallParams.Address, remoteCallParams.CallingConvention);
        }


        /// <summary>
        ///     Implements the operator ==.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        ///     The result of the operator.
        /// </returns>
        public static bool operator ==(MemorySharp left, MemorySharp right)
        {
            return Equals(left, right);
        }

        /// <summary>
        ///     Implements the operator !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        ///     The result of the operator.
        /// </returns>
        public static bool operator !=(MemorySharp left, MemorySharp right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        ///     Reads the value of a specified type in the remote process.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="address">The address where the value is read.</param>
        /// <param name="isRelative">[Optional] State if the address is relative to the main module.</param>
        /// <returns>A value.</returns>
        public T Read<T>(IntPtr address, bool isRelative = false)
        {
            return MarshalType<T>.ByteArrayToObject(ReadBytes(address, MarshalType<T>.Size, isRelative));
        }

        /// <summary>
        ///     Reads the value of a specified type in the remote process.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="address">The address where the value is read.</param>
        /// <param name="isRelative">[Optional] State if the address is relative to the main module.</param>
        /// <returns>A value.</returns>
        public T Read<T>(Enum address, bool isRelative = false)
        {
            return Read<T>(new IntPtr(Convert.ToInt64(address)), isRelative);
        }

        /// <summary>
        ///     Reads an array of a specified type in the remote process.
        /// </summary>
        /// <typeparam name="T">The type of the values.</typeparam>
        /// <param name="address">The address where the values is read.</param>
        /// <param name="count">The number of cells in the array.</param>
        /// <param name="isRelative">[Optional] State if the address is relative to the main module.</param>
        /// <returns>An array.</returns>
        public T[] Read<T>(IntPtr address, int count, bool isRelative = false)
        {
            // Allocate an array to store the results
            var array = new T[count];
            // Read the array in the remote process
            for (var i = 0; i < count; i++)
            {
                array[i] = Read<T>(address + MarshalType<T>.Size*i, isRelative);
            }
            return array;
        }

        /// <summary>
        ///     Reads an array of a specified type in the remote process.
        /// </summary>
        /// <typeparam name="T">The type of the values.</typeparam>
        /// <param name="address">The address where the values is read.</param>
        /// <param name="count">The number of cells in the array.</param>
        /// <param name="isRelative">[Optional] State if the address is relative to the main module.</param>
        /// <returns>An array.</returns>
        public T[] Read<T>(Enum address, int count, bool isRelative = false)
        {
            return Read<T>(new IntPtr(Convert.ToInt64(address)), count, isRelative);
        }

        /// <summary>
        ///     Reads an array of bytes in the remote process.
        /// </summary>
        /// <param name="address">The address where the array is read.</param>
        /// <param name="count">The number of cells.</param>
        /// <param name="isRelative">[Optional] State if the address is relative to the main module.</param>
        /// <returns>The array of bytes.</returns>
        public byte[] ReadBytes(IntPtr address, int count, bool isRelative = false)
        {
            return NativeDriver.MemoryCore.ReadBytes(Handle, isRelative ? MakeAbsolute(address) : address, count);
        }

        /// <summary>
        ///     Reads a string with a specified encoding in the remote process.
        /// </summary>
        /// <param name="address">The address where the string is read.</param>
        /// <param name="encoding">The encoding used.</param>
        /// <param name="isRelative">[Optional] State if the address is relative to the main module.</param>
        /// <param name="maxLength">
        ///     [Optional] The number of maximum bytes to read. The string is automatically cropped at this end
        ///     ('\0' char).
        /// </param>
        /// <returns>The string.</returns>
        public string ReadString(IntPtr address, Encoding encoding, bool isRelative = false, int maxLength = 512)
        {
            // Read the string
            var data = encoding.GetString(ReadBytes(address, maxLength, isRelative));
            // Search the end of the string
            var end = data.IndexOf('\0');
            // Crop the string with this end
            return data.Substring(0, end);
        }

        /// <summary>
        ///     Reads a string with a specified encoding in the remote process.
        /// </summary>
        /// <param name="address">The address where the string is read.</param>
        /// <param name="encoding">The encoding used.</param>
        /// <param name="isRelative">[Optional] State if the address is relative to the main module.</param>
        /// <param name="maxLength">
        ///     [Optional] The number of maximum bytes to read. The string is automatically cropped at this end
        ///     ('\0' char).
        /// </param>
        /// <returns>The string.</returns>
        public string ReadString(Enum address, Encoding encoding, bool isRelative = false, int maxLength = 512)
        {
            return ReadString(new IntPtr(Convert.ToInt64(address)), encoding, isRelative, maxLength);
        }

        /// <summary>
        ///     Reads a string using the encoding UTF8 in the remote process.
        /// </summary>
        /// <param name="address">The address where the string is read.</param>
        /// <param name="isRelative">[Optional] State if the address is relative to the main module.</param>
        /// <param name="maxLength">
        ///     [Optional] The number of maximum bytes to read. The string is automatically cropped at this end
        ///     ('\0' char).
        /// </param>
        /// <returns>The string.</returns>
        public string ReadString(IntPtr address, bool isRelative = false, int maxLength = 512)
        {
            return ReadString(address, Encoding.UTF8, isRelative, maxLength);
        }

        /// <summary>
        ///     Reads a string using the encoding UTF8 in the remote process.
        /// </summary>
        /// <param name="address">The address where the string is read.</param>
        /// <param name="isRelative">[Optional] State if the address is relative to the main module.</param>
        /// <param name="maxLength">
        ///     [Optional] The number of maximum bytes to read. The string is automatically cropped at this end
        ///     ('\0' char).
        /// </param>
        /// <returns>The string.</returns>
        public string ReadString(Enum address, bool isRelative = false, int maxLength = 512)
        {
            return ReadString(new IntPtr(Convert.ToInt64(address)), isRelative, maxLength);
        }

        /// <summary>
        ///     Writes the values of a specified type in the remote process.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="address">The address where the value is written.</param>
        /// <param name="value">The value to write.</param>
        /// <param name="isRelative">[Optional] State if the address is relative to the main module.</param>
        public void Write<T>(IntPtr address, T value, bool isRelative = false)
        {
            WriteBytes(address, MarshalType<T>.ObjectToByteArray(value), isRelative);
        }

        /// <summary>
        ///     Writes the values of a specified type in the remote process.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="address">The address where the value is written.</param>
        /// <param name="value">The value to write.</param>
        /// <param name="isRelative">[Optional] State if the address is relative to the main module.</param>
        public void Write<T>(Enum address, T value, bool isRelative = false)
        {
            Write(new IntPtr(Convert.ToInt64(address)), value, isRelative);
        }

        /// <summary>
        ///     Writes an array of a specified type in the remote process.
        /// </summary>
        /// <typeparam name="T">The type of the values.</typeparam>
        /// <param name="address">The address where the values is written.</param>
        /// <param name="array">The array to write.</param>
        /// <param name="isRelative">[Optional] State if the address is relative to the main module.</param>
        public void Write<T>(IntPtr address, T[] array, bool isRelative = false)
        {
            // Write the array in the remote process
            for (var i = 0; i < array.Length; i++)
            {
                Write(address + MarshalType<T>.Size*i, array[i], isRelative);
            }
        }

        /// <summary>
        ///     Writes an array of a specified type in the remote process.
        /// </summary>
        /// <typeparam name="T">The type of the values.</typeparam>
        /// <param name="address">The address where the values is written.</param>
        /// <param name="array">The array to write.</param>
        /// <param name="isRelative">[Optional] State if the address is relative to the main module.</param>
        public void Write<T>(Enum address, T[] array, bool isRelative = false)
        {
            Write(new IntPtr(Convert.ToInt64(address)), array, isRelative);
        }

        /// <summary>
        ///     Write an array of bytes in the remote process.
        /// </summary>
        /// <param name="address">The address where the array is written.</param>
        /// <param name="byteArray">The array of bytes to write.</param>
        /// <param name="isRelative">[Optional] State if the address is relative to the main module.</param>
        public void WriteBytes(IntPtr address, byte[] byteArray, bool isRelative = false)
        {
            // Change the protection of the memory to allow writable
            using (
                new MemoryProtection(this, isRelative ? MakeAbsolute(address) : address,
                    MarshalType<byte>.Size*byteArray.Length))
            {
                // Write the byte array
                NativeDriver.MemoryCore.WriteBytes(Handle, isRelative ? MakeAbsolute(address) : address, byteArray);
            }
        }

        /// <summary>
        ///     Writes a string with a specified encoding in the remote process.
        /// </summary>
        /// <param name="address">The address where the string is written.</param>
        /// <param name="text">The text to write.</param>
        /// <param name="encoding">The encoding used.</param>
        /// <param name="isRelative">[Optional] State if the address is relative to the main module.</param>
        public void WriteString(IntPtr address, string text, Encoding encoding, bool isRelative = false)
        {
            // Write the text
            WriteBytes(address, encoding.GetBytes(text + '\0'), isRelative);
        }

        /// <summary>
        ///     Writes a string with a specified encoding in the remote process.
        /// </summary>
        /// <param name="address">The address where the string is written.</param>
        /// <param name="text">The text to write.</param>
        /// <param name="encoding">The encoding used.</param>
        /// <param name="isRelative">[Optional] State if the address is relative to the main module.</param>
        public void WriteString(Enum address, string text, Encoding encoding, bool isRelative = false)
        {
            WriteString(new IntPtr(Convert.ToInt64(address)), text, encoding, isRelative);
        }

        /// <summary>
        ///     Writes a string using the encoding UTF8 in the remote process.
        /// </summary>
        /// <param name="address">The address where the string is written.</param>
        /// <param name="text">The text to write.</param>
        /// <param name="isRelative">[Optional] State if the address is relative to the main module.</param>
        public void WriteString(IntPtr address, string text, bool isRelative = false)
        {
            WriteString(address, text, Encoding.UTF8, isRelative);
        }

        /// <summary>
        ///     Writes a string using the encoding UTF8 in the remote process.
        /// </summary>
        /// <param name="address">The address where the string is written.</param>
        /// <param name="text">The text to write.</param>
        /// <param name="isRelative">[Optional] State if the address is relative to the main module.</param>
        public void WriteString(Enum address, string text, bool isRelative = false)
        {
            WriteString(new IntPtr(Convert.ToInt64(address)), text, isRelative);
        }
    }
}