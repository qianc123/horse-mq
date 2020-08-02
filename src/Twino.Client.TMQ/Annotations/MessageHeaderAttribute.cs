using System;

namespace Twino.Client.TMQ.Annotations
{
    /// <summary>
    /// Used to add header information for message types
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class MessageHeaderAttribute : Attribute
    {
        /// <summary>
        /// Header key
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Header value
        /// </summary>
        public String Value { get; }

        /// <summary>
        /// Creates new message header attribute
        /// </summary>
        public MessageHeaderAttribute(string key, string value)
        {
            Key = key;
            Value = value;
        }
    }
}