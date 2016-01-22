using Rel.Data.Bulk;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Rel.Data.Models
{
    public class Job : ILock
    {
        private string _lockedBy;

        public virtual ICollection<Asset> Assets { get; set; }
        public string City { get; set; }
        public int Id { get; set; }

        LockStatus ILock.Status
        {
            get
            {
                // the interlocked thread safety is a bit overkill here.
                // Especially since the behavior is not honored by the
                // actual LockedBy property. However this makes the
                // intent of the interface more explicit.

                var user = System.Threading.Thread.CurrentPrincipal.Identity;
                var lockedBy = Interlocked.CompareExchange(ref _lockedBy, null, null);
                if (lockedBy == null)
                    return LockStatus.Open;

                if (user.IsAuthenticated && user.Name == lockedBy)
                    return LockStatus.Exclusive;

                return LockStatus.Closed;
            }
        }

        public string LockedBy { get { return _lockedBy; } set { _lockedBy = value; } }
        public DateTime? LockedOn { get; set; }
        public string Name { get; set; }
        public string PostalCode { get; set; }
        public byte[] RowVersion { get; set; }
        public string State { get; set; }
        public string Street1 { get; set; }
        public string Street2 { get; set; }

        LockStatus ILock.Close()
        {
            var user = System.Threading.Thread.CurrentPrincipal.Identity;
            if (!user.IsAuthenticated)
                throw Error.AnonymousUserAccess();

            var lockedBy = Interlocked.CompareExchange(ref _lockedBy, user.Name, null);

            if (lockedBy == null || lockedBy == user.Name)
                // either noone held a lock or the current user
                // already did. Either way, the current user now holds
                // exclusive access.
                return LockStatus.Exclusive;

            return LockStatus.Closed;
        }

        LockStatus ILock.Open()
        {
            var user = System.Threading.Thread.CurrentPrincipal.Identity;
            if (!user.IsAuthenticated)
                throw Error.AnonymousUserAccess();

            var lockedBy = Interlocked.CompareExchange(ref _lockedBy, null, user.Name);

            if (lockedBy == null || lockedBy == user.Name)
                // either noone held a lock or the current user
                // already did. Either way, the lock is now open.
                return LockStatus.Open;

            return LockStatus.Closed;
        }

    }
}