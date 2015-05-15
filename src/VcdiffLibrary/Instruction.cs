namespace VcdiffLibrary
{
    /// <summary>
    /// Contains the information for a single instruction
    /// </summary>
    /// <remarks>
    /// <para>
    /// This code is based on (is a copy of) CodeTable from Miscellaneous Utility Library
    /// (http://www.yoda.arachsys.com/csharp/miscutil/)/// written by Jon Skeet and Marc Gravell.
    /// </para>
    /// <para>
    /// See Section 5.4 of the RFC for documentation about how this structure works.
    /// </para>
    /// </remarks>
    internal struct Instruction
    {
        /// <summary>
        /// An "inst" field can have one of the four values: NOOP (0), ADD (1), RUN (2) or COPY (3) to indicate the instruction types.
        /// </summary>
        /// <remarks>
        /// NOOP means that no instruction is specified.
        /// In this case, both the corresponding size and mode fields will be zero.
        /// </remarks>
        private readonly InstructionType inst;

        /// <summary>
        /// A "size" field is zero or positive.
        /// A value zero means that the size associated with the instruction is encoded separately as an integer in the "Instructions and sizes section" (Section 6).
        /// A positive value for "size" defines the actual data size. 
        /// </summary>
        /// <remarks>
        /// Note that since the size is restricted to a byte, the maximum value for any instruction with size implicitly defined in the code table is 255.
        /// </remarks>
        private readonly byte size;

        /// <summary>
        /// A "mode" field is significant only when the associated delta instruction is a COPY.
        /// It defines the mode used to encode the associated addresses.
        /// For other instructions, this is always zero.
        /// </summary>
        private readonly byte mode;

        /// <summary>
        /// Initializes a new instance of <see cref="Instruction"/>.
        /// </summary>
        public Instruction(InstructionType inst, byte size, byte mode)
        {
            this.inst = inst;
            this.size = size;
            this.mode = mode;
        }

        /// <summary>
        /// Gets the instruction type.
        /// </summary>
        public InstructionType Inst
        {
            get { return inst; }
        }

        /// <summary>
        /// Gets the size of the instruction.
        /// 0 if the size is custom and must be retrived from the instructions and sizes section.
        /// </summary>
        public byte Size
        {
            get { return size; }
        }

        /// <summary>
        /// Gets the mode of the instruction, in the case when <see cref="Inst"/> is equals to <see cref="InstructionType.Copy"/>.
        /// See <see cref="AddressCache.DecodeAddress"/> and Section 5.3 for details about this value.
        /// </summary>
        public byte Mode
        {
            get { return mode; }
        }
    }
}