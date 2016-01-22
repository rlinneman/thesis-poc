using Rel.Merge.Properties;
using System;
using System.Globalization;

namespace Rel.Merge
{
    /// <summary>
    ///   An exception factory utility.
    /// </summary>
    internal static class Error
    {
        /// <summary>
        ///   Raised when an attempt is made to merge an entity type
        ///   which does not support optimistic concurrency.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        internal static Exception MergeNotSupported(Type type)
        {
            return new NotSupportedException(Format(StringResources.OptimisitcConcurrencyNotSupported, type.FullName));
        }

        /// <summary>
        ///   Shorthand wrapper for String.Format.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>A formatted string.</returns>
        private static string Format(string format, params object[] args)
        {
            return string.Format(CultureInfo.CurrentCulture, format, args);
        }
    }
}