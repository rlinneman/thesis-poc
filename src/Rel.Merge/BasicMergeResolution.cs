namespace Rel.Merge
{
    /// <summary>
    ///   Stubs the core purpose of IMergeResolution{} for internal
    ///   communication and state sharing.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class BasicMergeResolution<T> 
        : IMergeResolution<T>
    {
        private MergeActionResult _result;
        private T _value;

        /// <summary>
        ///   Initializes a new instance of the
        ///   <see cref="BasicMergeResolution{T}"/> class.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <param name="value">The value.</param>
        public BasicMergeResolution(MergeActionResult result, T value)
        {
            _result = result;
            _value = value;
        }

        /// <summary>
        ///   Gets the resolved value.
        /// </summary>
        /// <value>The resolved value.</value>
        public T ResolvedValue
        {
            get { return _value; }
        }

        /// <summary>
        ///   Gets the result.
        /// </summary>
        /// <value>The result.</value>
        public MergeActionResult Result
        {
            get { return _result; }
        }
    }
}