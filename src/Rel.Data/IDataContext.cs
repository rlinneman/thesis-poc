using Rel.Data.Models;
using System;

namespace Rel.Data
{
    /// <summary>
    ///   Represents a composite data store for Thesis Portal entities.
    /// </summary>
    public interface IDataContext
        : IDisposable
    {
        /// <summary>
        ///   Gets the asset repository.
        /// </summary>
        /// <value>The assets.</value>
        IRepository<Asset, int> Assets { get; }

        /// <summary>
        ///   Gets the job repository.
        /// </summary>
        /// <value>The jobs.</value>
        IRepository<Job, int> Jobs { get; }

        /// <summary>
        ///   <para>
        ///     Attempts to push any changes made in this data context
        ///     since the last time it communicated with the
        ///     underlying data store to persisted storage.
        ///   </para>
        ///   <para>
        ///     Any error preventing a complete commit of the changes
        ///     within throws an exception.
        ///   </para>
        /// </summary>
        void AcceptChanges();

        /// <summary>
        ///   Undoes any changes made in this data context since the
        ///   last time it communicated with the underlying data store.
        /// </summary>
        void RejectChanges();

        /// <summary>
        ///   Validates changes in this data context.
        /// </summary>
        /// <returns>
        ///   <see langword="true"/> if valid; otherwise, false.
        /// </returns>
        bool Validate();
    }
}