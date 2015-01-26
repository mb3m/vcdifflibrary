using System;
using System.IO;

namespace VcdiffLibrary
{
    public class VcdiffDecoder
    {
        private readonly Stream origin;
        private readonly Stream delta;
        private readonly Stream target;

        public VcdiffDecoder(Stream origin, Stream delta, Stream target)
        {
            if (origin == null) throw new ArgumentNullException("origin");
            if (delta == null) throw new ArgumentNullException("delta");
            if (target == null) throw new ArgumentNullException("target");

            this.origin = origin;
            this.delta = delta;
            this.target = target;
        }

        public void Decode()
        {
            this.ReadHeader();
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
            var headerIndicator = IOUtils.CheckedReadByte(delta);
            if ((headerIndicator & 1) != 0)
            {
                throw new VcdiffFormatException("This implementation does not support delta stream with secondary compressors");
            }

            var customCodeTable = ((headerIndicator & 2) != 0);
            var applicationHeader = ((headerIndicator & 4) != 0);

            if ((headerIndicator & 0xf8) != 0)
            {
                throw new VcdiffFormatException("Invalid header indicator - bits 3-7 not all zero.");
            }

            // Load the custom code table, if there is one
            if (customCodeTable)
            {
                ReadCodeTable();
            }

            // Ignore the application header if we have one. This tells xdelta3 what the right filenames are.
            if (applicationHeader)
            {
                int appHeaderLength = IOUtils.ReadBigEndian7BitEncodedInt(delta);
                IOHelper.CheckedReadBytes(delta, appHeaderLength);
            }
        }

        /// <summary>
        /// Reads the custom code table, if there is one
        /// </summary>
        void ReadCodeTable()
        {
            // The length given includes the nearSize and sameSize bytes
            int compressedTableLength = IOHelper.ReadBigEndian7BitEncodedInt(delta) - 2;
            int nearSize = IOHelper.CheckedReadByte(delta);
            int sameSize = IOHelper.CheckedReadByte(delta);
            byte[] compressedTableData = IOHelper.CheckedReadBytes(delta, compressedTableLength);

            byte[] defaultTableData = CodeTable.Default.GetBytes();

            MemoryStream tableOriginal = new MemoryStream(defaultTableData, false);
            MemoryStream tableDelta = new MemoryStream(compressedTableData, false);
            byte[] decompressedTableData = new byte[1536];
            MemoryStream tableOutput = new MemoryStream(decompressedTableData, true);
            VcdiffDecoder.Decode(tableOriginal, tableDelta, tableOutput);

            if (tableOutput.Position != 1536)
            {
                throw new VcdiffFormatException("Compressed code table was incorrect size");
            }

            codeTable = new CodeTable(decompressedTableData);
            cache = new AddressCache(nearSize, sameSize);
        }
    }
}