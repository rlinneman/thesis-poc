using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Rel.Data
{
    /// <summary>
    ///   Represents a local cache and interface to an underlying data
    ///   store of TEntity types.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    public interface IRepository<TEntity, TKey> : IEnumerable<TEntity>, IQueryable<TEntity>
    {
        /// <summary>
        ///   Gets an expression which, when compiled and run, yields
        ///   the primary key to entities of type TEntity.
        /// </summary>
        /// <value>The key selector.</value>
        Expression<Func<TEntity, TKey>> KeySelector { get; }

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
        TEntity Create(TEntity entity);

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
        TEntity Delete(TEntity entity);

        /// <summary>
        ///   Flushes the local cache of this repository instance.
        /// </summary>
        void Flush();

        /// <summary>
        ///   Gets a queryable which may be used to access all
        ///   entities contained in the underlying repository which
        ///   may not yet be cached locally.
        /// </summary>
        /// <returns>A queryable to the underlying data store.</returns>
        IQueryable<TEntity> GetAll();

        /// <summary>
        ///   Gets an entity by its id.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>
        ///   The entity with the given id (possibly a proxy instance)
        ///   or <see langword="null"/>.
        /// </returns>
        TEntity GetById(TKey id);

        /// <summary>
        ///   Gets the primary key from the given entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>The primary key of the given entity.</returns>
        TKey GetId(TEntity entity);

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
        TEntity Update(TEntity entity);
    }
}