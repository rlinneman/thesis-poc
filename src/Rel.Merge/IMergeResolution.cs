using System;

namespace Rel.Merge
{
    /// <summary>
    ///   Identifies the result a merge operation.
    /// </summary>
    /// <typeparam name="TResolved">The type of the resolved.</typeparam>
    public interface IMergeResolution<TResolved>
    {
        /// <summary>
        ///   Gets the resolved value.
        /// </summary>
        /// <value>The resolved value.</value>
        TResolved ResolvedValue { get; }

        /// <summary>
        ///   Gets the result.
        /// </summary>
        /// <value>The result.</value>
        MergeActionResult Result { get; }
    }

    /// <summary>
    ///   Simple convenience helpers to avoid having to implement on any IMergeResolution{}.
    /// </summary>
    public static class MergeResolutionExtension
    {
        /// <summary>
        ///   Determines whether this instance is resolved.
        /// </summary>
        /// <typeparam name="TResolved">The type of the resolved.</typeparam>
        /// <param name="resolution">The resolution.</param>
        /// <returns>
        ///   <see langword="true"/> if this instance has resolved;
        ///   otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsResolved<TResolved>(this IMergeResolution<TResolved> resolution)
        {
            return resolution.Result.HasFlag(MergeActionResult.Resolved);
        }
    }
}