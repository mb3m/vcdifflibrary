using System.IO;

namespace VcdiffLibrary
{
    /// <summary>
    /// Some internals IO related functions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This code is based on (is a copy of) CodeTable from Miscellaneous Utility Library
    /// (http://www.yoda.arachsys.com/csharp/miscutil/) written by Jon Skeet and Marc Gravell.
    /// </para>
    /// </remarks>
    internal sealed class IOUtils
    {
        /// <summary>
        /// Read <paramref name="size" /> bytes, throwing an error if <paramref name="stream" /> is not readable.
        /// </summary>
        internal static byte[] CheckedReadBytes(Stream stream, int size)
        {
            var ret = new byte[size];
            var index = 0;
            while (index < size)
            {
                var read = stream.Read(ret, index, size - index);
                if (read == 0)
                {
                    throw new EndOfStreamException(string.Format("End of stream reached with {0} byte{1} left to read", size - index, size - index == 1 ? "s" : ""));
                }

                index += read;
            }

            return ret;
        }

        /// <summary>
        /// Read one byte, throwing an error if <paramref name="stream" /> is not readable.
        /// </summary>
        internal static byte CheckedReadByte(Stream stream)
        {
            var b = stream.ReadByte();

            if (b == -1)
            {
                throw new EndOfStreamException("End of stream reached with 1 byte left to read");
            }

            return (byte)b;
        }

        /// <summary>
        /// Read an int encoded using the portable, variable-sized format used in vcdiff.
        /// </summary>
        /// <remarks>
        /// This format consists of a number in base 128, where each digit is encoded in the
        /// lower seven bits of a byte.
        /// Except for the least significant byte (the last when reading from left to right), other
        /// bytes have their most significant bit turned on to indicate that there are still more
        /// digits in the encoding.
        /// </remarks>
        internal static int ReadBigEndian7BitEncodedInt(Stream stream)
        {
            // we assume that an unsigned int is 5 bytes max
            const int maxLen = 5;
            const byte valueMask = 0x7f;
            const byte continuationMask = 0x80;

            var ret = 0;

            for (var i = 0; i < maxLen; i++)
            {
                var b = stream.ReadByte();
                if (b == -1)
                {
                    // we reached the end of the stream without any value, it's an unsupported state
                    throw new EndOfStreamException();
                }

                // we shift the value 128 to the left, then add the byte value (ignoring the continuation bit)
                ret = (ret << 7) | (b & valueMask);

                if ((b & continuationMask) == 0)
                {
                    // There a no more values
                    return ret;
                }
            }

            // More than 5 bytes ? Unsupported scenario
            throw new IOException("Invalid 7-bit encoded integer in stream.");
        }

        /// <summary>
        /// Read an int encoded using the portable, variable-sized format used in vcdiff.
        /// </summary>
        /// <remarks>
        /// This format consists of a number in base 128, where each digit is encoded in the
        /// lower seven bits of a byte.
        /// Except for the least significant byte (the last when reading from left to right), other
        /// bytes have their most significant bit turned on to indicate that there are still more
        /// digits in the encoding.
        /// </remarks>
        internal static int ReadBigEndian7BitEncodedInt(byte[] buffer, ref int index)
        {
            // we assume that an unsigned int is 5 bytes max
            const int maxLen = 5;
            const byte valueMask = 0x7f;
            const byte continuationMask = 0x80;

            var ret = 0;

            for (var i = 0; i < maxLen; i++)
            {
                // we suppose index and buffer are correct, we let any IndexOutOfRangeException propagate to caller
                var b = buffer[index];
                index++;

                // we shift the value 128 to the left, then add the byte value (ignoring the continuation bit)
                ret = (ret << 7) | (b & valueMask);

                if ((b & continuationMask) == 0)
                {
                    // There a no more values
                    return ret;
                }
            }

            // More than 5 bytes ? Unsupported scenario
            throw new IOException("Invalid 7-bit encoded integer in stream.");
        }
    }
}