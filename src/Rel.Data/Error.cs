using Rel.Data.Properties;
using System;
using System.ComponentModel.DataAnnotations;

namespace Rel.Data
{
    /// <summary>
    ///   A simple Exception factory utility.
    /// </summary>
    internal static class Error
    {
        /// <summary>
        ///   The error raised when an unauthenticated user attempts
        ///   modify status of a <see cref="T:Rel.Data.ILock"/>.
        /// </summary>
        /// <returns>An exception to be thrown.</returns>
        internal static Exception AnonymousUserAccess()
        {
            return new UnauthorizedAccessException(StringResources.AnonymousUserAccess);
        }

        /// <summary>
        ///   Identifies that entities in a change set failed validation.
        /// </summary>
        /// <param name="changeSet">The change set.</param>
        /// <param name="wasRedressed">
        ///   if set to <c>true</c> [was redressed].
        /// </param>
        /// <returns></returns>
        internal static Exception InvalidData(Bulk.ChangeSet changeSet, bool wasRedressed)
        {
            return new ValidationException(
                wasRedressed ?
                StringResources.InvalidSubmission :
                StringResources.BadMerge);
        }

        /// <summary>
        ///   Creates a new pessimistic lock exception.
        /// </summary>
        /// <returns></returns>
        internal static Exception PessimisticLock()
        {
            return new PessimisticConcurrencyException(StringResources.ErrorPessimisticLock);
        }
    }
}