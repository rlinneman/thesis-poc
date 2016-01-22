using System;
using System.Linq;
using System.Reflection;

namespace Rel.Merge
{
    /// <summary>
    ///   A wrapper for properties which are mergeable.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <typeparam name="TProperty">The type of the property.</typeparam>
    internal class MergePropertyWrapper<TEntity, TProperty>
        : PropertyWrapper<TEntity, TProperty>
    {
        private Strategies.MergeableAttribute[] mattrs;

        /// <summary>
        ///   Initializes a new instance of the
        ///   <see cref="MergePropertyWrapper{TEntity, TProperty}"/> class.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="mattrs">The merge attributes.</param>
        public MergePropertyWrapper(PropertyInfo info,
            Strategies.MergeableAttribute[] mattrs)
            : base(info)
        {
            this.mattrs = mattrs;
        }

        /// <summary>
        ///   Merges the specified entities.
        /// </summary>
        /// <param name="kind">
        ///   The kind of merge operation being attempted. Here for
        ///   signature compliance.
        /// </param>
        /// <param name="baseValue">The base value.</param>
        /// <param name="current">The current.</param>
        /// <param name="modifiedValue">The modified value.</param>
        /// <returns>An intermediate result with a commit callback.</returns>
        public PendingMergeResolution Merge(MergeKind kind,
            TEntity baseValue,
            TEntity current,
            TEntity modifiedValue)
        {
            TProperty
                b = Get(baseValue),
                c = Get(current),
                n = Get(modifiedValue);

            var action = new MergeAction<TProperty>(kind, b, c, n);

            if (current == null)
                current = modifiedValue;

            mattrs.FirstOrDefault(_ =>
            {
                _.Merge(action);
                return action.Resolved;
            });

            if (action.Resolved)
                return new PendingMergeResolution(action.Result,
                    new Action(() => Set(current, Get(modifiedValue))));

            return new PendingMergeResolution(action.Result, Noop);
        }

        /// <summary>
        ///   Convenience no operation method.
        /// </summary>
        private static void Noop()
        {
        }
    }
}