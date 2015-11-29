﻿using System;
using System.IO;
using Binarysharp.MemoryManagement.Logging.Interfaces;

namespace Binarysharp.MemoryManagement.Logging.Defaults
{
    /// <summary>
    ///     A class class to handle writing logs to text files.
    /// </summary>
    public class FileLog : IManagedLog
    {
        #region Public Properties, Indexers
        /// <summary>
        ///     The streamwrite to use to write to the file log.
        /// </summary>
        protected StreamWriter StreamWriter { get; set; }


        /// <summary>
        ///     Gets the name of the current file log folder.
        /// </summary>
        /// <value>The name of the current file log folder.</value>
        protected internal string FolderName { get; set; }


        /// <summary>
        ///     Gets the name of the current file log.
        /// </summary>
        /// <value>The name of the current file log.</value>
        public string FileName { protected get; set; }


        /// <summary>
        ///     Gets or sets if the log should use formatted or raw text.
        /// </summary>
        /// <value> If the log should use formatted or raw text.</value>
        protected internal bool UseFormattedText { get; set; }

        /// <summary>
        ///     Gets the is disposed.
        /// </summary>
        /// <value>The is disposed.</value>
        public bool IsDisposed { get; set; } = false;

        /// <summary>
        ///     Gets the must be disposed.
        /// </summary>
        /// <value>The must be disposed.</value>
        public bool MustBeDisposed { get; set; } = true;

        /// <summary>
        ///     Gets the is enabled.
        /// </summary>
        /// <value>The is enabled.</value>
        public bool IsEnabled { get; set; }

        /// <summary>
        ///     Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }
        #endregion

        #region Interface Implementations
        /// <summary>
        ///     Logs the normal.
        /// </summary>
        /// <param name="message">The message.</param>
        public void LogNormal(string message)
        {
            StreamWriter.WriteLine(UseFormattedText ? FormatText("LogNormal", message) : message);
        }


        /// <summary>
        ///     Logs the error.
        /// </summary>
        /// <param name="message">The message.</param>
        public void LogError(string message)
        {
            StreamWriter.WriteLine(UseFormattedText ? FormatText("LogError", message) : message);
        }

        /// <summary>
        ///     Logs the information.
        /// </summary>
        /// <param name="message">The message.</param>
        public void LogInfo(string message)
        {
            StreamWriter.WriteLine(UseFormattedText ? FormatText("Information", message) : message);
        }


        /// <summary>
        ///     Logs the warning.
        /// </summary>
        /// <param name="message">The message.</param>
        public void LogWarning(string message)
        {
            StreamWriter.WriteLine(UseFormattedText ? FormatText("LogWarning", message) : message);
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            StreamWriter.Flush();
            StreamWriter.Close();
            StreamWriter.Dispose();
        }

        /// <summary>
        ///     Disables this instance.
        /// </summary>
        public void Disable()
        {
            IsEnabled = false;
            if (MustBeDisposed)
                Dispose();
        }

        /// <summary>
        ///     Enables the element.
        /// </summary>
        public void Enable()
        {
            FileName = FileName;
            FolderName = FolderName;
            UseFormattedText = UseFormattedText;
            Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FolderName));
            StreamWriter =
                new StreamWriter(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FolderName,
                                              $"{FileName}-{DateTime.Now:yyyy-MM-dd_hh-mm-ss}.txt"))
                {AutoFlush = true};
            IsEnabled = true;
        }
        #endregion

        #region Public Methods
        /// <summary>
        ///     Creates the specified log instance name.
        /// </summary>
        /// <param name="logInstanceName">Name of the log instance.</param>
        /// <param name="folderName">Name of the folder.</param>
        /// <param name="logFileName">Name of the log file.</param>
        /// <param name="useFormattedText">The use formatted text.</param>
        /// <param name="autoEnable">
        ///     States if the <see cref="StreamWriter" /> object should be created right away to allow logging
        ///     to files with out the need to call the <see cref="Enable()" /> method manually first.
        /// </param>
        public static FileLog Create(string logInstanceName, string folderName, string logFileName,
                                     bool useFormattedText, bool autoEnable)
        {
            var log = new FileLog
                      {
                          Name = logInstanceName,
                          FolderName = folderName,
                          FileName = logFileName,
                          UseFormattedText = useFormattedText
                      };
            if (autoEnable)
            {
                log.Enable();
            }
            return log;
        }
        #endregion

        #region Private Methods
        /// <summary>
        ///     Formats the text.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="text">The text.</param>
        /// <returns>System.String.</returns>
        private static string FormatText(string type, string text)
        {
            return $"{"["}{type}{"]"}{"["}{DateTime.Now.ToString("HH:mm:ss")}{"]: "}{text}";
        }
        #endregion
    }
}