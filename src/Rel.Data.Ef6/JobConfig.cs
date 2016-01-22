using Rel.Data.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Annotations;

namespace Rel.Data.Ef6
{
    internal static partial class DataConfigExt
    {
        /// <summary>
        ///   Configures the EF data context for the
        ///   <see cref="T:Rel.Data.Models.Job"/> entity type.
        /// </summary>
        /// <param name="b">The model builder.</param>
        /// <returns>The given DbModelBuilder for chaining.</returns>
        internal static DbModelBuilder ConfigureJobs(this DbModelBuilder b)
        {
            b.Entity<Job>()
                .HasKey(_ => _.Id)
                .Property(_ => _.Id)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            b.Entity<Job>()
                .Property(_ => _.RowVersion)
                .IsRowVersion();

            b.Entity<Job>()
                .Property(_ => _.City)
                .HasMaxLength(250)
                .IsOptional();

            b.Entity<Job>()
                .Property(_ => _.LockedBy)
                .IsOptional()
                .HasMaxLength(20);

            b.Entity<Job>()
                .Property(_ => _.LockedOn)
                .IsOptional();

            b.Entity<Job>()
                .Property(_ => _.Name)
                .IsRequired()
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(
                        new IndexAttribute("UQ_JobName", 1) { IsUnique = true }))
                .HasMaxLength(100);

            b.Entity<Job>()
                 .Property(_ => _.PostalCode)
                 .HasMaxLength(10)
                 .IsOptional();

            b.Entity<Job>()
                 .Property(_ => _.State)
                 .HasMaxLength(2)
                 .IsOptional();

            b.Entity<Job>()
                 .Property(_ => _.Street1)
                 .HasMaxLength(200)
                 .IsOptional();

            b.Entity<Job>()
                 .Property(_ => _.Street2)
                 .HasMaxLength(100)
                 .IsOptional();

            b.Entity<Job>()
                .HasMany(_ => _.Assets);

            return b;
        }
    }
}