using System.Globalization;
using System.Linq;

namespace SharpPlus.Memory.Modules
{
    /// <summary>
    /// A factory for pattenrs.
    /// </summary>
    public static class Patterns
    {
        /// <summary>
        ///     Gets the mask from a string based byte pattern to scan for.
        /// </summary>
        /// <param name="pattern">The string pattern to search for. ?? is mask and space between each byte and mask.</param>
        /// <returns>The mask from the pattern.</returns>
        public static string GetMaskFromDwordPattern(this string pattern)
        {
            var mask = pattern.Split(' ').Select(s => s.Contains('?') ? "?" : "x");

            return string.Concat(mask);
        }

        /// <summary>
        ///     Gets the byte[] pattern from string format patterns.
        /// </summary>
        /// <param name="pattern">The string pattern to search for. ?? is mask and space between each byte and mask.</param>
        /// <returns>An array of bytes.</returns>
        public static byte[] GetBytesFromDwordPattern(this string pattern)
        {
            return pattern.Split(' ').Select(s => s.Contains('?') ? (byte)0 : byte.Parse(s, NumberStyles.HexNumber)).ToArray();
        }
        /// <summary>
        /// Gets a new instance of the xml data pattern class.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="pattern">The pattern.</param>
        /// <param name="offset">The offset.</param>
        /// <returns><see cref="XmlDataPattern"/></returns>
        public static XmlDataPattern XmlData(string key, string pattern, int offset)
        {
            return new XmlDataPattern {Identifier = key, Pattern = pattern, Offset = offset};
        }

        /// <summary>
        /// Gets a new instance of the DataPattern class.
        /// </summary>
        /// <param name="pattern">The pattern.</param>
        /// <param name="offset">The offset.</param>
        /// <returns><see cref="DataPattern"/></returns>
        public static DataPattern Data(string pattern, int offset)
        {
            return new DataPattern {Pattern = pattern, Offset = offset };
        }
        /// <summary>
        /// Gets a new instance of the xml function pattern class.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="pattern">The pattern.</param>
        /// <returns><see cref=" XmlFunctionPattern"/></returns>
        public static XmlFunctionPattern XmlFunction(string key, string pattern)
        {
            return new XmlFunctionPattern { Identifier = key, Pattern = pattern };
        }

        /// <summary>
        /// Gets a new instance of the function pattern class.
        /// </summary>
        /// <param name="pattern">The pattern.</param>
        /// <returns><see cref="FunctionPattern"/></returns>
        public static FunctionPattern Function(string pattern)
        {
            return new FunctionPattern { Pattern = pattern };         
        }
    }
}