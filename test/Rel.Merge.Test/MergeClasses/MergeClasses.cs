using Rel.Merge.Strategies;
using System;
using System.ComponentModel.DataAnnotations;

namespace Rel.Merge.Test
{
    /// <summary>
    ///   Entity does not participate in concurrency control of any kind.
    /// </summary>
    public class ChaosEntity
    {
        [Key]
        public virtual int Id { get; set; }

        public virtual string Name { get; set; }
    }

    internal class InvalidMergeDefEntityBadDecoration
    {
        public int Id { get; set; }

        [LastWriteWins]
        public string Name { get; set; }

        [Timestamp]
        public object Timestamp { get; set; }
    }

    internal class InvalidMergeDefEntityBadPropertyImpl
    {
        public int Id { get; set; }

        [LastWriteWins]
        public string Name { get; set; }

        [Timestamp]
        public byte[] Timestamp { set { throw new NotSupportedException(); } }
    }

    internal class InvalidMergeDefEntityOverloadedDecoration
    {
        public int Id { get; set; }

        [LastWriteWins]
        public string Name { get; set; }

        [Timestamp]
        [LastWriteWins]
        public byte[] Timestamp { get; set; }
    }
    internal class InvalidMergeDefEntityBadTimeStampType
    {
        public int Id { get; set; }

        [LastWriteWins]
        public string Name { get; set; }

        [Timestamp]
        public object Timestamp { get; set; }
    }

    [LastWriteWins]
    internal class LastWriteWinsGlobalEntity
    {
        public int Id { get; set; }

        public string Name { get; set; }

        [Timestamp]
        public byte[] Timestamp { get; set; }
    }

    /// <summary>
    ///   Entity participates in optimistic concurrency control, but
    ///   does not participate in merge.
    /// </summary>
    internal class OccEntity : ChaosEntity
    {
        [Timestamp]
        public byte[] Timestamp { get; set; }
    }
    /// <summary>
    ///   Entity participates in optimistic concurrency control, but
    ///   does not participate in merge.
    /// </summary>
    internal class MergeEntity : OccEntity
    {
        [LastWriteWins]
        public override string Name
        {
            get
            {
                return base.Name;
            }
            set
            {
                base.Name = value;
            }
        }
    }

    /// <summary>
    ///   Entity participates in optimistic concurrency control, but
    ///   does not participate in merge.
    /// </summary>
    [LastWriteWins]
    internal class MergeTypeEntity : OccEntity
    {
        
    }
}