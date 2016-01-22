using FakeItEasy;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rel.Merge.Strategies;
using Rel.Test;
using System;

namespace Rel.Merge.Test.StrategiesTests
{
    [TestClass]
    public class LastWriteWinsTests
    {
        [TestMethod]
        public void Dirty_delete_resolves_with_delete()
        {
            var action = new MergeAction<object>(MergeKind.DirtyDelete, A.Dummy<object>(), null, null);
            var p = new LastWriteWinsAttribute(true);

            p.Merge(action);

            Assert.IsTrue(p.DoesLastWriteWin);
            Assert.AreEqual(MergeActionResult.Delete, action.Result);
            Assert.AreEqual(null, action.ResolvedValue);
        }

        [TestMethod]
        public void Dirty_update_resolves_with_update()
        {
            var update = A.Dummy<object>();
            var action = new MergeAction<object>(MergeKind.ConflictingUpdate, A.Dummy<object>(), null, update);
            var p = new LastWriteWinsAttribute(true);

            p.Merge(action);

            Assert.IsTrue(p.DoesLastWriteWin);
            Assert.AreEqual(MergeActionResult.Update, action.Result);
            Assert.AreEqual(update, action.ResolvedValue);
        }

        [TestMethod]
        public void Is_not_last_write_wins_immediately_rejects()
        {
            var action = A.Fake<MergeAction<object>>();
            var p = new LastWriteWinsAttribute(false);

             p.Merge(action);


            Assert.IsFalse(p.DoesLastWriteWin);
            Assert.AreEqual(MergeActionResult.Unresolved, action.Result);
        }

        [TestMethod]
        public void Merge_delete_on_hidden_delete_results_in_noop_resolution()
        {
            var action = new MergeAction<object>(MergeKind.HiddenDelete, A.Dummy<object>(), null, null);
            var p = new LastWriteWinsAttribute(true);

            p.Merge(action);

            Assert.IsTrue(p.DoesLastWriteWin);
            Assert.AreEqual(MergeActionResult.Resolved, action.Result);
            Assert.AreEqual(null, action.ResolvedValue);
        }

        [TestMethod]
        public void Merge_update_on_hidden_delete_results_in_create_resolution()
        {
            var restoreWith = A.Dummy<object>();

            var action = new MergeAction<object>(MergeKind.HiddenDelete, A.Dummy<object>(), null, restoreWith);
            var p = new LastWriteWinsAttribute(true);

            p.Merge(action);

            Assert.IsTrue(p.DoesLastWriteWin);
            Assert.AreEqual(MergeActionResult.Create, action.Result);
            Assert.AreEqual(restoreWith, action.ResolvedValue);
        }

        [TestMethod]
        public void Rejects_Auto_Actions()
        {
            var action = new MergeAction<object>(MergeKind.Auto,null,null,null);
            var p = new LastWriteWinsAttribute(true);

            ExceptionAssert.Throws<NotSupportedException>(() => p.Merge(action));


            Assert.IsTrue(p.DoesLastWriteWin);
            Assert.AreEqual(MergeActionResult.Unresolved, action.Result);
            Assert.IsFalse(action.Resolved);
        }
        [TestMethod]
        public void Rejects_Unknown_Actions()
        {
            var action = new MergeAction<object>((MergeKind)42, null, null, null);
            var p = new LastWriteWinsAttribute(true);

            ExceptionAssert.Throws<ArgumentException>(() => p.Merge(action));


            Assert.IsTrue(p.DoesLastWriteWin);
            Assert.AreEqual(MergeActionResult.Unresolved, action.Result);
            Assert.IsFalse(action.Resolved);
        }
    }
}