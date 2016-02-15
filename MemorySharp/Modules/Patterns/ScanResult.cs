using System;
using System.Runtime.Remoting.Metadata.W3cXsd2001;

namespace SharpPlus.Memory.Modules
{
    /// <summary>
    ///     Contains data regarding a pattern scan result.
    /// </summary>
    public struct ScanResult
    {

        /// <summary>
        ///     The address found.
        /// </summary>
        public IntPtr Address { get; set; }

        /// <summary>
        ///     The offset found.
        /// </summary>
        public IntPtr Offset { get; set; }

        /// <summary>
        /// Makes the offset relative.
        /// </summary>
        /// <param name="base">The base.</param>
        /// <returns></returns>
        public IntPtr GetRelativeOffset(IntPtr @base)
        {
            return IntPtr(@base, Offset);
        }

        /// <summary>
        /// Makes the offset relative.
        /// </summary>
        /// <param name="base">The base.</param>
        /// <returns></returns>
        public IntPtr GetRelativeAddress(IntPtr @base)
        {
            return IntPtr(@base, Address);
        }

        private static IntPtr IntPtr(IntPtr @base, IntPtr additon)
        {
            try
            {
                return new IntPtr(@base.ToInt64() - additon.ToInt64());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return System.IntPtr.Zero;
            }
        }
    }
}