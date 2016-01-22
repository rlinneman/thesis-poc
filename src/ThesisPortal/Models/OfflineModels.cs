using Rel.Data.Bulk;
using System.ComponentModel.DataAnnotations;

namespace ThesisPortal.Models
{
    /// <summary>
    ///   Represents a request to commit bulk changes wrapped in a <see cref="T:Rel.Data.Bulk.ChangeSet"/>.
    /// </summary>
    public class CheckinRequest
    {
        /// <summary>
        ///   Gets or sets the change set.
        /// </summary>
        /// <value>The change set.</value>
        //[Required]
        public ChangeSet ChangeSet { get; set; }

        /// <summary>
        ///   Gets or sets the claim partition indicator.
        /// </summary>
        /// <value>The claim partition.</value>
        public bool ClaimPartition { get; set; }

        /// <summary>
        ///   Gets or sets the partition identifier.
        /// </summary>
        /// <value>The partition identifier.</value>
        [Required]
        [Range(1, int.MaxValue)]
        public int PartitionId { get; set; }
    }
}