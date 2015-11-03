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
using System.IO;
using System.Linq;
using System.Reflection;
using Binarysharp.MemoryManagement.Extensions;
using Binarysharp.MemoryManagement.Windows;

namespace Binarysharp.MemoryManagement.Helpers
{
    /// <summary>
    ///     Static helper class providing tools for finding applications.
    /// </summary>
    public static class ApplicationFinder
    {
        #region  Fields

        private static readonly string _name = string.Empty;
        private static string _appDataFolderName = string.Empty;

        #endregion

        #region  Properties

        /// <summary>
        ///     Gets all top-level windows on the screen.
        /// </summary>
        public static IEnumerable<IntPtr> TopLevelWindows => WindowCore.EnumTopLevelWindows();

        /// <summary>
        ///     Gets all the windows on the screen.
        /// </summary>
        public static IEnumerable<IntPtr> Windows => WindowCore.EnumAllWindows();

        /// <summary>
        ///     Gets the application path.
        ///     <value>The application path.</value>
        /// </summary>
        public static string ApplicationPath
            => Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        /// <summary>
        ///     Gets the application version.
        /// </summary>
        public static Version ApplicationVersion => System.Reflection.Assembly.GetExecutingAssembly().
            GetName().Version;

        /// <summary>
        ///     Get default application name based on <see cref="Assembly.GetEntryAssembly()" />.<see cref="Assembly.GetName()" />.
        ///     <see cref="AssemblyName.Name" />.
        /// </summary>
        public static string DefaultApplicationName
        {
            get
            {
                var asm = System.Reflection.Assembly.GetEntryAssembly();
                return asm.IsNull() ? "Inflop.Common" : asm.GetName().Name;
            }
        }

        /// <summary>
        ///     Get or set application folder name in <see cref="Environment.SpecialFolder.ApplicationData" /> location.
        /// </summary>
        public static string AppDataFolderName
        {
            get
            {
                if (_appDataFolderName.IsEmpty())
                    _appDataFolderName = _name;

                return _appDataFolderName;
            }
            set { _appDataFolderName = value; }
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Returns a new <see cref="Process" /> component, given the identifier of a process.
        /// </summary>
        /// <param name="processId">The system-unique identifier of a process resource.</param>
        /// <returns>
        ///     A <see cref="Process" /> component that is associated with the local process resource identified by the
        ///     processId parameter.
        /// </returns>
        public static Process FromProcessId(int processId)
        {
            return Process.GetProcessById(processId);
        }

        /// <summary>
        ///     Get the version info of a <see cref="Process" />.
        /// </summary>
        /// <param name="process">The process to get information from.</param>
        /// <returns>The version information as a string.</returns>
        public static string GetVersionInfo(Process process)
        {
            return
                $"{process.MainModule.FileVersionInfo.FileDescription} {process.MainModule.FileVersionInfo.FileMajorPart}.{process.MainModule.FileVersionInfo.FileMinorPart}.{process.MainModule.FileVersionInfo.FileBuildPart} {process.MainModule.FileVersionInfo.FilePrivatePart}";
        }

        /// <summary>
        ///     Creates an collection of new <see cref="Process" /> components and associates them with all the process resources
        ///     that share the specified process name.
        /// </summary>
        /// <param name="processName">The friendly name of the process.</param>
        /// <returns>
        ///     A collection of type <see cref="Process" /> that represents the process resources running the specified
        ///     application or file.
        /// </returns>
        public static IEnumerable<Process> FromProcessName(string processName)
        {
            return Process.GetProcessesByName(processName);
        }

        /// <summary>
        ///     Creates a collection of new <see cref="Process" /> components and associates them with all the process resources
        ///     that share the specified class name.
        /// </summary>
        /// <param name="className">The class name string.</param>
        /// <returns>
        ///     A collection of type <see cref="Process" /> that represents the process resources running the specified
        ///     application or file.
        /// </returns>
        public static IEnumerable<Process> FromWindowClassName(string className)
        {
            return Windows.Where(window => WindowCore.GetClassName(window) == className).Select(FromWindowHandle);
        }

        /// <summary>
        ///     Retrieves a new <see cref="Process" /> component that created the window.
        /// </summary>
        /// <param name="windowHandle">A handle to the window.</param>
        /// <returns>
        ///     A <see cref="Process" />A <see cref="Process" /> component that is associated with the specified window
        ///     handle.
        /// </returns>
        public static Process FromWindowHandle(IntPtr windowHandle)
        {
            return FromProcessId(WindowCore.GetWindowProcessId(windowHandle));
        }

        /// <summary>
        ///     Creates a collection of new <see cref="Process" /> components and associates them with all the process resources
        ///     that share the specified window title.
        /// </summary>
        /// <param name="windowTitle">The window title string.</param>
        /// <returns>
        ///     A collection of type <see cref="Process" /> that represents the process resources running the specified
        ///     application or file.
        /// </returns>
        public static IEnumerable<Process> FromWindowTitle(string windowTitle)
        {
            return Windows.Where(window => WindowCore.GetWindowText(window) == windowTitle).Select(FromWindowHandle);
        }

        /// <summary>
        ///     Creates a collection of new <see cref="Process" /> components and associates them with all the process resources
        ///     that contain the specified window title.
        /// </summary>
        /// <param name="windowTitle">A part a window title string.</param>
        /// <returns>
        ///     A collection of type <see cref="Process" /> that represents the process resources running the specified
        ///     application or file.
        /// </returns>
        public static IEnumerable<Process> FromWindowTitleContains(string windowTitle)
        {
            return
                Windows.Where(window => WindowCore.GetWindowText(window).Contains(windowTitle))
                    .Select(FromWindowHandle);
        }

        #endregion
    }
}