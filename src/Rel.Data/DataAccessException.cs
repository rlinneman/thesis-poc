﻿using System;
using System.Runtime.Serialization;

namespace Rel.Data
{
    /// <summary>
    ///   Denotes an error in the data processing subsystem.
    /// </summary>
    [Serializable]
    public class DataAccessException : Exception
    {
        /// <summary>
        ///   Initializes a new instance of the
        ///   <see cref="DataAccessException"/> class.
        /// </summary>
        public DataAccessException()
            : base()
        {
        }

        /// <summary>
        ///   Initializes a new instance of the
        ///   <see cref="DataAccessException"/> class.
        /// </summary>
        /// <param name="message">
        ///   A message that describes the error.
        /// </param>
        public DataAccessException(string message)
            : base(message)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the
        ///   <see cref="DataAccessException"/> class.
        /// </summary>
        /// <param name="message">
        ///   The error message that explains the reason for the exception.
        /// </param>
        /// <param name="innerException">
        ///   The exception that is the cause of the current
        ///   exception. If the <paramref name="innerException"/>
        ///   parameter is not a null reference, the current exception
        ///   is raised in a catch block that handles the inner exception.
        /// </param>
        public DataAccessException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the
        ///   <see cref="DataAccessException"/> class.
        /// </summary>
        /// <param name="info">
        ///   The object that holds the serialized object data.
        /// </param>
        /// <param name="context">
        ///   The contextual information about the source or destination.
        /// </param>
        protected DataAccessException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}