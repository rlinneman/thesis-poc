using System;

namespace Rel.Merge.Strategies
{
    /// <summary>
    ///   Allows for specifying max and min decay of a record when
    ///   considering accepting a change.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    public class DecaySpanMergeableAttribute : MergeableAttribute
    {
        private readonly TimeSpan _lbound, _ubound;

        /// <summary>
        ///   Initializes a new instance of the
        ///   <see cref="DecaySpanMergeableAttribute"/> class.
        /// </summary>
        /// <param name="maxDecay">The maximum decay.</param>
        /// <param name="minDecay">The minimum decay.</param>
        /// <exception cref="System.ArgumentException">
        ///   max decay must be greater than min decay;maxDecay,minDecay
        /// </exception>
        public DecaySpanMergeableAttribute(string maxDecay, string minDecay)
            : base()
        {
            var tsL = TimeSpan.Parse(minDecay);
            var tsU = TimeSpan.Parse(maxDecay);
            if (tsU <= tsL)
                throw new ArgumentException("max decay must be greater than min decay", "maxDecay,minDecay");

            _lbound = tsL;
            _ubound = tsU;
            FieldDateTimeKind = DateTimeKind.Local;
        }

        /// <summary>
        ///   Initializes a new instance of the
        ///   <see cref="DecaySpanMergeableAttribute"/> class.
        /// </summary>
        /// <param name="maxDecay">The maximum decay.</param>
        public DecaySpanMergeableAttribute(string maxDecay)
            : this(maxDecay, "0.00:00:00")
        {
        }

        /// <summary>
        ///   Gets or sets the kind of the field date time. Default is
        ///   local as most frameworks assume this, though they
        ///   certainly *should* assume UTC.
        /// </summary>
        /// <value>The kind of the field date time.</value>
        public DateTimeKind FieldDateTimeKind { get; set; }

        /// <summary>
        ///   Gets or sets a value indicating whether lower bound is
        ///   inclusive. Defaults to <see langword="false"/>.
        /// </summary>
        /// <value>
        ///   <see langword="true"/> if the lower bound is inclusive;
        ///   otherwise, <see langword="false"/>.
        /// </value>
        public bool InclusiveLBound { get; set; }

        /// <summary>
        ///   Gets or sets a value indicating whether upper bound is
        ///   inclusive. Defaults to <see langword="false"/>.
        /// </summary>
        /// <value>
        ///   <see langword="true"/> if upper bound is inclusive;
        ///   otherwise, <see langword="false"/>.
        /// </value>
        public bool InclusiveUBound { get; set; }

        /// <summary>
        ///   Gets the lower bound.
        /// </summary>
        /// <value>The lower bound.</value>
        public TimeSpan LowerBound { get { return _lbound; } }

        /// <summary>
        ///   Gets the upper bound.
        /// </summary>
        /// <value>The upper bound.</value>
        public TimeSpan UpperBound { get { return _ubound; } }

        /// <summary>
        ///   Merges the values given into the modified value.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="request">The request.</param>
        protected internal override void Merge<TValue>(MergeAction<TValue> request)
        {
            DateTime touched = (dynamic)request.CFIM;
            var dtn = DateTime.UtcNow;

            if (touched.Kind == DateTimeKind.Unspecified)
                touched = DateTime.SpecifyKind(touched, FieldDateTimeKind);

            if (touched.Kind != DateTimeKind.Utc)
                touched = touched.ToUniversalTime();

            var delta = dtn - touched;

            if (Acceptable(delta))
                request.Resolve(MergeActionResult.Update, request.AFIM);
        }

        /// <summary>
        ///   Resolves if a calculated delta lies within the
        ///   acceptable bounds.
        /// </summary>
        /// <param name="delta">The delta.</param>
        /// <returns>
        ///   <see langword="true"/> if the change is acceptable;
        ///   otherwise, <see langword="false"/>.
        /// </returns>
        private bool Acceptable(TimeSpan delta)
        {
            if (InclusiveLBound)
            {
                if (delta < _lbound)
                    return false;
            }
            else if (delta <= _lbound)
            {
                return false;
            }

            if (InclusiveUBound)
            {
                if (delta > _ubound)
                    return false;
            }
            else if (delta >= _ubound)
            {
                return false;
            }

            return true;
        }
    }
}