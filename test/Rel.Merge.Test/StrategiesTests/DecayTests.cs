using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rel.Merge.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rel.Merge.Test.StrategiesTests
{
    [TestClass]
    public class DecayTests
    {


        [TestMethod]
        public void Rollup()
        {
            var decay = new DecaySpanMergeableAttribute("0.00:00:02", "0.00:00:01") { InclusiveLBound = true, InclusiveUBound = true };
            Assert.AreEqual(DateTimeKind.Local, decay.FieldDateTimeKind);
            Assert.AreEqual(TimeSpan.FromSeconds(1), decay.LowerBound);
            Assert.AreEqual(TimeSpan.FromSeconds(2), decay.UpperBound);
            Assert.IsTrue(decay.InclusiveLBound);
            Assert.IsTrue(decay.InclusiveUBound);

            TestPass(decay, DateTime.Now.AddMilliseconds(-1200));
            TestPass(decay, DateTime.Now.AddMilliseconds(-1000));
            TestPass(decay, DateTime.Now.AddMilliseconds(-2000));
            TestPass(decay, DateTime.Now.AddMilliseconds(-300), MergeActionResult.Unresolved);
            TestPass(decay, DateTime.Now.AddMilliseconds(-3000), MergeActionResult.Unresolved);

            decay = new DecaySpanMergeableAttribute("0.00:00:03" ) {  FieldDateTimeKind= DateTimeKind.Utc };
            Assert.AreEqual(DateTimeKind.Utc, decay.FieldDateTimeKind);
            Assert.AreEqual(TimeSpan.FromSeconds(0), decay.LowerBound);
            Assert.AreEqual(TimeSpan.FromSeconds(3), decay.UpperBound);
            Assert.IsFalse(decay.InclusiveLBound);
            Assert.IsFalse(decay.InclusiveUBound);


            TestPass(decay, DateTime.SpecifyKind(DateTime.UtcNow.AddMilliseconds(-300), DateTimeKind.Unspecified));
            TestPass(decay, DateTime.Now.AddMilliseconds(-1200));
            TestPass(decay, DateTime.UtcNow.AddMilliseconds(-2000));
            TestPass(decay, DateTime.Now.AddMilliseconds(-3000), MergeActionResult.Unresolved);
            TestPass(decay, DateTime.Now.AddMilliseconds(-4000), MergeActionResult.Unresolved);


        }

        [System.Diagnostics.DebuggerStepThrough]
        private void TestPass(DecaySpanMergeableAttribute step, DateTime current, MergeActionResult expectedResult = MergeActionResult.Update)
        {
            var action = new MergeAction<DateTime>(MergeKind.ConflictingUpdate, DateTime.MaxValue, current, DateTime.MaxValue);
            step.Merge(action);
            Assert.AreEqual(expectedResult, action.Result);
        }
    }
}
