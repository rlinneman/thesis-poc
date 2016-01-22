using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rel.Merge
{
    /// <summary>
    ///   Identifies an object which can merge arbitrary objects with
    ///   minimal boxing.
    /// </summary>
    public interface IMergeProvider
    {
        /// <summary>
        ///   Merges the given entities understanding the intent of the
        ///   merge by the kind specified.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="kind">The kind of merge.</param>
        /// <param name="before">The before image.</param>
        /// <param name="current">The current image.</param>
        /// <param name="after">The after image.</param>
        /// <returns>A resolution for the merge request.</returns>
        IMergeResolution<TEntity> Merge<TEntity>(MergeKind kind, TEntity before, TEntity current, TEntity after);
    }
}
