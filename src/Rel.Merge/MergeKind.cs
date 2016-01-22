namespace Rel.Merge
{
    /// <summary>
    ///   Identify the reason for invocation of a merge action.
    /// </summary>
    public enum MergeKind
    {
        /// <summary>
        ///   Let merge decide the kind of operation based upon
        ///   bfim,cfim,and afim.
        /// </summary>
        Auto = 0,

        /// <summary>
        ///   The action is attempting to resolve a conflicting update.
        /// </summary>
        ConflictingUpdate,

        /// <summary>
        ///   The action is attempting to reconcile a new write
        ///   (delete or update) with a delete which has previously
        ///   been committed.
        /// </summary>
        HiddenDelete,

        /// <summary>
        ///   The action is attempting to resolve a previously
        ///   committed change with a new delete action.
        /// </summary>
        DirtyDelete
    }
}