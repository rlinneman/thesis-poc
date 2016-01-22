using System;

namespace Rel.Merge.Strategies
{
    /// <summary>
    ///   Resolves conflicts by always forcing the new value on the
    ///   old value. May be applied to both classes and properties.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = false)]
    public sealed class LastWriteWinsAttribute
        : MergeableAttribute
    {
        private readonly bool _doesLastWriteWin;

        /// <summary>
        ///   Initializes a new instance of the
        ///   <see cref="LastWriteWinsAttribute"/> class.
        /// </summary>
        public LastWriteWinsAttribute()
            : this(true)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the
        ///   <see cref="LastWriteWinsAttribute"/> class.
        /// </summary>
        /// <param name="doesLastWriteWin">
        ///   if set to <see langword="true"/> will operate with last
        ///   write wins logic. If <see langword="false"/> concurrent
        ///   writes are rejected. <note type="note">Defaults to <see langword="true"/>.</note>
        /// </param>
        public LastWriteWinsAttribute(bool doesLastWriteWin)
        {
            _doesLastWriteWin = doesLastWriteWin;
        }

        /// <summary>
        ///   Gets a value indicating whether the application of this
        ///   attribute actually results in a last write win or last
        ///   write reject.
        /// </summary>
        /// <value>
        ///   <see langword="true"/> if the last write should win;
        ///   otherwise, <see langword="false"/>.
        /// </value>
        public bool DoesLastWriteWin { get { return _doesLastWriteWin; } }

        /// <summary>
        ///   Merges the values given into the modified value.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="baseValue">The base value.</param>
        /// <param name="current">The current.</param>
        /// <param name="modified">The modified.</param>
        /// <returns>
        ///   <see langword="true"/> if merge was successful;
        ///   otherwise, <see langword="false"/>.
        /// </returns>
        protected internal override void Merge<TValue>(MergeAction<TValue> request)
        {
            if (!_doesLastWriteWin)
                return;

            switch (request.Kind)
            {
                case MergeKind.Auto:
                    throw new NotSupportedException();
                case MergeKind.ConflictingUpdate:
                    request.Resolve(MergeActionResult.Update, request.AFIM);
                    break;

                case MergeKind.HiddenDelete:
                    if (request.AFIM != null)
                    {
                        request.Resolve(MergeActionResult.Create, request.AFIM);
                    }
                    else
                    {
                        // noop resolution
                        request.Resolve(MergeActionResult.Resolved, default(TValue));
                    }
                    break;

                case MergeKind.DirtyDelete:
                    request.Resolve(MergeActionResult.Delete, request.CFIM);
                    break;

                default:
                    throw new ArgumentException("request.Kind");
            }
        }
    }
}