using System.Collections.Generic;

namespace Rel.Data.Bulk
{
    /// <summary>
    ///   Identifies a concurrent, conflicting update resolution
    ///   strategy provider.
    /// </summary>
    public interface IConflictResolver
    {
        /// <summary>
        ///   Attempts to resolve a conflict by inspecting the past,
        ///   current, and divergent state of an entity.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="repository">The repository.</param>
        /// <param name="change">The change.</param>
        /// <param name="index">
        ///   The index into repository for O(1) lookup.
        /// </param>
        /// <returns></returns>
        bool Resolve<TEntity, TKey>(IRepository<TEntity, TKey> repository, ChangeItem<TEntity> change, IDictionary<TKey, TEntity> index);
    }
}