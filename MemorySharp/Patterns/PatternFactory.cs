﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Binarysharp.MemoryManagement.Internals;
using Binarysharp.MemoryManagement.Modules;
using Binarysharp.MemoryManagement.Native;
using Binarysharp.MemoryManagement.Patterns.Structs;

namespace Binarysharp.MemoryManagement.Patterns
{
    /// <summary>
    ///     A class for extracting data via pattern scanning process modules for patterns.
    /// </summary>
    public class PatternFactory : IFactory
    {
        #region Fields, Private Properties
        private byte[] InternalModuleData { get; set; }
        private MemorySharp MemorySharp { get; }
        private ProcessModule ProcessModule { get; }
        #endregion

        #region Constructors, Destructors
        /// <summary>
        ///     Initializes a new instance of the <see cref="PatternFactory" /> class.
        /// </summary>
        /// <param name="memorySharp">The memory sharp instance.</param>
        /// <param name="processModule">The process module the pattern data is contained in,</param>
        public PatternFactory(MemorySharp memorySharp, ProcessModule processModule)
        {
            MemorySharp = memorySharp;
            ProcessModule = processModule;
        }
        #endregion

        #region Public Properties, Indexers
        /// <summary>
        ///     A dump of the modules data as a byte array.
        /// </summary>
        public byte[] ModuleData => InternalModuleData ??
                                    (InternalModuleData =
                                        MemorySharp.ReadBytes(ProcessModule.BaseAddress, ProcessModule.ModuleMemorySize))
            ;
        #endregion

        #region Interface Implementations
        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // Nothing.
        }
        #endregion

        /// <summary>
        ///     Adds all pointers found from scanning a xml file to a given dictonary using the <code>IDictonary</code> interface.
        /// </summary>
        /// <param name="xmlFileNameOrPath">The name or path to the xml ProcessModulePattern file to use.</param>
        /// <param name="thePointerDictionary">The dictonary to fill.</param>
        public void CollectXmlScanResults(string xmlFileNameOrPath, IDictionary<string, IntPtr> thePointerDictionary)
        {
            var patterns = PatternCore.LoadXmlPatternFile(xmlFileNameOrPath);
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
            var patterns = PatternCore.LoadJsonPatternFile(xmlFileNameOrPath);
            foreach (var pattern in patterns)
            {
                thePointerDictionary.Add(pattern.Description, Find(pattern).Address);
            }
        }

        /// <summary>
        ///     Logs pattern scan results from n xml file or json file containing an array of <see cref="SerializablePattern" />
        ///     objects to a text file. The log should give ready-to-go formatting results from the pattern scan. An example result
        ///     logged would be <code>public IntPtr MyPattern {get;} = (IntPtr)0x0000;</code>
        /// </summary>
        /// <param name="xmlFileNameOrPath">The XML file name or path.</param>
        /// <param name="patternFileType">Type of the pattern file.</param>
        public void LogScanResultsToFile(string xmlFileNameOrPath, PatternFileType patternFileType)
        {
            var results = new Dictionary<string, IntPtr>();
            switch (patternFileType)
            {
                case PatternFileType.Xml:
                    CollectXmlScanResults(xmlFileNameOrPath, results);
                    foreach (var result in results)
                    {
                        PatternCore.LogScanResultToFile(result.Key, result.Value);
                    }
                    return;
                case PatternFileType.Json:
                    CollectJsonScanResults(xmlFileNameOrPath, results);
                    foreach (var result in results)
                    {
                        PatternCore.LogScanResultToFile(result.Key, result.Value);
                    }
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(patternFileType), patternFileType, null);
            }
        }

        /// <summary>
        ///     Performs a pattern scan from the data inside the <see cref="SerializablePattern" /> instance supplied in the
        ///     parameter.
        /// </summary>
        /// m>
        /// <param name="pattern">The <see cref="SerializablePattern" /> Instance containing the data to use.</param>
        /// <returns>A new <see cref="ScanResult" /></returns>
        public ScanResult Find(SerializablePattern pattern)
        {
            var bytes = PatternCore.GetBytesFromDwordPattern(pattern.TextPattern);
            var mask = PatternCore.GetMaskFromDwordPattern(pattern.TextPattern);
            return Find(bytes, mask, pattern.OffsetToAdd, pattern.IsOffsetMode, pattern.RebaseAddress);
        }

        /// <summary>
        ///     Performs a pattern scan.
        /// </summary>
        /// m>
        /// <param name="pattern">The <see cref="Pattern" /> Instance containing the data to use.</param>
        /// <returns>A new <see cref="ScanResult" /></returns>
        public ScanResult Find(Pattern pattern)
        {
            var bytes = PatternCore.GetBytesFromDwordPattern(pattern.TextPattern);
            var mask = PatternCore.GetMaskFromDwordPattern(pattern.TextPattern);
            return Find(bytes, mask, pattern.OffsetToAdd, pattern.IsOffsetMode, pattern.RebaseAddress);
        }

        /// <summary>
        ///     Performs a pattern scan.
        /// </summary>
        /// m>
        /// <param name="patternText">
        ///     The dword formatted text of the pattern.
        ///     <example>A2 5B ?? ?? ?? A2</example>
        /// </param>
        /// <param name="offsetToAdd">The offset to add to the offset result found from the pattern.</param>
        /// <param name="isOffsetMode">If the address is found from the base address + offset or not.</param>
        /// <param name="reBase">If the address should be rebased to this <see cref="RemoteModule" /> Instance's base address.</param>
        /// <returns>A new <see cref="ScanResult" /></returns>
        public ScanResult Find(string patternText, int offsetToAdd, bool isOffsetMode, bool reBase)
        {
            var bytes = PatternCore.GetBytesFromDwordPattern(patternText);
            var mask = PatternCore.GetMaskFromDwordPattern(patternText);
            return Find(bytes, mask, offsetToAdd, isOffsetMode, reBase);
        }

        /// <summary>
        ///     Preformpattern scan from byte[]
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="offsetToAdd"></param>
        /// <param name="isOffsetMode"></param>
        /// <param name="reBase"></param>
        /// <returns></returns>
        public ScanResult Find(byte[] pattern, int offsetToAdd, bool isOffsetMode, bool reBase)
        {
            var bytes = pattern;
            var mask = PatternCore.MaskFromPattern(pattern);
            return Find(bytes, mask, offsetToAdd, isOffsetMode, reBase);
        }


        /// <summary>
        ///     Performs a pattern scan.
        /// </summary>
        /// <param name="myPattern">The patterns bytes.</param>
        /// <param name="mask">The mask of the pattern. ? Is for wild card, x otherwise.</param>
        /// <param name="offsetToAdd">The offset to add to the offset result found from the pattern.</param>
        /// <param name="isOffsetMode">If the address is found from the base address + offset or not.</param>
        /// <param name="reBase">If the address should be rebased to this <see cref="RemoteModule" /> Instance's base address.</param>
        /// <returns>A new <see cref="ScanResult" /></returns>
        public ScanResult Find(byte[] myPattern, string mask, int offsetToAdd, bool isOffsetMode, bool reBase)
        {
            var patternBytes = myPattern;
            var patternMask = mask;
            var scanResult = PatternCore.Find(MemorySharp.Handle.DangerousGetHandle(), ProcessModule, patternBytes,
                patternMask, offsetToAdd, isOffsetMode, reBase);
            return scanResult;
        }
    }
}