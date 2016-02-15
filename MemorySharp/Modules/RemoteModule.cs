/*
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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Binarysharp.MemoryManagement.Memory;
using Binarysharp.MemoryManagement.Native;
using SharpPlus.Memory.Modules;

namespace Binarysharp.MemoryManagement.Modules
{
    /// <summary>
    ///     Class repesenting a module in the remote process.
    /// </summary>
    public class RemoteModule : RemoteRegion
    {
        #region Fields, Private Properties
        /// <summary>
        ///     The dictionary containing all cached functions of the remote module.
        /// </summary>
        internal static readonly IDictionary<Tuple<string, SafeMemoryHandle>, RemoteFunction> CachedFunctions =
            new Dictionary<Tuple<string, SafeMemoryHandle>, RemoteFunction>();

        private Lazy<byte[]> LazyData { get; }
        #endregion

        #region Constructors, Destructors
        /// <summary>
        ///     Initializes a new instance of the <see cref="RemoteModule" /> class.
        /// </summary>
        /// <param name="memorySharp">The reference of the <see cref="MemorySharp" /> object.</param>
        /// <param name="module">The native <see cref="ProcessModule" /> object corresponding to this module.</param>
        internal RemoteModule(MemoryBase memorySharp, ProcessModule module) : base(memorySharp, module.BaseAddress)
        {
            // Save the parameter
            Native = module;
            LazyData =
                new Lazy<byte[]>(
                    () => MemoryCore.ReadBytes(memorySharp.Handle, module.BaseAddress, module.ModuleMemorySize));
        }
        #endregion

        #region Public Properties, Indexers
        /// <summary>
        ///     State if this is the main module of the remote process.
        /// </summary>
        public bool IsMainModule => MemorySharp.Process.MainModule.BaseAddress == BaseAddress;

        /// <summary>
        ///     Gets if the <see cref="RemoteModule" /> is valid.
        /// </summary>
        public override bool IsValid
        {
            get
            {
                return base.IsValid &&
                    MemorySharp.Process.Modules.Cast<ProcessModule>()
                        .Any(m => m.BaseAddress == BaseAddress && m.ModuleName == Name);
            }
        }

        /// <summary>
        ///     Gets the modules data as an array of bytes.
        /// </summary>
        /// <value>
        ///     The modules data as a <see cref="byte" /> array.
        /// </value>
        public byte[] Data => LazyData.Value;

        /// <summary>
        ///     The name of the module.
        /// </summary>
        public string Name => Native.ModuleName;

        /// <summary>
        ///     The native <see cref="ProcessModule" /> object corresponding to this module.
        /// </summary>
        public ProcessModule Native { get; }

        /// <summary>
        ///     The full path of the module.
        /// </summary>
        public string Path => Native.FileName;

        /// <summary>
        ///     The size of the module in the memory of the remote process.
        /// </summary>
        public int Size => Native.ModuleMemorySize;

        /// <summary>
        ///     Gets the specified function in the remote module.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>A new instance of a <see cref="RemoteFunction" /> class.</returns>
        public RemoteFunction this[string functionName] => FindFunction(functionName);
        #endregion

        #region Public Methods
        /// <summary>
        ///     Finds the data pattern.
        /// </summary>
        /// <param name="patternClass">The pattern class.</param>
        /// <returns></returns>
        /// <exception cref="Exception">The pattern  + [ + bytes.Length + ]  + mask +  was not found.</exception>
        public ScanResult FindDataPattern(DataPattern patternClass)
        {
            var patternData = LazyData.Value;
            var patternDataLength = patternData.Length;
            var bytes = patternClass.Pattern.GetBytesFromDwordPattern();
            var mask = patternClass.Pattern.GetMaskFromDwordPattern();

            for (var offset = 0; offset < patternDataLength; offset++)
            {
                if (mask.Where((m, b) => m == 'x' && bytes[b] != patternData[b + offset]).Any())
                    continue;
                return new ScanResult { Address = MemorySharp.Read<IntPtr>(BaseAddress + offset), Offset = (IntPtr) offset };
            }
            // If this is reached, the pattern was not found.
            throw new Exception("The pattern " + "[" + bytes.Length + "] " + mask + " was not found.");
        }

        /// <summary>
        /// Finds the function pattern.
        /// </summary>
        /// <param name="patternClass">The pattern class.</param>
        /// <returns></returns>
        /// <exception cref="Exception">The pattern  + [ + pattern.Length + ]  + mask +  was not found.</exception>
        public ScanResult FindFunctionPattern(FunctionPattern patternClass)
        {
            var patternData = LazyData.Value;
            var patternDataLength = patternData.Length;
            var pattern = patternClass.Pattern.GetBytesFromDwordPattern();
            var mask = patternClass.Pattern.GetMaskFromDwordPattern();
            for (var offset = 0; offset < patternDataLength; offset++)
            {
                if (mask.Where((m, b) => m == 'x' && pattern[b] != patternData[b + offset]).Any())
                    continue;
                return new ScanResult { Address = BaseAddress + offset, Offset = (IntPtr) offset };
            }
            // If this is reached, the pattern was not found.
            throw new Exception("The pattern " + "[" + pattern.Length + "] " + mask + " was not found.");
        }
        /// <summary>
        ///     Finds the specified function in the remote module.
        /// </summary>
        /// <param name="functionName">The name of the function (case sensitive).</param>
        /// <returns>A new instance of a <see cref="RemoteFunction" /> class.</returns>
        /// <remarks>
        ///     Interesting article on how DLL loading works: http://msdn.microsoft.com/en-us/magazine/bb985014.aspx
        /// </remarks>
        public RemoteFunction FindFunction(string functionName)
        {
            // Create the tuple
            var tuple = Tuple.Create(functionName, MemorySharp.Handle);

            // Check if the function is already cached
            if (CachedFunctions.ContainsKey(tuple))
                return CachedFunctions[tuple];

            // If the function is not cached
            // Check if the local process has this module loaded
            var localModule =
                Process.GetCurrentProcess()
                    .Modules.Cast<ProcessModule>()
                    .FirstOrDefault(m => string.Equals(m.FileName, Path, StringComparison.CurrentCultureIgnoreCase));
            var isManuallyLoaded = false;

            try
            {
                // If this is not the case, load the module inside the local process
                if (localModule == null)
                {
                    isManuallyLoaded = true;
                    localModule = ModuleCore.LoadLibrary(Native.FileName);
                }

                // Get the offset of the function
                var offset = ModuleCore.GetProcAddress(localModule, functionName).ToInt64() -
                    localModule.BaseAddress.ToInt64();

                // Rebase the function with the remote module
                var function = new RemoteFunction(MemorySharp, new IntPtr(Native.BaseAddress.ToInt64() + offset),
                    functionName);

                // Store the function in the cache
                CachedFunctions.Add(tuple, function);

                // Return the function rebased with the remote module
                return function;
            }
            finally
            {
                // Free the module if it was manually loaded
                if (isManuallyLoaded)
                    ModuleCore.FreeLibrary(localModule);
            }
        }

        /// <summary>
        ///     Returns a string that represents the current object.
        /// </summary>
        public override string ToString()
        {
            return $"BaseAddress = 0x{BaseAddress.ToInt64():X} Name = {Name}";
        }
        #endregion

        /// <summary>
        ///     Frees the loaded dynamic-link library (DLL) module and, if necessary, decrements its reference count.
        /// </summary>
        /// <param name="memorySharp">The reference of the <see cref="MemorySharp" /> object.</param>
        /// <param name="module">The module to eject.</param>
        internal static void InternalEject(MemoryBase memorySharp, RemoteModule module)
        {
            // Call FreeLibrary remotely
            memorySharp.Threads.CreateAndJoin(memorySharp["kernel32"]["FreeLibrary"].BaseAddress, module.BaseAddress);
        }
    }
}