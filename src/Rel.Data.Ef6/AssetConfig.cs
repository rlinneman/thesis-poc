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
        ///   <see cref="T:Rel.Data.Models.Asset"/> entity type.
        /// </summary>
        /// <param name="b">The model builder.</param>
        /// <returns>The given DbModelBuilder for chaining.</returns>
        internal static DbModelBuilder ConfigureAssets(this DbModelBuilder b)
        {
            b.Entity<Asset>()
                .HasKey(_ => _.Id)
                .Property(_ => _.Id)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            b.Entity<Asset>()
                .Property(_ => _.RowVersion)
                .IsRowVersion();

            b.Entity<Asset>()
                .Property(_ => _.JobId)
                .IsRequired()
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(
                        new IndexAttribute("UQ_AssetName", 0) { IsUnique = true }));

            b.Entity<Asset>()
                .Property(_ => _.Name)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(
                        new IndexAttribute("UQ_AssetName", 1) { IsUnique = true }))
                .IsRequired()
                .HasMaxLength(100);

            b.Entity<Asset>()
                .Property(_ => _.ServiceArea)
                .IsRequired()
                .HasMaxLength(100);

            return b;
        }
    }
}