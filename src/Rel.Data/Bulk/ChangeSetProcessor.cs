using Rel.Data.Configuration;
using Rel.Data.Diagnostics;
using Rel.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Transactions;

namespace Rel.Data.Bulk
{
    /// <summary>
    ///   Manages integration of a collection of changes 
    ///   into an <see cref="Rel.Data.IDataContext"/>.
    /// </summary>
    public class ChangeSetProcessor
    {
        private readonly IDataContext _db;
        private readonly bool _disableResolution = false;
        private readonly IConflictResolver _resolver;
        private readonly string _resolverName;

        /// <summary>
        ///   Initializes a new instance of the
        ///   <see cref="ChangeSetProcessor"/> class.
        /// </summary>
        /// <param name="context">The data context.</param>
        /// <param name="conflictResolver">The conflict resolver.</param>
        public ChangeSetProcessor(IDataContext context, IConflictResolver conflictResolver)
        {
            _db = context;
            _resolver = conflictResolver;
            _resolverName = conflictResolver.GetType().Name;
            if (conflictResolver is RejectConcurrentEditsConflictResolver)
                _disableResolution = true;
        }

        /// <summary>
        ///   Builds the initial change set which a client will build
        ///   changes from.
        /// </summary>
        /// <param name="partitionId">The partition identifier.</param>
        /// <returns>
        ///   A change set where every item is marked as Initialize.
        /// </returns>
        public ChangeSet BuildInitialChangeSet(int partitionId)
        {
            var cs = new ChangeSet();
            cs.Assets = _db
                .Assets
                .GetAll()
                .Where(_ => _.JobId == partitionId)
                .ToList()
                .Select(_ => new ChangeItem<Asset>(null, _) { Action = ChangeAction.Initialize })
                .ToList();

            return cs;
        }

        /// <summary>
        ///   Processes the specified change set under the given
        ///   partition id.
        /// </summary>
        /// <param name="partitionId">The partition identifier.</param>
        /// <param name="claimIt">
        ///   Set to <see langword="true"/> to indicate desire to
        ///   claim exclusive access to the partition if unable to
        ///   commit in a single optimistic concurrency control update.
        /// </param>
        /// <param name="changeSet">The change set.</param>
        /// <returns>
        ///   ChangeSet.Empty if the change set was accepted;
        ///   otherwise, a new change set is returned containing a
        ///   "migration script" for the submitter to bring their
        ///   local data current with the remote version for the items submitted.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        ///   changeSet
        /// </exception>
        /// <exception cref="EntityNotFoundException"></exception>
        /// <exception cref="ConcurrencyException"></exception>
        public ChangeSet Process(int partitionId, bool claimIt, ChangeSet changeSet)
        {
            if (changeSet == null)
                throw new ArgumentNullException("changeSet");

            var result = ChangeSet.Empty;
            using (var perfScope = new ChangeSetPerformanceScope(_resolverName, changeSet))
            {
                using (perfScope.TimeReplay())
                    Replay(changeSet);

                if (!_db.Validate())
                    throw Error.InvalidData(changeSet, false);

                bool accepted;

                using (perfScope.TimeSave())
                    accepted = Apply(partitionId, claimIt, changeSet);

                if (accepted)
                {
                    perfScope.Complete();
                }
                else
                {
                    using (perfScope.TimeRedress())
                    {
                        bool resolved = false;
                        using (perfScope.TimeCacheBuilding())
                            FlushAndReCache();
                        using (perfScope.TimeResolve())
                            resolved = ResolveConflicts(changeSet);

                        if (resolved)
                        {
                            if (!_db.Validate())
                                throw Error.InvalidData(changeSet, true);

                            using (perfScope.TimeSave())
                                accepted = Apply(partitionId, claimIt, changeSet);

                            if (accepted)
                            {
                                perfScope.Complete();
                                return result;
                            }
                        }

                        result = BuildReconciliationChangeSet(changeSet);
                    }
                }
            }

            return result;
        }

        private bool Apply(int partitionId, bool claimIt, ChangeSet changeSet)
        {
            ILock @lock;
            bool accepted = false;

            using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew))
            {
                @lock = _db.Jobs.GetAll().Where(_ => _.Id == partitionId).SingleOrDefault();

                if (@lock == null)
                    throw new EntityNotFoundException();

                if (@lock.Status == LockStatus.Closed)
                {
                    _db.RejectChanges();
                    throw Error.PessimisticLock();
                }

                try
                {
                    _db.AcceptChanges();
                    scope.Complete();
                    accepted = true;
                }
                catch (ConcurrencyException)
                {
                    // normal OC update failed, claim partition if
                    // necessary then fall back to reconciliation.
                }
            }

            if (accepted)
            {
                if (@lock.Status == LockStatus.Exclusive)
                {
                    @lock.Open();
                    _db.AcceptChanges();
                }
            }
            else
            {
                _db.RejectChanges();

                if (@lock.Status == LockStatus.Open &&
                    (claimIt ||
                    IsOfSufficientSize(changeSet)))
                {
                    using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew))
                    {
                        try
                        {
                            @lock.Close();
                            _db.AcceptChanges();
                            scope.Complete();
                        }
                        catch (ConcurrencyException)
                        {
                            throw Error.PessimisticLock();
                        }
                    }
                }
            }
            return accepted;
        }

        /// <summary>
        ///   Builds the reconciliation change set.
        /// </summary>
        /// <param name="changeSet">
        ///   The change set containing unresolvable conflict(s).
        /// </param>
        /// <returns>
        ///   A reconciliation change set fit for client consumption.
        /// </returns>
        private ChangeSet BuildReconciliationChangeSet(ChangeSet changeSet)
        {
            var reconcile = new ChangeSet();

            reconcile.Assets = CreateReconcileChangeCollection(_db.Assets, changeSet.Assets);

            return reconcile;
        }

        /// <summary>
        ///   Creates a primary key join filter expression.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="keySelector">The key selector.</param>
        /// <param name="keys">The keys.</param>
        /// <returns>
        ///   A LINQ expression which will filter on a known
        ///   collection of primary key values.
        /// </returns>
        /// <remarks>
        ///   Note that this builds and returns a LINQ expression, not
        ///   a function. This is necessary for the LINQ to SQL
        ///   provider (or any other provider which may intelligently
        ///   bridge languages) to translate a LINQ query to SQL query.
        /// </remarks>
        private Expression<Func<TEntity, bool>> CreateJoinFilter<TEntity, TKey>(Expression<Func<TEntity, TKey>> keySelector, IEnumerable<TKey> keys)
        {
            var lparams = Expression.Parameter(typeof(TEntity), "e");
            return Expression.Lambda<Func<TEntity, bool>>(
                Expression.Call(
                    typeof(Enumerable),
                    "Contains",
                    new[] { typeof(TKey) },
                    new Expression[] { Expression.Constant(keys, typeof(IEnumerable<TKey>)), Expression.Invoke(keySelector, lparams) }
                ),
                lparams
                );
        }

        /// <summary>
        ///   Creates a collection of changes to be applied remotely
        ///   for reconciliation.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="repository">The repository.</param>
        /// <param name="changes">The changes.</param>
        /// <returns>
        ///   A collection of changes necessary to bring the submitter
        ///   of the changes which failed to current for all items submitted.
        /// </returns>
        private List<ChangeItem<TEntity>> CreateReconcileChangeCollection<TEntity, TKey>(IRepository<TEntity, TKey> repository, IEnumerable<ChangeItem<TEntity>> changes)
        {
            List<ChangeItem<TEntity>> reconcile = new List<ChangeItem<TEntity>>();
            foreach (var change in changes)
            {
                if (change.Action == ChangeAction.Create)
                    // for the time being, create is not capable of
                    // conflict or reconciliation
                    continue;

                var updateTo = repository.GetById(repository.GetId(change.BFIM));

                if (updateTo == null)
                {
                    reconcile.Add(new ChangeItem<TEntity>(change.BFIM, default(TEntity))
                    {
                        Action = ChangeAction.Delete
                    });
                }
                else
                {
                    reconcile.Add(new ChangeItem<TEntity>(change.BFIM, updateTo)
                    {
                        Action = ChangeAction.Update
                    });
                }
            }

            return reconcile;
        }

        private void FlushAndReCache()
        {
            // get rid of all change set changes in the local cache
            // and pull down fresh copies of cache items.
            using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew))
            {
                FlushAndReCache(_db.Assets);
                scope.Complete();
            }
        }

        /// <summary>
        ///   Flushes the local cache of the given repository and
        ///   re-queries the GetAll() method to load up fresh copies
        ///   of data.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="repository">The repository.</param>
        /// <remarks>
        ///   This method is necessary as the initial attempt to
        ///   submit a change set is not proactive in fetching items
        ///   from the underlying data store. This is predicated on
        ///   the assumption that majority of change set submissions
        ///   should succeed on their initial pass and be "clean".
        ///   Since the items are not preloaded, the local data cache
        ///   does not actually know the current state of any items it holds.
        ///   <para>
        ///     This method will record the identities of all items it
        ///     holds locally, empty the local cache, then query the
        ///     underlying data store for all identities previously witnessed.
        ///   </para>
        /// </remarks>
        private void FlushAndReCache<TEntity, TKey>(IRepository<TEntity, TKey> repository)
        {
            var ids = repository.Select(repository.GetId).ToArray();
            repository.Flush();
            var e = CreateJoinFilter(repository.KeySelector, ids);
            repository.GetAll().Where(e.Compile()).ToList();
        }

        /// <summary>
        ///   Determines whether the change set is of sufficient size
        ///   to warrant attempting exclusive access.
        /// </summary>
        /// <param name="changeSet">The change set.</param>
        /// <returns></returns>
        private bool IsOfSufficientSize(ChangeSet changeSet)
        {
            var threshold = DataConfigurationSection.Default.ChangeSets.LockThreshold;
            if (threshold > 0 && changeSet.TotalItemsCount >= threshold)
                return true;

            return false;
        }

        /// <summary>
        ///   Replays actions recorded in a change set on a repository.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="repository">The repository.</param>
        /// <param name="changes">The changes.</param>
        /// <exception cref="System.InvalidOperationException">
        ///   Initialize action only supported on client. or Invalid
        ///   change action.
        /// </exception>
        /// <exception cref="System.NotImplementedException"></exception>
        private void ProcessActions<TEntity, TKey>(IRepository<TEntity, TKey> repository, IEnumerable<ChangeItem<TEntity>> changes)
        {
            foreach (var item in changes)
            {
                switch (item.Action)
                {
                    case ChangeAction.Initialize:
                        throw new InvalidOperationException("Initialize action only supported on client.");
                    case ChangeAction.Create:
                        repository.Create(item.AFIM);
                        break;

                    case ChangeAction.Update:
                        repository.Update(item.AFIM);
                        break;

                    case ChangeAction.Delete:
                        repository.Delete(item.BFIM);
                        break;

                    default:
                        throw new InvalidOperationException("Invalid change action.");
                }
            }
        }

        private void Replay(ChangeSet changeSet)
        {
            ProcessActions(_db.Assets, changeSet.Assets);
        }

        /// <summary>
        ///   Attempts to resolve all conflicts in the change set given.
        /// </summary>
        /// <param name="changeSet">The change set.</param>
        /// <returns>
        ///   <see langword="true"/> if all conflicts were resolved;
        ///   otherwise, <see langword="false"/>.
        /// </returns>
        private bool ResolveConflicts(ChangeSet changeSet)
        {
            if (_disableResolution) return false;
            var lookup = _db.Assets.ToDictionary(_ => _.Id);

            // attempt to resolve all conflicts
            bool isClean = changeSet.Assets.All(_ =>
                _resolver.Resolve(_db.Assets, _, lookup));
            return isClean;
        }
    }
}