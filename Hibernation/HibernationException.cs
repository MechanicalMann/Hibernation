using System;
using System.Runtime.Serialization;

namespace Hibernation
{
    /// <summary>
    /// Represents errors that occur during Hibernation session management.
    /// </summary>
    [Serializable]
    public class HibernationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the HibernationException class.
        /// </summary>
        public HibernationException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the HibernationException class
        /// with the specified error message.
        /// </summary>
        /// <param name="message">
        /// A message describing the error that occurred.
        /// </param>
        public HibernationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the HibernationException class
        /// with the specified error message and a reference to the inner
        /// exception that caused this exception.
        /// </summary>
        /// <param name="message">
        /// A message describing the error that occurred in Hibernation.
        /// </param>
        /// <param name="innerException">
        /// The inner exception that caused this exception.
        /// </param>
        public HibernationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the HibernationException class
        /// with serialized data.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected HibernationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Initializes a new instance of the HibernationException class
        /// with a formatted message and a reference to the inner exception
        /// that caused this exception.
        /// </summary>
        /// <param name="messageFormat">
        /// An optionally-formatted message describing the error that occurred.
        /// </param>
        /// <param name="innerException">
        /// The inner exception that caused this exception.
        /// </param>
        /// <param name="messageData">
        /// Optional data items that will replace any format string elements in
        /// the message parameter.
        /// </param>
        public HibernationException(string messageFormat, Exception innerException, params object[] messageData)
            : base(string.Format(messageFormat, messageData), innerException)
        {
        }
    }
}
