namespace Rel.Data.Bulk
{
    /// <summary>
    ///   Denotes the intended behavior when processing a <see cref="T:Rel.Data.Bulk.ChangeItem"/>.
    /// </summary>
    public enum ChangeAction
    {
        /// <summary>
        ///   Identifies that a change is meant to initialize a
        ///   domain. Only used when sending changes to client to
        ///   establish baseline entity states.
        /// </summary>
        Initialize,

        /// <summary>
        ///   Specifies the change is meant to create a new entity.
        ///   Requires AFIM
        /// </summary>
        Create,

        /// <summary>
        ///   Specifies the change is meant to update an entity.
        ///   Requires BFIM and AFIM.
        /// </summary>
        Update,

        /// <summary>
        ///   Specifies the change is meant to delete an entity.
        ///   Requires BFIM.
        /// </summary>
        Delete,
    }
}