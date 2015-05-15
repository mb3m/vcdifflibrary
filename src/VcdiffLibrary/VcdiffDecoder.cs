using System;
using System.IO;

namespace VcdiffLibrary
{
    /// <summary>
    /// Decoding implementation of VCDIFF algorithm and file format, from <a href="http://tools.ietf.org/html/rfc3284">RFC 3284</a>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This code is based on (is a copy of) CodeTable from Miscellaneous Utility Library
    /// (http://www.yoda.arachsys.com/csharp/miscutil/) written by Jon Skeet and Marc Gravell.
    /// </para>
    /// <para>
    /// I plan to update it later, and include an encoder, but for now it's just a copy with some cosmetic changes.
    /// </para>
    /// </remarks>
    public class VcdiffDecoder
    {
        // header
        private const byte VCD_DECOMPRESS = 0x1;
        private const byte VCD_CODETABLE = 0x2;
        private const byte VCD_MASK = 0xf8;

        // Win_indicator
        private const byte VCD_SOURCE = 0x1;
        private const byte VCD_TARGET = 0x2;

        // Delta_indicator
        private const byte VCD_DATACOMP = 0x1;
        private const byte VCD_INSTCOMP = 0x2;
        private const byte VCD_ADDRCOMP = 0x4;

        private readonly Stream origin;
        private readonly Stream delta;
        private readonly Stream target;

        /// <summary>
        /// Code table to use for decoding.
        /// </summary>
        private CodeTable codeTable = CodeTable.Default;

        /// <summary>
        /// Address cache to use when decoding; must be reset before decoding each window.
        /// Default to the default size.
        /// </summary>
        private AddressCache cache;

        public VcdiffDecoder(Stream origin, Stream delta, Stream target)
        {
            if (origin == null) throw new ArgumentNullException("origin");
            if (delta == null) throw new ArgumentNullException("delta");
            if (target == null) throw new ArgumentNullException("target");

            if (!origin.CanRead || !origin.CanSeek) throw new ArgumentException("The origin stream must support reading and seeking", "origin");
            if (!delta.CanRead) throw new ArgumentException("The delta stream must support reading", "delta");
            if (!target.CanWrite || !target.CanSeek) throw new ArgumentException("The target stream must support writing and seeking", "target");

            this.origin = origin;
            this.delta = delta;
            this.target = target;
        }

        public void Decode()
        {
            ReadHeader();

            while (DecodeWindow())
            {
            }
        }

        private void ReadHeader()
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
                ReadCodeTable();
            }
            else
            {
                cache = new AddressCache(4, 3);
            }
        }

        /// <summary>
        /// Reads the custom code table, if there is one
        /// </summary>
        private void ReadCodeTable()
        {
            // The length given includes the nearSize and sameSize bytes
            var compressedTableLength = IOUtils.ReadBigEndian7BitEncodedInt(delta) - 2;

            var nearSize = IOUtils.CheckedReadByte(delta);
            var sameSize = IOUtils.CheckedReadByte(delta);

            var compressedTableData = IOUtils.CheckedReadBytes(delta, compressedTableLength);

            var defaultTableData = CodeTable.Default.GetBytes();

            var decompressedTableData = new byte[CodeTable.TableSize];

            using (var tableOriginal = new MemoryStream(defaultTableData, false))
            using (var tableDelta = new MemoryStream(compressedTableData, false))
            using (var tableOutput = new MemoryStream(decompressedTableData, true))
            {
                var innerDecoder = new VcdiffDecoder(tableOriginal, tableDelta, tableOutput);
                innerDecoder.Decode();

                if (tableOutput.Position != CodeTable.TableSize)
                {
                    throw new VcdiffFormatException("Compressed code table was incorrect size");
                }
            }

            codeTable = new CodeTable(decompressedTableData);
            cache = new AddressCache(nearSize, sameSize);
        }

        private bool DecodeWindow()
        {
            // Win_Indicator
            var winIndicator = IOUtils.CheckedReadByte(delta);

            var fromSource = ((winIndicator & VCD_SOURCE) != 0);
            var fromTarget = ((winIndicator & VCD_TARGET) != 0);

            if (fromSource && fromTarget)
            {
                throw new VcdiffFormatException("Invalid window indicator : only one bit of VCD_SOURCE and VCD_TARGET must be set");
            }

            Stream sourceStream = null;
            var sourceStreamPostionBak = -1;

            if (fromSource)
            {
                sourceStream = origin;
            }
            else if (fromTarget)
            {
                sourceStream = target;
                sourceStreamPostionBak = (int)target.Position;
            }

            // Read the source data, if any
            byte[] sourceData = null;
            var sourceLength = 0;
            if (sourceStream != null)
            {
                // Read source position and 
                sourceLength = IOUtils.ReadBigEndian7BitEncodedInt(delta);
                var sourcePosition = IOUtils.ReadBigEndian7BitEncodedInt(delta);

                sourceStream.Position = sourcePosition;
                sourceData = IOUtils.CheckedReadBytes(sourceStream, sourceLength);

                // Reposition the source stream if appropriate
                if (sourceStreamPostionBak != -1)
                {
                    sourceStream.Position = sourceStreamPostionBak;
                }
            }

            // Read how long the delta encoding is - then ignore it
            IOUtils.ReadBigEndian7BitEncodedInt(delta);

            // Read how long the target window is
            var targetLength = IOUtils.ReadBigEndian7BitEncodedInt(delta);

            var targetData = new byte[targetLength];
            using (var targetDataStream = new MemoryStream(targetData, true))
            {
                var deltaIndicator = IOUtils.CheckedReadByte(delta);
                if (deltaIndicator != 0)
                {
                    throw new VcdiffFormatException("This implementation does not support delta stream with secondary compressors");
                }

                var addRunLength = IOUtils.ReadBigEndian7BitEncodedInt(delta);

                var instructionsLength = IOUtils.ReadBigEndian7BitEncodedInt(delta);

                var addrLength = IOUtils.ReadBigEndian7BitEncodedInt(delta);

                // ADD/RUN data section, containing all unmatched data for the ADD and RUN instructions
                var addRunData = IOUtils.CheckedReadBytes(delta, addRunLength);

                // List of instructions and their sizes section
                var instructions = IOUtils.CheckedReadBytes(delta, instructionsLength);

                // Initialize AddressCache with addresses section for COPY
                cache.Init(IOUtils.CheckedReadBytes(delta, addrLength));

                var addRunDataIndex = 0;

                var instructionStream = new MemoryStream(instructions, false);

                while (true)
                {
                    var instructionIndex = instructionStream.ReadByte();
                    if (instructionIndex == -1)
                    {
                        break;
                    }

                    for (var i = 0; i < 2; i++)
                    {
                        var instruction = codeTable[instructionIndex, i];

                        // 1st argument : Instruction size
                        int size = instruction.Size;
                        if (size == 0 && instruction.Inst != InstructionType.NoOp)
                        {
                            // Custom size, read from stream
                            size = IOUtils.ReadBigEndian7BitEncodedInt(instructionStream);
                        }

                        switch (instruction.Inst)
                        {
                            case InstructionType.NoOp:
                                break;

                            case InstructionType.Add:
                                targetDataStream.Write(addRunData, addRunDataIndex, size);
                                addRunDataIndex += size;
                                break;

                            case InstructionType.Copy:
                                // 5.3 Encoding of COPY instruction addresses

                                // address of the current location in the target data
                                var here = (int)targetDataStream.Position + sourceLength;

                                // address of the COPY instruction to execute
                                var addr = cache.DecodeAddress(here, instruction.Mode);

                                if (sourceData != null && addr < sourceData.Length)
                                {
                                    // Regular copy
                                    targetDataStream.Write(sourceData, addr, size);
                                }
                                else
                                {
                                    // Data is in target data : see Section 3. for an example of COPY overlapping

                                    // Get rid of the offset
                                    addr -= sourceLength;

                                    // Can we just ignore overlap issues ?
                                    if (addr + size < targetDataStream.Position)
                                    {
                                        targetDataStream.Write(targetData, addr, size);
                                    }
                                    else
                                    {
                                        // No, we must write byte one by one, because we copy a repeating pattern
                                        for (var j = 0; j < size; j++)
                                        {
                                            targetDataStream.WriteByte(targetData[addr++]);
                                        }
                                    }
                                }
                                break;

                            case InstructionType.Run:
                                var data = addRunData[addRunDataIndex++];
                                for (var j = 0; j < size; j++)
                                {
                                    targetDataStream.WriteByte(data);
                                }
                                break;

                            default:
                                throw new VcdiffFormatException("Invalid instruction type found.");
                        }
                    }
                }
            }

            // Write decoded datas
            target.Write(targetData, 0, targetLength);

            return false;
        }
    }
}