using System;

namespace Rel.Merge
{
    /// <summary>
    ///   ***internal use only*** An intermediate result which
    ///      communicates state of child merge operations with a
    ///      callback to apply them. Use the callback to ensure that all
    ///      sibling merges are able to resolve before any of them
    ///      resolve. This keeps the parent merge pristine until such
    ///      time as it abandons or commits the merge. Pseudo
    ///      transactional behavior.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     <note type="note">***THIS IS A VALUE TYPE***</note> to
    ///     alleviate GC pressure. This will be invoked for each property
    ///     of each entity which gets merged. As such, this could see
    ///     many very small lifetimes.
    ///   </para>
    /// </remarks>
    internal struct PendingMergeResolution
    {
        /// <summary>
        ///   The commit callback to execute the resolution.
        /// </summary>
        public readonly Action Commit;

        private readonly MergeActionResult _result;

        /// <summary>
        ///   Initializes a new instance of the <see
        ///   cref="PendingMergeResolution"/> struct.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <param name="commit">The commit.</param>
        public PendingMergeResolution(MergeActionResult result, Action commit)
        {
            _result = result;
            Commit = commit;
        }

        /// <summary>
        ///   Gets a value indicating whether this instance can resolve.
        /// </summary>
        /// <value>
        ///   <see langword="true"/> if this instance can resolve;
        ///   otherwise, <see langword="false"/>.
        /// </value>
        public bool CanResolve { get { return _result.HasFlag(MergeActionResult.Resolved); } }
    }
}