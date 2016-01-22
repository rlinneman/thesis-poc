using System;

namespace Rel.Merge.Strategies
{
    /// <summary>
    ///   Base implementation of merge attributes which should be
    ///   applied to numeric only field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public abstract class NumericMergeableAttribute : MergeableAttribute
    {
        /// <summary>
        ///   Merges the values given into the modified value.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="request">The request.</param>
        /// <exception cref="System.InvalidOperationException">
        ///   Property attributes should only be called during
        ///   conflicting updates.
        /// </exception>
        protected internal override void Merge<TValue>(MergeAction<TValue> request)
        {
            if (request.Kind != MergeKind.ConflictingUpdate)
                throw new InvalidOperationException("Property attributes should only be called during conflicting updates.");
        }

        /// <summary>
        ///   Converts the value to the specified output as double.
        /// </summary>
        /// <param name="src">The input value.</param>
        /// <param name="dest">The output value.</param>
        /// <returns>
        ///   <see langword="true"/> if translation was successful;
        ///   otherwise, <see langword="false"/>.
        /// </returns>
        protected virtual bool Coherse(object src, out double dest)
        {
            dest = 0;

            if (src != null)
            {
                dest = Convert.ToDouble(src);
                return true;
            }
            return false;
        }
    }
}