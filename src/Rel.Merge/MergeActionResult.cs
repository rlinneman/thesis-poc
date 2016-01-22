using System;

namespace Rel.Merge
{
    /// <summary>
    ///   Identifies action taken in response to a merge operation.
    /// </summary>
    [Flags]
    public enum MergeActionResult
    {
        /// <summary>
        ///   No action taken, merge cannot be resolved.
        /// </summary>
        Unresolved = 0x00000000,

        /// <summary>
        ///   Identifies that the action was resolved. If no sibling
        ///   accent states appear with this state, the merge is
        ///   resolve through NOOP.
        /// </summary>
        Resolved = 0x00000001,

        /// <summary>
        ///   Merge is resolved by deletion.
        /// </summary>
        Delete = 0x00000003,

        /// <summary>
        ///   Merge is resolved by creation.
        /// </summary>
        Create = 0x00000005,

        /// <summary>
        ///   Merge is resolved by update to existing state.
        /// </summary>
        Update = 0x00000009
    }
}