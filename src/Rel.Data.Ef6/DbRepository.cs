using System;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;

namespace Rel.Data.Ef6
{
    /// <summary>
    ///   Exposes an EF DbSet as a
    ///   <see cref="T:Rel.Data.IRepository"/>. Queries directly on
    ///   DbRepository are *local only*.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <remarks>
    ///   <note type="note">All direct queries access the local store,
    ///   not the traditional EF Linq-SQL provider.</note>
    ///   <para>
    ///     To access the EF Linq-SQL provider, use the
    ///     <see cref="M:GetAll()"/> method.
    ///   </para>
    /// </remarks>
    internal class DbRepository<TEntity, TKey>
        : IRepository<TEntity, TKey> where TEntity : class
    {
        private readonly Func<TEntity, TKey> _compiledKeySelector;
        private readonly DbSet<TEntity> _dbSet;
        private readonly Expression<Func<TEntity, TKey>> _keySelector;
        private readonly IQueryable<TEntity> _queryable;
        private DbContext _context;

        /// <summary>
        ///   Initializes a new instance of the
        ///   <see cref="DbRepository{TEntity, TKey}"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="dbSet">The database set.</param>
        /// <param name="keySelector">The key selector.</param>
        /// <exception cref="System.ArgumentNullException">
        ///   context or dbSet or keySelector
        /// </exception>
        public DbRepository(DbContext context, DbSet<TEntity> dbSet, Expression<Func<TEntity, TKey>> keySelector)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            if (dbSet == null)
                throw new ArgumentNullException("dbSet");
            if (keySelector == null)
                throw new ArgumentNullException("keySelector");

            _context = context;
            _dbSet = dbSet;
            _queryable = dbSet.Local.AsQueryable();
            _keySelector = keySelector;
            _compiledKeySelector = keySelector.Compile();
        }

        /// <summary>
        ///   Gets the type of the element(s) that are returned when
        ///   the expression tree associated with this instance of
        ///   <see cref="T:System.Linq.IQueryable"/> is executed.
        /// </summary>
        Type IQueryable.ElementType
        {
            get { return _queryable.ElementType; }
        }

        /// <summary>
        ///   Gets the expression tree that is associated with the
        ///   instance of <see cref="T:System.Linq.IQueryable"/>.
        /// </summary>
        Expression IQueryable.Expression
        {
            get { return _queryable.Expression; }
        }

        /// <summary>
        ///   Gets the query provider that is associated with this
        ///   data source.
        /// </summary>
        IQueryProvider IQueryable.Provider
        {
            get { return _queryable.Provider; }
        }

        /// <summary>
        ///   Gets an expression which, when compiled and run, yields
        ///   the primary key to entities of type TEntity.
        /// </summary>
        /// <value>The key selector.</value>
        Expression<Func<TEntity, TKey>> IRepository<TEntity, TKey>.KeySelector { get { return _keySelector; } }

        /// <summary>
        ///   Adds the specified entity to this repository and places
        ///   it in queue for creation when the parent data context
        ///   has changes on it accepted.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>
        ///   The entity given or a potential proxy instance for
        ///   interfacing with underlying framework.
        /// </returns>
        TEntity IRepository<TEntity, TKey>.Create(TEntity entity)
        {
            var e = _context.Entry(entity);
            e.State = EntityState.Added;
            return e.Entity;
        }

        /// <summary>
        ///   Adds the specified entity to this repository and places
        ///   it in queue for deletion when the parent data context
        ///   has changes on it accepted.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>
        ///   The entity given or a potential proxy instance for
        ///   interfacing with underlying framework.
        /// </returns>
        TEntity IRepository<TEntity, TKey>.Delete(TEntity entity)
        {
            var e = _context.Entry(entity);
            e.State = EntityState.Deleted;
            return e.Entity;
        }

        /// <summary>
        ///   Flushes the local cache of this repository instance.
        /// </summary>
        void IRepository<TEntity, TKey>.Flush()
        {
            foreach (var item in _dbSet.Local.ToArray())
            {
                _context.Entry(item).State = EntityState.Detached;
            }
        }

        /// <summary>
        ///   Gets a queryable which may be used to access all
        ///   entities contained in the underlying repository which
        ///   may not yet be cached locally.
        /// </summary>
        /// <returns>A queryable to the underlying data store.</returns>
        IQueryable<TEntity> IRepository<TEntity, TKey>.GetAll()
        {
            return _dbSet;
        }

        /// <summary>
        ///   Gets an entity by its id.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>
        ///   The entity with the given id (possibly a proxy instance)
        ///   or <see langword="null"/>.
        /// </returns>
        TEntity IRepository<TEntity, TKey>.GetById(TKey id)
        {
            var selector = BuildSelectorFor(id);
            TEntity result;
            try
            {
                result = _dbSet.Local.AsQueryable().SingleOrDefault(selector);
                result = result ?? _dbSet.SingleOrDefault(selector);
                return result;
            }
            catch (InvalidOperationException)
            {
                System.Diagnostics.Trace.TraceError("Caught duplicate exception id", id);
                throw;
            }
        }

        /// <summary>
        ///   Gets the primary key from the given entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>The primary key of the given entity.</returns>
        TKey IRepository<TEntity, TKey>.GetId(TEntity entity)
        {
            return _compiledKeySelector(entity);
        }

        /// <summary>
        ///   Adds the specified entity to this repository and places
        ///   it in queue for update when the parent data context has
        ///   changes on it accepted.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>
        ///   The entity given or a potential proxy instance for
        ///   interfacing with underlying framework.
        /// </returns>
        TEntity IRepository<TEntity, TKey>.Update(TEntity entity)
        {
            var e = _context.Entry(entity);
            e.State = EntityState.Modified;

            return e.Entity;
        }

        /// <summary>
        ///   Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        ///   A
        ///   <see cref="T:System.Collections.Generic.IEnumerator`1"/>
        ///   that can be used to iterate through the collection.
        /// </returns>
        System.Collections.Generic.IEnumerator<TEntity> System.Collections.Generic.IEnumerable<TEntity>.GetEnumerator()
        {
            return _queryable.GetEnumerator();
        }

        /// <summary>
        ///   Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        ///   An <see cref="T:System.Collections.IEnumerator"/> object
        ///   that can be used to iterate through the collection.
        /// </returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _queryable.GetEnumerator();
        }

        /// <summary>
        ///   Builds the selector for.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        private Expression<Func<TEntity, bool>> BuildSelectorFor(TKey id)
        {
            var eq = Expression.Equal(_keySelector.Body, Expression.Constant(id));
            return Expression.Lambda<Func<TEntity, bool>>(eq, _keySelector.Parameters);
        }
    }
}