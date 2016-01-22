using Rel.Data.Models;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;

namespace Rel.Data.Ef6
{
    /// <summary>
    ///   Exposes an EF6 <see cref="T:System.Data.Entity.DbContext"/>
    ///   as a <see cref="T:Rel.Data.IDataContext"/> to allow for
    ///   simple replacement of the underlying data persistence layer.
    /// </summary>
    public class TpContext 
        : DbContext, IDataContext
    {
        private readonly DbRepository<Asset, int> _assets;
        private readonly DbRepository<Job, int> _jobs;

        /// <summary>
        ///   Initializes a new instance of the
        ///   <see cref="TpContext"/> class.
        /// </summary>
        public TpContext()
            : base("TpContext")
        {
            Configuration.LazyLoadingEnabled = false;

            _assets = new DbRepository<Asset, int>(this, Assets, _ => _.Id);
            _jobs = new DbRepository<Job, int>(this, Jobs, _ => _.Id);
        }

        /// <summary>
        ///   Gets the asset repository.
        /// </summary>
        /// <value>The assets.</value>
        public DbSet<Asset> Assets { get; set; }

        /// <summary>
        ///   Gets the asset repository.
        /// </summary>
        /// <value>The assets.</value>
        IRepository<Asset, int> IDataContext.Assets
        {
            get { return _assets; }
        }

        /// <summary>
        ///   Gets the job repository.
        /// </summary>
        /// <value>The jobs.</value>
        IRepository<Job, int> IDataContext.Jobs
        {
            get { return _jobs; }
        }

        /// <summary>
        ///   Gets the job repository.
        /// </summary>
        /// <value>The jobs.</value>
        public DbSet<Job> Jobs { get; set; }

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
        void IDataContext.AcceptChanges()
        {
            SaveChanges();
        }

        /// <summary>
        ///   Undoes any changes made in this data context since the
        ///   last time it communicated with the underlying data store.
        /// </summary>
        void IDataContext.RejectChanges()
        {
            this.RejectChanges();
        }

        /// <summary>
        ///   Validates changes in this data context.
        /// </summary>
        /// <returns>
        ///   <see langword="true"/> if valid; otherwise, false.
        /// </returns>
        bool IDataContext.Validate()
        {
            return !GetValidationErrors().Any();
        }

        /// <summary>
        ///   Saves all changes made in this context to the underlying database.
        /// </summary>
        /// <returns>
        ///   The number of state entries written to the underlying
        ///   database. This can include state entries for entities
        ///   and/or relationships. Relationship state entries are
        ///   created for many-to-many relationships and relationships
        ///   where there is no foreign key property included in the
        ///   entity class (often referred to as independent associations).
        /// </returns>
        /// <exception cref="ConcurrencyException">
        ///   Dirty writes detected.
        /// </exception>
        public override int SaveChanges()
        {
            var errors = GetValidationErrors();
            if (!errors.Any())
            {
                try
                {
                    return base.SaveChanges();
                }
                catch (System.Data.Entity.Infrastructure.DbUpdateConcurrencyException ex)
                {
                    throw new ConcurrencyException("Dirty writes detected.", ex);
                }
            }
            else
            {
                throw new ValidationException();
            }
        }

        /// <summary>
        ///   Calls the protected Dispose method.
        /// </summary>
        void System.IDisposable.Dispose()
        {
            this.Dispose();
        }

        /// <summary>
        ///   This method is called when the model for a derived
        ///   context has been initialized, but before the model has
        ///   been locked down and used to initialize the context. The
        ///   default implementation of this method does nothing, but
        ///   it can be overridden in a derived class such that the
        ///   model can be further configured before it is locked down.
        /// </summary>
        /// <param name="modelBuilder">
        ///   The builder that defines the model for the context being created.
        /// </param>
        /// <remarks>
        ///   Typically, this method is called only once when the
        ///   first instance of a derived context is created. The
        ///   model for that context is then cached and is for all
        ///   further instances of the context in the app domain. This
        ///   caching can be disabled by setting the ModelCaching
        ///   property on the given ModelBuidler, but note that this
        ///   can seriously degrade performance. More control over
        ///   caching is provided through use of the DbModelBuilder
        ///   and DbContextFactory classes directly.
        /// </remarks>
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            modelBuilder.Conventions.Remove<ManyToManyCascadeDeleteConvention>();

            modelBuilder
                .ConfigureJobs()
                .ConfigureAssets();
        }

        /// <summary>
        ///   Undoes any changes made in this data context since the
        ///   last time it communicated with the underlying data store.
        /// </summary>
        private void RejectChanges()
        {
            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.State = EntityState.Detached;
                }
                else
                {
                    entry.State = EntityState.Unchanged;
                }
            }
        }
    }
}