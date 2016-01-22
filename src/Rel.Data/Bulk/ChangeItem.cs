using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Rel.Data.Bulk
{
    /// <summary>
    ///   Serves as a common ancestor for generic implementation such
    ///   that a single collection may contain multiple change item
    ///   generic types.
    /// </summary>
    [CustomValidation(typeof(ChangeValidator), "SanityCheck")]
    public abstract class ChangeItem
    {
        private ICollection<ValidationResult> _validationResults =
            new List<ValidationResult>();

        /// <summary>
        ///   Gets or sets the action intended to result from this change.
        /// </summary>
        /// <value>The action.</value>
        public ChangeAction Action { get; set; }

        /// <summary>
        ///   Gets the validation results of this change.
        /// </summary>
        /// <value>The validation results.</value>
        protected internal ICollection<ValidationResult> ValidationResults
        { get { return _validationResults; } }

        /// <summary>
        ///   Gets the type of the entity.
        /// </summary>
        /// <returns></returns>
        public abstract Type GetEntityType();

        /// <summary>
        ///   Gets the after completion image of this change.
        /// </summary>
        /// <returns></returns>
        internal abstract object GetAFIM();

        /// <summary>
        ///   Gets the before completion image of this change..
        /// </summary>
        /// <returns></returns>
        internal abstract object GetBFIM();
    }

    /// <summary>
    ///   Provides concrete instancing for generic change items. This
    ///   generic version makes client communication simple by
    ///   strongly enforcing an endpoint for data of type TEntity in a <see cref="ChangeSet"/>.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    public class ChangeItem<TEntity> : ChangeItem
    {
        internal static readonly IEnumerable<ChangeItem<TEntity>> Empty = new ChangeItem<TEntity>[0];

        /// <summary>
        ///   Initializes a new instance of the
        ///   <see cref="ChangeItem{TEntity}"/> class.
        /// </summary>
        public ChangeItem()
        {
        }

        /// <summary>
        ///   Initializes a new instance of the
        ///   <see cref="ChangeItem{TEntity}"/> class.
        /// </summary>
        /// <param name="bfim">The bfim.</param>
        /// <param name="afim">The afim.</param>
        public ChangeItem(TEntity bfim, TEntity afim)
        {
            BFIM = bfim;
            AFIM = afim;
        }

        /// <summary>
        ///   Initializes a new instance of the
        ///   <see cref="ChangeItem{TEntity}"/> class.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="bfim">The bfim.</param>
        /// <param name="afim">The afim.</param>
        public ChangeItem(ChangeAction action, TEntity bfim, TEntity afim)
            : this(bfim, afim)
        {
            Action = action;
        }

        /// <summary>
        ///   Gets or sets the after image of this change.
        /// </summary>
        /// <value>The after image.</value>
        public TEntity AFIM { get; set; }

        /// <summary>
        ///   Gets or sets the before image of this change.
        /// </summary>
        /// <value>The before image.</value>
        public TEntity BFIM { get; set; }

        /// <summary>
        ///   Gets the type of the entity.
        /// </summary>
        /// <returns>
        ///   The type of entity associated with this change.
        /// </returns>
        public override Type GetEntityType()
        {
            return typeof(TEntity);
        }

        /// <summary>
        ///   Gets the after completion image of this change.
        /// </summary>
        /// <returns></returns>
        internal override object GetAFIM()
        {
            return AFIM;
        }

        /// <summary>
        ///   Gets the before completion image of this change..
        /// </summary>
        /// <returns></returns>
        internal override object GetBFIM()
        {
            return BFIM;
        }
    }
}