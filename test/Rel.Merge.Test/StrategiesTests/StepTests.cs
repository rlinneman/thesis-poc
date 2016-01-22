using FakeItEasy;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rel.Merge.Strategies;
using Rel.Test;
using System;

namespace Rel.Merge.Test.StrategiesTests
{
    [TestClass]
    public class StepTests
    {
        [TestMethod]
        public void Numeric_mergeable_requires_update_conflict()
        {
            var step = new StepMergeableAttribute(-1, 2);

            ExceptionAssert.Throws<InvalidOperationException>(() =>
                step.Merge(new MergeAction<int>(MergeKind.Auto, A<int>.Ignored, A<int>.Ignored, A<int>.Ignored)));

            ExceptionAssert.Throws<InvalidOperationException>(() =>
                step.Merge(new MergeAction<int>(MergeKind.DirtyDelete, A<int>.Ignored, A<int>.Ignored, A<int>.Ignored)));

            ExceptionAssert.Throws<InvalidOperationException>(() =>
                step.Merge(new MergeAction<int>(MergeKind.HiddenDelete, A<int>.Ignored, A<int>.Ignored, A<int>.Ignored)));

            ExceptionAssert.Throws<ArgumentException>(() => new StepMergeableAttribute(5, 0));
        }

        [TestMethod]
        public void Rollup()
        {
            var step = new StepMergeableAttribute(-3, 2) { InclusiveLBound = true, InclusiveUBound = true };
            Assert.AreEqual(false, step.IsPercentageBased);
            Assert.AreEqual(-3, step.LowerBound);
            Assert.AreEqual(2, step.UpperBound);


            TestPass(step, 42, 10, 11);
            TestPass(step, 42, 10, 12);
            TestPass(step, 42, 10, 13, MergeActionResult.Unresolved);


            TestPass(step, 42, 10, 8);
            TestPass(step, 42, 10, 7);
            TestPass(step, 42, 10, 6, MergeActionResult.Unresolved);


            step = new StepMergeableAttribute(-3, 2) { InclusiveLBound = false, InclusiveUBound = false};
            TestPass(step, 42, 10, 11);
            TestPass(step, 42, 10, 12, MergeActionResult.Unresolved);
            TestPass(step, 42, 10, 13, MergeActionResult.Unresolved);


            TestPass(step, 42, 10, 8);
            TestPass(step, 42, 10, 7, MergeActionResult.Unresolved);
            TestPass(step, 42, 10, 6, MergeActionResult.Unresolved);


            step = new StepMergeableAttribute(5) { InclusiveLBound = true, InclusiveUBound = true };
            TestPass(step, 42, 10, 14);
            TestPass(step, 42, 10, 15);
            TestPass(step, 42, 10, 16, MergeActionResult.Unresolved);


            TestPass(step, 42, 10, 6);
            TestPass(step, 42, 10, 5);
            TestPass(step, 42, 10, 4, MergeActionResult.Unresolved);




            step = new StepMergeableAttribute(true, -0.3, 0.2) { InclusiveLBound = true, InclusiveUBound = true };
            TestPass(step, 42, 100, 119);
            TestPass(step, 42, 100, 120);
            TestPass(step, 42, 100, 120.3, MergeActionResult.Unresolved);


            TestPass(step, 42, 100, 71);
            TestPass(step, 42, 100, 70);
            TestPass(step, 42, 100, 69, MergeActionResult.Unresolved);


            step = new StepMergeableAttribute(true, -0.3, 0.2) { InclusiveLBound = false, InclusiveUBound = false };
            TestPass(step, 42, 100, 119);
            TestPass(step, 42, 100, 120, MergeActionResult.Unresolved);
            TestPass(step, 42, 100, 120.3, MergeActionResult.Unresolved);


            TestPass(step, 42, 100, 71);
            TestPass(step, 42, 100, 70, MergeActionResult.Unresolved);
            TestPass(step, 42, 100, 69, MergeActionResult.Unresolved);


            step = new StepMergeableAttribute(true, 0.5) { InclusiveLBound = true, InclusiveUBound = true };
            TestPass(step, 42, 100, 149);
            TestPass(step, 42, 100, 150);
            TestPass(step, 42, 100, 151, MergeActionResult.Unresolved);


            TestPass(step, 42, 100, 51);
            TestPass(step, 42, 100, 50);
            TestPass(step, 42, 100, 49, MergeActionResult.Unresolved);



            step = new StepMergeableAttribute(true, 0.5) { InclusiveLBound = true, InclusiveUBound = true, DivideByZeroOk=true };
            TestPass(step, 42, 0, 149);
            step = new StepMergeableAttribute(true, 0.5) { InclusiveLBound = true, InclusiveUBound = true};
            TestPass(step, 42, 0, 149, MergeActionResult.Unresolved);

        }

        [System.Diagnostics.DebuggerStepThrough]
        private void TestPass<T>(StepMergeableAttribute step, T bfim, T current, T afim, MergeActionResult expectedResult = MergeActionResult.Update)
        {
            var action = new MergeAction<T>(MergeKind.ConflictingUpdate, bfim, current, afim);
            step.Merge(action);
            Assert.AreEqual(expectedResult, action.Result);
        }
    }
}