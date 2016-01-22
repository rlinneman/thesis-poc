using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rel.Data.Bulk
{
    /// <summary>
    ///   Precludes concurrent edits of data. All updates must be
    ///   performed on current values.
    /// </summary>
    public sealed class RejectConcurrentEditsConflictResolver: IConflictResolver
    {
        /// <summary>
        ///   Rejects all concurrent updates.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="repository">The repository.</param>
        /// <param name="change">The change.</param>
        /// <returns><see langword="false"/> always.</returns>
        public bool Resolve<TEntity, TKey>(IRepository<TEntity, TKey> repository, ChangeItem<TEntity> change, IDictionary<TKey, TEntity> index)
        {
            return false;
        }
    }
}
