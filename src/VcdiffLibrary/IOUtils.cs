using System.IO;

namespace VcdiffLibrary
{
    /// <summary>
    /// Some internales IO related functions.
    /// Some methods are based on MiscUtil.Compression.Vcdiff.IOHelper class from MiscUtil project.
    /// </summary>
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

        internal static int ReadBigEndian7BitEncodedInt(Stream stream)
        {
            var ret = 0;
            for (var i = 0; i < 5; i++)
            {
                var b = stream.ReadByte();
                if (b == -1)
                {
                    throw new EndOfStreamException();
                }
                ret = (ret << 7) | (b & 0x7f);
                if ((b & 0x80) == 0)
                {
                    return ret;
                }
            }

            // Still haven't seen a byte with the high bit unset? Dodgy data.
            throw new IOException("Invalid 7-bit encoded integer in stream.");
        }
    }
}