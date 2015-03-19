using System;
using System.IO;

namespace VcdiffLibrary
{
    /// <summary>
    /// Decoding implementation of VCDIFF algorithm and file format, from <a href="http://tools.ietf.org/html/rfc3284">RFC 3284</a>.
    /// </summary>
    public class VcdiffDecoder
    {
        private const byte VCD_DECOMPRESS = 0x1;
        private const byte VCD_CODETABLE = 0x2;
        private const byte VCD_MASK = 0xf8;
        private const byte VCD_SOURCE = 0x1;
        private const byte VCD_TARGET = 0x2;

        private readonly Stream origin;
        private readonly Stream delta;
        private readonly Stream target;

        public VcdiffDecoder(Stream origin, Stream delta, Stream target)
        {
            if (origin == null) throw new ArgumentNullException("origin");
            if (delta == null) throw new ArgumentNullException("delta");
            if (target == null) throw new ArgumentNullException("target");

            if (!origin.CanRead || !origin.CanSeek) throw new ArgumentException("The origin stream must support reading and seeking", "origin");
            if (!delta.CanRead) throw new ArgumentException("The delta stream must support reading", "delta");
            if (!target.CanRead || !target.CanWrite || !target.CanSeek) throw new ArgumentException("The target stream must support reading, writing and seeking", "origin");

            this.origin = origin;
            this.delta = delta;
            this.target = target;
        }

        public void Decode()
        {
            ReadHeader();

            while (DecodeWindow()) {
                // TODO
            };
        }

        internal bool DecodeWindow()
        {
            // Win_Indicator
            var indicator = IOUtils.CheckedReadByte(delta);

            var fromSource = ((indicator & VCD_SOURCE) != 0);
            var fromTarget = ((indicator & VCD_TARGET) != 0);

            if (fromSource && fromTarget) {
                throw new VcdiffFormatException("Invalid window indicator : only one bit of VCD_SOURCE and VCD_TARGET must be set");
            }

            var sourceLength = 0;
            var sourcePosition = 0;
            if (fromSource || fromTarget) {
                sourceLength = IOUtils.ReadBigEndian7BitEncodedInt(delta);
                sourcePosition = IOUtils.ReadBigEndian7BitEncodedInt(delta);
            }

            return false;
        }

        internal void ReadHeader()
        {
            var header = IOUtils.CheckedReadBytes(delta, 4);

            if (header[0] != 0xd6 || header[1] != 0xc3 || header[2] != 0xc4)
            {
                throw new VcdiffFormatException("Invalid VCDIFF header in delta stream, should start with 0xd6c3c4");
            }

            if (header[3] != 0)
            {
                throw new VcdiffFormatException("Only version 0 delta stream are supported by this version");
            }

            // Load the header indicator
            var hdrIndicator = IOUtils.CheckedReadByte(delta);

            if ((hdrIndicator & VCD_MASK) != 0)
            {
                throw new VcdiffFormatException("Invalid header indicator - only bits 1 and 2 can be set.");
            }

            if ((hdrIndicator & VCD_DECOMPRESS) != 0)
            {
                throw new VcdiffFormatException("This implementation does not support delta stream with secondary compressors");
            }

            if ((hdrIndicator & VCD_CODETABLE) != 0)
            {
                throw new VcdiffFormatException("This implementation does not support application-defined code tables");
                // TODO ReadCodeTable();
            }
        }
    }
}
