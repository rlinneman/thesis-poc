using FakeItEasy;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rel.Test;
using System;

namespace Rel.Merge.Test
{
    [TestClass]
    public class MergeOperationTest
    {
        private byte[] _ts1, _ts2;

        [TestMethod]
        public void PerfCounters()
        {
            var op = new MergeOperation();
            op.Merge(
                    MergeKind.Auto,
                    A.Dummy<MergeEntity>(),
                    A.Dummy<MergeEntity>(),
                    A.Dummy<MergeEntity>());
        }

        [TestMethod]
        public void Bad_entity_configurations_throw()
        {
            var op = new MergeOperation();

            ExceptionAssert.Throws<TypeInitializationException>(
                _ =>
                    _.InnerException != null && _.InnerException is MergeException,
                () =>
                    op.Merge(
                    MergeKind.Auto,
                    A.Dummy<InvalidMergeDefEntityBadPropertyImpl>(),
                    A.Dummy<InvalidMergeDefEntityBadPropertyImpl>(),
                    A.Dummy<InvalidMergeDefEntityBadPropertyImpl>()));

            ExceptionAssert.Throws<TypeInitializationException>(
                _ =>
                    _.InnerException != null && _.InnerException is MergeException,
                () =>
                    op.Merge(
                    MergeKind.Auto,
                    A.Dummy<InvalidMergeDefEntityBadDecoration>(),
                    A.Dummy<InvalidMergeDefEntityBadDecoration>(),
                    A.Dummy<InvalidMergeDefEntityBadDecoration>()));

            ExceptionAssert.Throws<TypeInitializationException>(
                _ =>
                    _.InnerException != null && _.InnerException is MergeException,
                () =>
                    op.Merge(
                    MergeKind.Auto,
                    A.Dummy<InvalidMergeDefEntityBadTimeStampType>(),
                    A.Dummy<InvalidMergeDefEntityBadTimeStampType>(),
                    A.Dummy<InvalidMergeDefEntityBadTimeStampType>()));

            //ExceptionAssert.Throws<TypeInitializationException>(
            //    _ =>
            //        _.InnerException != null && _.InnerException is MergeException,
            //    () =>
            op.Merge(
            MergeKind.Auto,
            A.Dummy<InvalidMergeDefEntityOverloadedDecoration>(),
            A.Dummy<InvalidMergeDefEntityOverloadedDecoration>(),
            A.Dummy<InvalidMergeDefEntityOverloadedDecoration>());
            //);
        }

        [TestMethod]
        public void CCValidator()
        {
            Assert.IsTrue(MergeOperation<OccEntity>.TimestampIsCurrentChecker(_ts1, _ts1));
            Assert.IsFalse(MergeOperation<OccEntity>.TimestampIsCurrentChecker(_ts1, _ts2));
            Assert.IsFalse(MergeOperation<OccEntity>.TimestampIsCurrentChecker(null, _ts2));
            Assert.IsFalse(MergeOperation<OccEntity>.TimestampIsCurrentChecker(_ts1, null));
            Assert.IsTrue(MergeOperation<OccEntity>.TimestampIsCurrentChecker(null, null));
            Assert.IsTrue(MergeOperation<OccEntity>.TimestampIsCurrentChecker(new byte[2], new byte[2]));
            Assert.IsFalse(MergeOperation<OccEntity>.TimestampIsCurrentChecker(new byte[3], new byte[2]));
            Assert.IsTrue(MergeOperation<OccEntity>.TimestampIsCurrentChecker(new byte[8], new byte[8] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }));
            Assert.IsFalse(MergeOperation<OccEntity>.TimestampIsCurrentChecker(new byte[8], new byte[8] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF }));
            Assert.IsFalse(MergeOperation<OccEntity>.TimestampIsCurrentChecker(new byte[8], new byte[8] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF }));
            Assert.IsFalse(MergeOperation<OccEntity>.TimestampIsCurrentChecker(new byte[8], new byte[8] { 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF }));
            Assert.IsFalse(MergeOperation<OccEntity>.TimestampIsCurrentChecker(new byte[8], new byte[8] { 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF }));
            Assert.IsFalse(MergeOperation<OccEntity>.TimestampIsCurrentChecker(new byte[8], new byte[8] { 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }));
            Assert.IsFalse(MergeOperation<OccEntity>.TimestampIsCurrentChecker(new byte[8], new byte[8] { 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }));
            Assert.IsFalse(MergeOperation<OccEntity>.TimestampIsCurrentChecker(new byte[8], new byte[8] { 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }));
            Assert.IsFalse(MergeOperation<OccEntity>.TimestampIsCurrentChecker(new byte[8], new byte[8] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }));
        }


        [TestMethod]
        public void Dirty_update_merges_on_type_where_not_all_properties_can_merge_resolves()
        {
            var bfim = new MergeTypeEntity { Timestamp = _ts1, Name = "foo", Id = 1 };
            var cfim = new MergeTypeEntity { Timestamp = _ts2, Name = "bar", Id = 1 };
            var afim = new MergeTypeEntity { Timestamp = _ts1, Name = "baz", Id = 1 };

            var op = new MergeOperation<MergeTypeEntity>(bfim, cfim, afim);
            var result = op.Merge();

            Assert.IsTrue(result.IsResolved());
            //Assert.AreEqual(cfim, op.AFIM);
            Assert.AreEqual("foo", bfim.Name);
            Assert.AreEqual("baz", cfim.Name);
            Assert.AreEqual("baz", afim.Name);

            Assert.AreEqual(_ts1, bfim.Timestamp);
            Assert.AreEqual(_ts2, cfim.Timestamp);
            Assert.AreEqual(_ts1, afim.Timestamp);
        }

        [TestMethod]
        public void Dirty_update_property_merge_resolves()
        {
            var bfim = new MergeEntity { Timestamp = _ts1, Name = "foo", Id = 1 };
            var cfim = new MergeEntity { Timestamp = _ts2, Name = "bar", Id = 1 };
            var afim = new MergeEntity { Timestamp = _ts1, Name = "baz", Id = 1 };

            var op = new MergeOperation<MergeEntity>(bfim, cfim, afim);
            var result = op.Merge();

            Assert.IsTrue(result.IsResolved());
            //Assert.AreEqual(cfim, op.AFIM);
            Assert.AreEqual("foo", bfim.Name);
            Assert.AreEqual("baz", cfim.Name);
            Assert.AreEqual("baz", afim.Name);

            Assert.AreEqual(_ts1, bfim.Timestamp);
            Assert.AreEqual(_ts2, cfim.Timestamp);
            Assert.AreEqual(_ts1, afim.Timestamp);
        }

        [TestInitialize]
        public void Initialize()
        {
            _ts1 = new byte[8] { 0x00, 0xBA, 0xB1, 0x0C, 0xDE, 0xAD, 0xBE, 0xEF };
            _ts2 = new byte[8] { 0xFE, 0xE1, 0xDE, 0xAD, 0xC0, 0x00, 0x10, 0xFF };
        }

        [TestMethod]
        public void Non_OC_results_in_noop()
        {
            var bfim = A.Dummy<ChaosEntity>();
            var cfim = A.Dummy<ChaosEntity>();
            var afim = A.Dummy<ChaosEntity>();

            var op = new MergeOperation<ChaosEntity>(bfim, cfim, afim);
            var result = op.Merge();

            Assert.IsTrue(result.IsResolved());
            Assert.AreEqual(afim, op.AFIM);
            A.CallTo(() => bfim.Id).MustNotHaveHappened();
            A.CallTo(() => bfim.Name).MustNotHaveHappened();
            A.CallTo(() => cfim.Id).MustNotHaveHappened();
            A.CallTo(() => cfim.Name).MustNotHaveHappened();
            A.CallTo(() => afim.Id).MustNotHaveHappened();
            A.CallTo(() => afim.Name).MustNotHaveHappened();
        }

        [TestMethod]
        public void OC_create_resolves()
        {
            OccEntity bfim = null;
            var cfim = new OccEntity { Timestamp = _ts1, Name = "bar", Id = 1 };
            var afim = new OccEntity { Timestamp = _ts2, Name = "baz", Id = 1 };

            var op = new MergeOperation<OccEntity>(bfim, cfim, afim);
            var result = op.Merge();

            Assert.IsTrue(result.IsResolved());
            Assert.AreEqual(afim, op.AFIM);
            Assert.AreEqual("bar", cfim.Name);
            Assert.AreEqual("baz", afim.Name);

            Assert.AreEqual(_ts1, cfim.Timestamp);
            Assert.AreEqual(_ts2, afim.Timestamp);
        }

        [TestMethod]
        public void OC_is_current_resolves()
        {
            var bfim = new OccEntity { Timestamp = _ts1, Name = "foo", Id = 1 };
            var cfim = new OccEntity { Timestamp = _ts1, Name = "bar", Id = 1 };
            var afim = new OccEntity { Timestamp = _ts1, Name = "baz", Id = 1 };

            var op = new MergeOperation<OccEntity>(bfim, cfim, afim);
            var result = op.Merge();

            Assert.IsTrue(result.IsResolved());
            Assert.AreEqual("foo", bfim.Name);
            Assert.AreEqual("bar", cfim.Name);
            Assert.AreEqual("baz", afim.Name);

            Assert.AreEqual(_ts1, bfim.Timestamp);
            Assert.AreEqual(_ts1, cfim.Timestamp);
            Assert.AreEqual(_ts1, afim.Timestamp);
        }

        /*
         *
         * THESE ARE TESTS FIT FOR ATTR TESTS
         *
         *
        [TestMethod]
        public void OC_paricipant_hidden_delete_resolves_to_create()
        {
            var bfim = new OccEntity { Timestamp = _ts1, Name = "foo", Id = 1 };
            OccEntity cfim = null;
            var afim = new OccEntity { Timestamp = _ts2, Name = "baz", Id = 1 };

            var op = new MergeOperation<OccEntity>(bfim, cfim, afim);
            var result = op.Merge();

            Assert.IsTrue(result);
            Assert.AreEqual(afim, op.AFIM);
            Assert.AreEqual("foo", bfim.Name);
            Assert.AreEqual("baz", afim.Name);

            Assert.AreEqual(_ts1, bfim.Timestamp);;
            Assert.AreEqual(_ts2, afim.Timestamp);
        }

        [TestMethod]
        public void OC_paricipant_hidden_delete_resolves_to_noop()
        {
            var bfim = new OccEntity { Timestamp = _ts1, Name = "foo", Id = 1 };
            OccEntity cfim = null;
            var afim = new OccEntity { Timestamp = _ts2, Name = "baz", Id = 1 };

            var op = new MergeOperation<OccEntity>(bfim, cfim, afim);
            var result = op.Merge();

            Assert.IsTrue(result);
            Assert.AreEqual(afim, op.AFIM);
            Assert.AreEqual("foo", bfim.Name);
            Assert.AreEqual("baz", afim.Name);

            Assert.AreEqual(_ts1, bfim.Timestamp); ;
            Assert.AreEqual(_ts2, afim.Timestamp);
        }
        */

        /*
        [TestMethod]
        public void Last_Write_wins_property_merges()
        {
            var orig = new LastWriteWinsPropertyEntity() { Id = 1, Name = "A Name", Timestamp = _ts1 };
            var current = new LastWriteWinsPropertyEntity() { Id = 1, Name = "A new Name", Timestamp = _ts2 };
            var mod = new LastWriteWinsPropertyEntity() { Id = 1, Name = "The next name", Timestamp = _ts1 };
            var test = mod;

            var op = new MergeOperation();
            op.Merge(orig, current, ref mod);

            Assert.AreEqual(current, mod);
            Assert.AreNotEqual(test, mod);
            Assert.AreEqual(mod.Name, current.Name);
            Assert.AreEqual(current.Timestamp, _ts2);
        }

        [TestMethod]
        public void NonMergeable_falls_back_to_implicit_last_write_wins()
        {
            var orig = new NonMergeableFauxEntity() { Id = 1, Name = "A Name" };
            var current = new NonMergeableFauxEntity() { Id = 1, Name = "A new Name" };
            var mod = new NonMergeableFauxEntity() { Id = 1, Name = "The next name" };
            var op = new MergeOperation();
            var test = mod;

            var result = op.Merge(orig, current, ref test);

            Assert.IsTrue(result);
            Assert.AreEqual(mod, test);
        }
         * */
    }
}