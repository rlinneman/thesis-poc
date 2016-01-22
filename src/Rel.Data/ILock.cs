namespace Rel.Data
{
    /// <summary>
    ///   Represents a pessimistically lockable data block.
    /// </summary>
    public interface ILock
    {
        /// <summary>
        ///   Gets the current status of this lock.
        /// </summary>
        /// <value>The status.</value>
        LockStatus Status { get; }

        /// <summary>
        ///   Attempts migrate access from Open to Exclusive.
        /// </summary>
        /// <returns>The final state of the lock.</returns>
        LockStatus Close();

        /// <summary>
        ///   Attempts to release this lock from Exclusive to Open status.
        /// </summary>
        /// <returns>The final state of the lock.</returns>
        LockStatus Open();
    }
}