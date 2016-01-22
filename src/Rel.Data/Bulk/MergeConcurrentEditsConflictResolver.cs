using Rel.Merge;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rel.Data.Bulk
{
    /// <summary>
    ///   Resolves conflicting writes using merge strategies.
    /// </summary>
    /// <remarks>
    ///   Logic considerations
    ///
    ///     |       HAS VALUE       |
    ///   # | BFIM | Current | AFIM | Represents
    ///   --+------+---------+-------------------------------------------
    ///   0 |  T   |    T    |  T   | normal OCC update 
    ///   1 |  T   |    T    |  F   | normal OCC delete
    ///   2 |  T   |    F    |  T   | dirty deleted OCC update 
    ///   3 |  T   |    F    |  F   | dirty deleted OCC delete 
    ///   4 |  F   |    T    |  T   | dirty created OCC create [1] 
    ///   5 |  F   |    T    |  F   | n/a [2] 
    ///   6 |  F   |    F    |  T   | Normal create 
    ///   7 |  F   |    F    |  F   | n/a [2]
    ///
    ///   [1] Not considered as possible in this version as no active
    ///   logic is given for how to find a possible collision between
    ///   entities beyond ID value. The ID value will never collide
    ///   because all IRepository{,}.Create calls replace any current
    ///   value for the ID with a new one upon save.
    ///
    ///   [2] Not considered as possible in this version as if there
    ///   is BFIM and no AFIM in a change, then there is nothing to
    ///   map the change back to a specific entity.
    /// </remarks>
    public class MergeConcurrentEditsConflictResolver : IConflictResolver
    {
        private readonly IMergeProvider _merge;

        /// <summary>
        ///   Initializes a new instance of the <see
        ///   cref="MergeConcurrentEditsConflictResolver"/> class.
        /// </summary>
        /// <param name="merge">The merge provider.</param>
        public MergeConcurrentEditsConflictResolver(IMergeProvider merge)
        {
            _merge = merge;
        }

        /// <summary>
        ///   Attempts to resolve a conflict by inspecting the past,
        ///   current, and divergent state of an entity.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="repository">The repository.</param>
        /// <param name="change">The change.</param>
        /// <returns>
        ///   <see langword="true"/> if successfully resolved conflict
        ///   with merge; otherwise, <see langword="false"/>.
        /// </returns>
        public bool Resolve<TEntity, TKey>(IRepository<TEntity, TKey> repository, ChangeItem<TEntity> change, IDictionary<TKey,TEntity> index)
        {
            var action = change.Action;
            TKey id;
            TEntity current;
            switch (action)
            {
                case ChangeAction.Create:
                    id = repository.GetId(change.AFIM);
                    repository.Create(change.AFIM);
                    return true;

                case ChangeAction.Update:
                case ChangeAction.Delete:

                    var kind = MergeKind.Auto;
                    id = repository.GetId(change.BFIM);
                    if (!index.TryGetValue(id, out current))
                    {
                        //kind = MergeKind.HiddenDelete;
                        //current = repository.GetById(id);
                    }
                    var resolution = _merge.Merge(kind, change.BFIM, current, change.AFIM);

                    if (resolution.IsResolved())
                    { // merge approved of delete
                        ReflectMergeInRepo(repository, change, current, resolution);
                        return true;
                    }
                    break;

                default:
                    break;
            }

            return false;
        }

        /// <summary>
        ///   Reflects the merge in repo.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="repository">The repository.</param>
        /// <param name="change">The change.</param>
        /// <param name="resolution">The resolution.</param>
        /// <exception cref="System.InvalidOperationException"></exception>
        private void ReflectMergeInRepo<TEntity, TKey>(IRepository<TEntity, TKey> repository, ChangeItem<TEntity> change, TEntity current, IMergeResolution<TEntity> resolution)
        {
            switch (resolution.Result)
            {
                case MergeActionResult.Resolved:
                    // absence of modifiers denotes the merge is wholly
                    // resolved by the resolution framework and does not
                    // need any action taken from the operational context.
                    break;

                case MergeActionResult.Delete:
                    repository.Delete(current);
                    break;

                case MergeActionResult.Create:
                    repository.Create(resolution.ResolvedValue);
                    break;

                case MergeActionResult.Update:
                    repository.Update(resolution.ResolvedValue);
                    break;

                case MergeActionResult.Unresolved:
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}