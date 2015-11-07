﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Binarysharp.MemoryManagement.LocalProcess.Extensions;
using Binarysharp.MemoryManagement.Native;
using Binarysharp.MemoryManagement.RemoteProcess;
using Binarysharp.MemoryManagement.RemoteProcess.Modules;

namespace Binarysharp.MemoryManagement.Patterns
{
    /// <summary>
    ///     Class containing tools and values to perform various types of ProcessModulePattern scans on the given
    ///     <see cref="System.Diagnostics.ProcessModule" /> Instance.
    /// </summary>
    public class ProcessModulePatternScanner
    {
        #region Fields, Private Properties
        private Lazy<byte[]> LazyProcesProccessModuleData { get; }
        #endregion

        #region Constructors, Destructors
        /// <summary>
        ///     Initializes a new instance of the <see cref="ProcessModulePatternScanner" /> class.
        /// </summary>
        /// <param name="processMemory">The process the process module is contained in.</param>
        /// <param name="processModule">The process module.</param>
        /// <param name="openHandle">if set to <c>true</c> [open handle].</param>
        public ProcessModulePatternScanner(Process processMemory, ProcessModule processModule, bool openHandle = true)
        {
            ProcessHandle = !openHandle
                ? processMemory.Handle
                : RemoteMemoryCore.OpenProcess(ProcessAccessFlags.AllAccess, processMemory.Id);
            ProcessModule = processModule;
            ProcessModuleAddress = processModule.BaseAddress;
            ProcessModuleSize = processModule.ModuleMemorySize;
            LazyProcesProccessModuleData =
                new Lazy<byte[]>(
                    (() => RemoteMemoryCore.ReadProcessMemory(ProcessHandle, ProcessModuleAddress, ProcessModuleSize)));
        }
        #endregion

        #region Public Properties, Indexers
        /// <summary>
        ///     Gets the open handle to the process.
        /// </summary>
        /// <value>The process handle.</value>
        public IntPtr ProcessHandle { get; }

        /// <summary>
        ///     Gets the process module reference for this instance.
        /// </summary>
        /// <value>The process module.</value>
        public ProcessModule ProcessModule { get; }

        /// <summary>
        ///     Gets the process module address in memory.
        /// </summary>
        /// <value>The process module address.</value>
        public IntPtr ProcessModuleAddress { get; }

        /// <summary>
        ///     Gets the size of the process modules data.
        /// </summary>
        /// <value>The size of the process module's data.</value>
        public int ProcessModuleSize { get; }
        #endregion

        /// <summary>
        ///     Adds all pointers found from scanning a xml file to a given dictonary using the <code>IDictonary</code> interface.
        /// </summary>
        /// <param name="xmlFileNameOrPath">The name or path to the xml ProcessModulePattern file to use.</param>
        /// <param name="thePointerDictionary">The dictonary to fill.</param>
        public void CollectXmlScanResults(string xmlFileNameOrPath, IDictionary<string, IntPtr> thePointerDictionary)
        {
            var patterns = PatternScanHelper.LoadXmlPatternFile(xmlFileNameOrPath);
            foreach (var pattern in patterns)
            {
                thePointerDictionary.Add(pattern.Description, Find(pattern).Address);
            }
        }


        /// <summary>
        ///     Adds all pointers found from scanning a json file to a given dictonary using the <code>IDictonary</code> interface.
        /// </summary>
        /// <param name="xmlFileNameOrPath">The name or path to the xml ProcessModulePattern file to use.</param>
        /// <param name="thePointerDictionary">The dictonary to fill.</param>
        public void CollectJsonScanResults(string xmlFileNameOrPath, IDictionary<string, IntPtr> thePointerDictionary)
        {
            var patterns = PatternScanHelper.LoadJsonPatternFile(xmlFileNameOrPath);
            foreach (var pattern in patterns)
            {
                thePointerDictionary.Add(pattern.Description, Find(pattern).Address);
            }
        }

        /// <summary>
        ///     Adds all pointers found from scanning an array of <see cref="SerializablePattern" /> objects to a given dictonary
        ///     using the
        ///     IDictonary interface.
        /// </summary>
        /// <param name="processModulePatterns"></param>
        /// <param name="thePointerDictionary">The dictonary to fill.</param>
        public void CollectScanResults(SerializablePattern[] processModulePatterns,
                                       IDictionary<string, IntPtr> thePointerDictionary)
        {
            if (processModulePatterns == null) throw new ArgumentNullException(nameof(processModulePatterns));
            foreach (var pattern in processModulePatterns)
            {
                thePointerDictionary.Add(pattern.Description, Find(pattern).Address);
            }
        }

        /// <summary>
        ///     Performs a ProcessModulePattern scan.
        /// </summary>
        /// <param name="pattern">The <see cref="SerializablePattern" /> Instance containing the data to use.</param>
        /// <returns>A new <see cref="ScanResult" /> instance.</returns>
        public ScanResult Find(SerializablePattern pattern)
        {
            return Find(pattern.TextPattern, pattern.OffsetToAdd,
                pattern.IsOffsetMode, pattern.RebaseAddress);
        }

        /// <summary>
        ///     Performs a ProcessModulePattern scan.
        /// </summary>
        /// <param name="pattern">The <see cref="SerializablePattern" /> Instance containing the data to use.</param>
        /// <returns>A new <see cref="ScanResult" /> instance.</returns>
        public ScanResult Find(Pattern pattern)
        {
            return Find(pattern.TextPattern, pattern.OffsetToAdd,
                pattern.IsOffsetMode, pattern.RebaseAddress);
        }

        /// <summary>
        ///     Performs a ProcessModulePattern scan.
        /// </summary>
        /// <param name="patternText">
        ///     The dword formatted text of the ProcessModulePattern.
        ///     <example>A2 5B ?? ?? ?? A2</example>
        /// </param>
        /// <param name="offsetToAdd">The offset to add to the offset result found from the ProcessModulePattern.</param>
        /// <param name="isOffsetMode">If the address is found from the base address + offset or not.</param>
        /// <param name="reBase">If the address should be rebased to this <see cref="RemoteModule" /> Instance's base address.</param>
        /// <returns>A new <see cref="ScanResult" /> instance.</returns>
        public ScanResult Find(string patternText, int offsetToAdd, bool isOffsetMode, bool reBase)
        {
            var bytes = PatternScanHelper.GetBytesFromDwordPattern(patternText);
            var mask = PatternScanHelper.GetMaskFromDwordPattern(patternText);
            return Find(bytes, mask, offsetToAdd, isOffsetMode, reBase);
        }

        /// <summary>
        ///     Performs a ProcessModulePattern scan.
        /// </summary>
        /// <param name="myPattern">The processModulePatterns bytes.</param>
        /// <param name="mask">The mask of the ProcessModulePattern. ? Is for wild card, x otherwise.</param>
        /// <param name="offsetToAdd">The offset to add to the offset result found from the ProcessModulePattern.</param>
        /// <param name="isOffsetMode">If the address is found from the base address + offset or not.</param>
        /// <param name="reBase">If the address should be rebased to this <see cref="RemoteModule" /> Instance's base address.</param>
        /// <returns>A new <see cref="ScanResult" /> instance.</returns>
        public ScanResult Find(byte[] myPattern, string mask, int offsetToAdd, bool isOffsetMode,
                               bool reBase)
        {
            var patternData = LazyProcesProccessModuleData.Value;
            var patternBytes = myPattern;
            var patternMask = mask;
            var result = new ScanResult();
            for (var offset = 0; offset < patternData.Length; offset++)
            {
                if (patternMask.Where((m, b) => m == 'x' && patternBytes[b] != patternData[b + offset]).Any()) continue;
                // If this area is reached, the ProcessModulePattern has been found.
                result.OriginalAddress = BytesToPointerObject(ProcessModuleAddress + offset + offsetToAdd);
                result.Address = result.OriginalAddress.Subtract(ProcessModuleAddress);
                result.Offset = offset;
                if (!isOffsetMode)
                {
                    result.Address = reBase ? ProcessModuleAddress.Add(result.Address) : result.Address;
                    return result;
                }
                result.Offset = reBase ? (int) ProcessModuleAddress.Add(offset) : result.Offset;
                result.Address = (IntPtr) result.Offset;
                return result;
            }
            // If this is reached, the ProcessModulePattern was not found.
            throw new Exception("The ProcessModulePattern " + "[" + myPattern + "]" + " was not found.");
        }

        /// <summary>
        ///     Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            ProcessModule.Dispose();
        }

        /// <summary>
        ///     Byteses to pointer object.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <returns>IntPtr.</returns>
        private IntPtr BytesToPointerObject(IntPtr address)
        {
            var byteData = RemoteMemoryCore.ReadProcessMemory(ProcessHandle, address, IntPtr.Size);
            unsafe
            {
                IntPtr ret;
                fixed (byte* pByte = byteData)
                    ret = new IntPtr(*(void**) pByte);
                return ret;
            }
        }
    }
}