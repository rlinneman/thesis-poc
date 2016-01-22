namespace Rel.Data
{
    /// <summary>
    ///   A lock which never closes. Useful for avoiding type and null
    ///   checking code as well as handling all data uniformly
    ///   regardless of whether it supports pessimistic locking.
    /// </summary>
    internal sealed class NoopLock : ILock
    {
        private static readonly NoopLock _instance = new NoopLock();

        /// <summary>
        /// Prevents a default instance of the <see cref="NoopLock"/> class from being created.
        /// </summary>
        private NoopLock() { }

        /// <summary>
        ///   Gets the instance of NoopLock.
        /// </summary>
        /// <value>The instance.</value>
        /// <remarks>
        ///   Useful for performance improvement in avoiding repetitive
        ///   create and GC actions on this type.
        /// </remarks>
        public static ILock Instance { get { return _instance; } }

        /// <summary>
        /// Gets the current status of this lock.
        /// </summary>
        /// <value>
        /// The status.
        /// </value>
        public LockStatus Status
        {
            get { return LockStatus.Open; }
        }

        /// <summary>
        /// Attempts migrate access from Open to Exclusive.
        /// </summary>
        /// <returns>
        /// The final state of the lock.
        /// </returns>
        public LockStatus Close()
        {
            return LockStatus.Open;
        }

        /// <summary>
        /// Attempts to release this lock from Exclusive to Open status.
        /// </summary>
        /// <returns>
        /// The final state of the lock.
        /// </returns>
        public LockStatus Open()
        {
            return LockStatus.Open;
        }
    }
}