using Rel.Data.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Rel.Data.Bulk
{
    /// <summary>
    ///   Curries a series of actions to be performed as an atomic action.
    /// </summary>
    public class ChangeSet //: IEnumerable<ChangeItem>
    {
        /// <summary>
        ///   ChangeSet's flavor of <see langword="null"/>.
        /// </summary>
        internal static readonly ChangeSet Empty = new ChangeSet();

        /// <summary>
        ///   Initializes a new instance of the
        ///   <see cref="ChangeSet"/> class.
        /// </summary>
        public ChangeSet()
        {
            Assets = new List<ChangeItem<Asset>>();
        }

        /// <summary>
        ///   Gets or sets the assets.
        /// </summary>
        /// <value>The assets.</value>
        public List<ChangeItem<Asset>> Assets { get; set; }

        /// <summary>
        ///   Gets a value indicating whether this instance is empty.
        /// </summary>
        /// <value>
        ///   <see langword="true"/> if this instance is empty;
        ///   otherwise, <see langword="false"/>.
        /// </value>
        public bool IsEmpty { get { return Assets == null || Assets.Count == 0; } }

        /// <summary>
        ///   Gets the total number of items in this change set.
        /// </summary>
        /// <value>The total items count.</value>
        /// <remarks>
        ///   The current implementation is limited to only one real
        ///   underlying type of
        ///   <see cref="T:Rel.Data.Models.Asset"/>. This property
        ///   will prove more useful as the project grows over time
        ///   and additional entity types are added with support for
        ///   bulk update.
        /// </remarks>
        [Range(1, int.MaxValue)]
        public long TotalItemsCount { get { return Assets.Count; } }
    }
}