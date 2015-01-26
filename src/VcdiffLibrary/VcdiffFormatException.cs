// -----------------------------------------------------------------------
// <copyright file="VcdiffFormatException.cs" company="MB3M">
// Copyright (c) MB3M. Tous droits reserves.
// </copyright>
// -----------------------------------------------------------------------

namespace VcdiffLibrary
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class VcdiffFormatException : Exception
    {
        public VcdiffFormatException()
        {
        }

        public VcdiffFormatException(string message)
            : base(message)
        {
        }

        public VcdiffFormatException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected VcdiffFormatException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}