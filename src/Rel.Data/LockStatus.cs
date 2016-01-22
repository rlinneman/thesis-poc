using System;

namespace Rel.Data
{
    /// <summary>
    ///   Identifies the various states of a lock.
    /// </summary>
    [Flags]
    public enum LockStatus
    {
        /// <summary>
        ///   No lock is held.
        /// </summary>
        Open = 0x00000000,

        /// <summary>
        ///   A lock is held.
        /// </summary>
        Closed = 0x00000001,

        /// <summary>
        ///   The current user holds the lock.
        /// </summary>
        Exclusive = 0x00000003
    }
}