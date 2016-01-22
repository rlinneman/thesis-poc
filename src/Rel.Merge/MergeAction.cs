using System;

namespace Rel.Merge
{
    /// <summary>
    ///   Communicates steps in the merge process to individual merge implementations.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    public class MergeAction<TValue>
    {
        private readonly TValue _bfim, _cfim, _afim;
        private readonly MergeKind _kind;
        private TValue _resolvedValue;
        private MergeActionResult? _result;

        /// <summary>
        ///   Initializes a new instance of the
        ///   <see cref="MergeAction{TValue}"/> class.
        /// </summary>
        /// <param name="kind">The kind of merge.</param>
        /// <param name="bfim">
        ///   The before image, shared base state between cfim and afim.
        /// </param>
        /// <param name="cfim">The current image.</param>
        /// <param name="afim">
        ///   The after image attempting to overwrite cfim.
        /// </param>
        public MergeAction(MergeKind kind, TValue bfim, TValue cfim, TValue afim)
        {
            _kind = kind;
            _bfim = bfim;
            _cfim = cfim;
            _afim = afim;
            _resolvedValue = default(TValue);
            _result = null;
        }

        /// <summary>
        ///   Gets the after image.
        /// </summary>
        /// <value>The after image.</value>
        public TValue AFIM { get { return _afim; } }

        /// <summary>
        ///   Gets the before image.
        /// </summary>
        /// <value>The before image.</value>
        public TValue BFIM { get { return _bfim; } }

        /// <summary>
        ///   Gets the current image.
        /// </summary>
        /// <value>The current image.</value>
        public TValue CFIM { get { return _cfim; } }

        /// <summary>
        ///   Gets the kind of merge.
        /// </summary>
        /// <value>The kind of merge.</value>
        public MergeKind Kind { get { return _kind; } }

        /// <summary>
        ///   Gets a value indicating whether this
        ///   <see cref="MergeAction{TValue}"/> is resolved.
        /// </summary>
        /// <value>
        ///   <see langword="true"/> if resolved; otherwise, <see langword="false"/>.
        /// </value>
        public bool Resolved
        {
            get
            {
                return _result.HasValue &&
_result.Value.HasFlag(MergeActionResult.Resolved);
            }
        }

        /// <summary>
        ///   Gets the value this action resolved to.
        /// </summary>
        /// <value>The resolved value.</value>
        public TValue ResolvedValue { get { return _resolvedValue; } }

        /// <summary>
        ///   Gets the result an attempt to merge on this action.
        /// </summary>
        /// <value>The result.</value>
        public MergeActionResult Result
        {
            get
            {
                return
                    _result ??
                    MergeActionResult.Unresolved;
            }
        }

        /// <summary>
        ///   Resolves using the specified result and value.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <param name="resolveWith">The resolve with.</param>
        /// <exception cref="System.ArgumentException">
        ///   Cannot resolve with Unresolved.;result
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        ///   Already resolved.
        /// </exception>
        public void Resolve(MergeActionResult result, TValue resolveWith)
        {
            if (!result.HasFlag(MergeActionResult.Resolved))
                throw new ArgumentException("Cannot resolve with Unresolved.", "result");

            if (_result.HasValue)
                throw new InvalidOperationException("Already resolved.");

            _result = result;
            _resolvedValue = resolveWith;
        }
    }
}