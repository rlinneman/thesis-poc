using Rel.Merge.Strategies;
using System.ComponentModel.DataAnnotations;

namespace Rel.Data.Models
{
    //[DirtyDelete]
    //[HiddenDelete]
    [LastWriteWins(false)]
    public class Asset
    {
        public int Id { get; set; }

        public int JobId { get; set; }

        //[DecaySpanMergeable("0.00:00:00.3", "0.00:00:00.3")]
        //[StepMergeable(true, 0.3)]
        //[LastWriteWins]
        public double? MaximumAndMinimumDecay { get; set; }

        //[DecaySpanMergeable("0.00:00:00.3", "0.00:00:00.3")]
        //[StepMergeable(true, 0.2)]
        public double? MaxMinDecayWithStepAndTol { get; set; }

        public double MinimumDecay { get; set; }

        [StepMergeable(0, 50, InclusiveLBound = false, InclusiveUBound = true)]
        public double? MonotonicTolerance { get; set; }

        //[LastWriteWins]
        public string Name { get; set; }

        [StepMergeable(true, 0.1, InclusiveLBound = true, InclusiveUBound = true)]
        public double? PercentTolerance { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; }

        public string ServiceArea { get; set; }

        [StepMergeable(50, InclusiveLBound = true, InclusiveUBound = true)]
        public double? StaticTolerance { get; set; }
    }
}