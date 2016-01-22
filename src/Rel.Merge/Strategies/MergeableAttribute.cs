using System;

namespace Rel.Merge.Strategies
{
    /// <summary>
    ///   The base class for class and property decoration for merging.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
    public abstract class MergeableAttribute
        : Attribute
    {
        /// <summary>
        ///   Initializes a new instance of the
        ///   <see cref="MergeableAttribute"/> class.
        /// </summary>
        protected MergeableAttribute()
        {
        }

        /// <summary>
        ///   Merges the values given into the modified value.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="request">The request.</param>
        protected internal abstract void Merge<TValue>(MergeAction<TValue> request);
    }
}