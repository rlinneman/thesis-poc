using System.ComponentModel.DataAnnotations;

namespace Rel.Data.Bulk
{
    /// <summary>
    ///   Inspects that a change is well formed. The change may still
    ///   result in invalid data when applied.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    public static class ChangeValidator
    {
        /// <summary>
        ///   Provides a basic sanity check that a change item is well formed.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="context">The context.</param>
        /// <returns>
        ///   <see cref="P:System.ComponentModel.DataAnnotations.ValidationResult.Success"/>
        ///   if the change is valid; otherwise, a ValidationResult
        ///   denoting the reason the change failed validation.
        /// </returns>
        public static ValidationResult SanityCheck(ChangeItem item, ValidationContext context)
        {
            switch (item.Action)
            {
                case ChangeAction.Create:
                    if (item.GetBFIM() != null)
                        return new ValidationResult("BFIM not permitted for new entries.", new[] { "BFIM" });
                    if (item.GetAFIM() == null)
                        return new ValidationResult("AFIM required for new entries.", new[] { "AFIM" });
                    break;

                case ChangeAction.Update:
                    if (item.GetBFIM() == null)
                        return new ValidationResult("BFIM required for update entries.", new[] { "BFIM" });
                    if (item.GetAFIM() == null)
                        return new ValidationResult("AFIM required for update entries.", new[] { "AFIM" });
                    break;

                case ChangeAction.Delete:
                    if (item.GetBFIM() == null)
                        return new ValidationResult("BFIM required for new entries.", new[] { "BFIM" });
                    if (item.GetAFIM() != null)
                        return new ValidationResult("AFIM not permitted for new entries.", new[] { "AFIM" });
                    break;

                default:
                    return new ValidationResult("Invalid change action", new[] { "Action" });
            }

            return ValidationResult.Success;
        }
    }
}