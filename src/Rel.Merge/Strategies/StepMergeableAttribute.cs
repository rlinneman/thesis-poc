using System;

namespace Rel.Merge.Strategies
{
    /// <summary>
    ///   Allows for specifying limits to quantity and monotonicity of
    ///   change permissible during merge operations.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    public class StepMergeableAttribute : NumericMergeableAttribute
    {
        private readonly Func<double, double, bool> _accept;
        private readonly double _lbound, _ubound;

        /// <summary>
        ///   Initializes a new instance of the
        ///   <see cref="StepMergeableAttribute"/> class.
        /// </summary>
        /// <param name="isPercent">
        ///   If set to <see langword="true"/> bounding values are
        ///   taken as percentages and the change is calculated as a
        ///   percent change.
        /// </param>
        /// <param name="lbound">The lower bound of change.</param>
        /// <param name="ubound">The upper bound of change.</param>
        /// <exception cref="System.ArgumentException">
        ///   lbound must be less than ubound;lbound,ubound
        /// </exception>
        public StepMergeableAttribute(bool isPercent, double lbound, double ubound)
            : base()
        {
            if (lbound > ubound)
                throw new ArgumentException("lbound must be less than ubound", "lbound,ubound");
            _lbound = lbound;
            _ubound = ubound;

            if (isPercent)
            {
                _accept = AcceptPercent;
            }
            else
            {
                _accept = AcceptMagnitude;
            }
        }

        /// <summary>
        ///   Initializes a new instance of the
        ///   <see cref="StepMergeableAttribute"/> class.
        /// </summary>
        /// <param name="lbound">The lower bound of change.</param>
        /// <param name="ubound">The upper bound of change.</param>
        public StepMergeableAttribute(double lbound, double ubound)
            : this(false, lbound, ubound)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the
        ///   <see cref="StepMergeableAttribute"/> class using
        ///   plus/minus <paramref name="step"/> for the bounds.
        /// </summary>
        /// <param name="step">The step size permissible.</param>
        public StepMergeableAttribute(double step)
            : this(false, step)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the
        ///   <see cref="StepMergeableAttribute"/> class using
        ///   plus/minus <paramref name="step"/> for the bounds.
        /// </summary>
        /// <param name="isPercent">
        ///   if set to <see langword="true"/> bounding values are
        ///   taken as percentages of change and the change is
        ///   calculated by (current-afim) / (current+afim).
        /// </param>
        /// <param name="step">The step.</param>
        public StepMergeableAttribute(bool isPercent, double step)
            : this(isPercent, -Math.Abs(step), Math.Abs(step))
        {
        }

        /// <summary>
        ///   Gets or sets a value indicating whether divide by zero
        ///   should reject or resolve.
        /// </summary>
        /// <value>
        ///   <see langword="true"/> if divide by zero should resolve;
        ///   otherwise, <see langword="false"/>.
        /// </value>
        public bool DivideByZeroOk { get; set; }

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
        ///   Gets a value indicating whether this instance is
        ///   percentage based.
        /// </summary>
        /// <value>
        ///   <see langword="true"/> if this instance is percentage
        ///   based; otherwise, <see langword="false"/>.
        /// </value>
        public bool IsPercentageBased { get { return _accept == AcceptPercent; } }

        /// <summary>
        ///   Gets the lower bound.
        /// </summary>
        /// <value>The lower bound.</value>
        public double LowerBound { get { return _lbound; } }

        /// <summary>
        ///   Gets the upper bound.
        /// </summary>
        /// <value>The upper bound.</value>
        public double UpperBound { get { return _ubound; } }

        /// <summary>
        ///   Merges the specified request.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="request">The request.</param>
        protected internal override void Merge<TValue>(MergeAction<TValue> request)
        {
            base.Merge(request);

            double current, next;
            if (Coherse(request.CFIM, out current) && Coherse(request.AFIM, out next))
            {
                if (_accept(current, next))
                    request.Resolve(MergeActionResult.Update, request.AFIM);
            }
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
        private bool Acceptable(double delta)
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

        /// <summary>
        ///   The default comparison, calculates change by simple
        ///   fixed value step.
        /// </summary>
        /// <param name="current">The current.</param>
        /// <param name="next">The next.</param>
        /// <returns>
        ///   <see langword="true"/> if the change is acceptable;
        ///   otherwise, <see langword="false"/>.
        /// </returns>
        private bool AcceptMagnitude(double current, double next)
        {
            var delta = next - current;
            return Acceptable(delta);
        }

        /// <summary>
        ///   Calculates acceptable step size as a percentage.
        /// </summary>
        /// <param name="current">The current.</param>
        /// <param name="next">The next.</param>
        /// <returns>
        ///   <see langword="true"/> if the change is acceptable;
        ///   otherwise, <see langword="false"/>.
        /// </returns>
        private bool AcceptPercent(double current, double next)
        {
            if (current == 0)
            {
                return DivideByZeroOk;
            }
            var delta = (next - current) / current;

            return Acceptable(delta);
        }
    }
}