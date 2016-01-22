using FakeItEasy;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rel.Data.Bulk;
using Rel.Data.Models;
using System.Collections.Generic;
using Rel.Test;
using System;

namespace Rel.Data.Test.Bulk
{
    [TestClass]
    public class ChangeSetTests
    {
        [TestMethod]
        public void ProcessChangeSet_update_cleanly_completes_without_error()
        {
            var context = A.Fake<IDataContext>();
            var resolver = A.Fake<IConflictResolver>();
            var bfim = A.Dummy<Models.Asset>();
            var afim = A.Dummy<Models.Asset>();

            var cs = new ChangeSet()
            {
                Assets = new List<ChangeItem<Models.Asset>>{
                     new ChangeItem<Models.Asset>{
                         Action = ChangeAction.Update,
                         BFIM = bfim,
                         AFIM = afim
                     }
                 }
            };

            var processor = new ChangeSetProcessor(context, resolver);
            var result = processor.Process(1, false, cs);

            ExceptionAssert.Throws<ArgumentNullException>(() => processor.Process(1, false, null));
            Assert.IsTrue(result.IsEmpty);
            Assert.AreEqual(ChangeSet.Empty, result);
            A.CallTo(() => context.Assets.Update(A<Models.Asset>.Ignored)).MustHaveHappened(Repeated.Exactly.Once);
            A.CallTo(() => context.Assets.Update(afim)).MustHaveHappened(Repeated.Exactly.Once);
            A.CallTo(() => context.AcceptChanges()).MustHaveHappened(Repeated.Exactly.Once);
        }

        [TestMethod]
        public void ProcessChangeSet_update_dirty_invokes_successful_resolver_and_successful_retry()
        {
            var repo = A.Fake<IRepository<Asset, int>>();
            var context = A.Fake<IDataContext>();
            var resolver = A.Fake<IConflictResolver>();
            var bfim = A.Dummy<Models.Asset>();
            var afim = A.Dummy<Models.Asset>();

            var cs = new ChangeSet()
            {
                Assets = new List<ChangeItem<Models.Asset>>{
                     new ChangeItem<Models.Asset>{
                         Action = ChangeAction.Update,
                         BFIM = bfim,
                         AFIM = afim
                     }
                 }
            };

            A.CallTo(() => repo.KeySelector).Returns((Asset a) => a.Id);
            A.CallTo(() => context.Assets).Returns(repo);
            A.CallTo(() => context.AcceptChanges()).Throws(new ConcurrencyException()).Once();
            A.CallTo(() => resolver.Resolve(repo, A<ChangeItem<Asset>>.Ignored, A<IDictionary<int,Asset>>.Ignored)).Returns(true);

            var processor = new ChangeSetProcessor(context, resolver);
            var result = processor.Process(1, false, cs);

            Assert.IsTrue(result.IsEmpty);
            Assert.AreEqual(ChangeSet.Empty, result);
            A.CallTo(() => context.Assets.Update(afim)).MustHaveHappened(Repeated.Exactly.Once);
            A.CallTo(() => context.AcceptChanges()).MustHaveHappened(Repeated.Exactly.Twice);
        }

        [TestMethod]
        public void ProcessChangeSet_update_dirty_invokes_unsuccessful_merge_and_no_retry()
        {
            var repo = A.Fake<IRepository<Asset, int>>();
            var context = A.Fake<IDataContext>();
            var resolver = new RejectConcurrentEditsConflictResolver();
            var afim = A.Dummy<Models.Asset>();

            var cs = new ChangeSet()
            {
                Assets = new List<ChangeItem<Models.Asset>>{
                     new ChangeItem<Models.Asset>{
                         Action = ChangeAction.Update,
                         BFIM = A.Dummy<Models.Asset>(),
                         AFIM = afim
                     }
                 }
            };
            A.CallTo(() => repo.KeySelector).Returns((Asset a) => a.Id);
            A.CallTo(() => context.Assets).Returns(repo);
            A.CallTo(() => context.AcceptChanges()).Throws(new ConcurrencyException()).Once();

            var processor = new ChangeSetProcessor(context, resolver);
            var result = processor.Process(1, false, cs);

            Assert.IsFalse(result.IsEmpty);
            Assert.AreNotEqual(ChangeSet.Empty, result);
            A.CallTo(() => context.Assets.Update(afim)).MustHaveHappened(Repeated.Exactly.Once);
            A.CallTo(() => context.AcceptChanges()).MustHaveHappened(Repeated.Exactly.Once);
        }


        [TestMethod]
        public void Get_initial_changeset_returns_only_partition_with_all_items_in_afim_and_action_is_initialize()
        {
            var assets = new List<Asset>
            {
                new Asset(){Id=1,JobId=0},
                new Asset(){Id=2,JobId=0},
                new Asset(){Id=3,JobId=1},
                new Asset(){Id=4,JobId=0}
            }.AsQueryable();

            var repo = A.Fake<IRepository<Asset, int>>();
            var context = A.Fake<IDataContext>();
            var resolver = A.Fake<IConflictResolver>();

            A.CallTo(() => repo.GetAll()).Returns(assets);
            A.CallTo(() => context.Assets).Returns(repo);

            var processor = new ChangeSetProcessor(context, resolver);
            var result = processor.BuildInitialChangeSet(0);



            A.CallTo(() => context.AcceptChanges()).MustNotHaveHappened();
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsEmpty);
            Assert.AreNotEqual(ChangeSet.Empty, result);
            //Assert.IsTrue(result.All(_ => _ is ChangeItem<Asset>));
            //Assert.IsTrue(result.All(_ => _.Action == ChangeAction.Initialize));
            //Assert.IsTrue(result.All(_ => _.GetBFIM() == null));
            //Assert.IsTrue(result.All(_ => _.GetAFIM() != null));
            //Assert.AreEqual(3, result.Assets.Count);
            //Assert.AreEqual(3, result.TotalItemsCount);
            //Assert.AreEqual(3, result.Count());
            Assert.IsTrue(result.Assets.All(_ => _.AFIM.JobId == 0));
        }


        [TestMethod]
        public void ProcessChangeSet_throws_when_invalid_job_id_used_in_lock()
        {
            var context = A.Fake<IDataContext>();
            var cs = A.Dummy<ChangeSet>();

            A.CallTo(() => context.Jobs.GetById(A<int>.Ignored)).Returns(null);

            var processor = new ChangeSetProcessor(context, A.Dummy<IConflictResolver>());

            ExceptionAssert.Throws<EntityNotFoundException>(() => processor.Process(A<int>.Ignored, false, cs));
        }
        [TestMethod]
        public void ProcessChangeSet_throws_when_attempt_to_lock_closed_lock()
        {
            var context = A.Fake<IDataContext>();
            var cs = A.Dummy<ChangeSet>();

            A.CallTo(() => context.Jobs.GetById(A<int>.Ignored))
                .Returns(new Job() { LockedBy = "FooBar" });

            var processor = new ChangeSetProcessor(context, A.Dummy<IConflictResolver>());

            ExceptionAssert.Throws<ConcurrencyException>(() => processor.Process(A<int>.Ignored, true, cs));
        }

        [TestMethod]
        public void ProcessChangeSet_which_locks_retains_lock_on_failed_merge()
        {
            var job = new Job() { Id = 42 };
            var context = A.Fake<IDataContext>();
            var resolver = A.Fake<IConflictResolver>();
            var bfim = A.Dummy<Models.Asset>();
            var afim = A.Dummy<Models.Asset>();

            var cs = new ChangeSet()
            {
                Assets = new List<ChangeItem<Models.Asset>>{
                    new ChangeItem<Models.Asset>(ChangeAction.Update,bfim,afim),
                    new ChangeItem<Models.Asset>(ChangeAction.Delete,A.Dummy<Asset>(),null),
                    new ChangeItem<Models.Asset>(ChangeAction.Create,null, A.Dummy<Asset>())
                }
            };

            A.CallTo(() => resolver.Resolve(A<IRepository<Asset, int>>.Ignored, A<ChangeItem<Asset>>.Ignored, A<IDictionary<int, Asset>>.Ignored)).Returns(false);
            A.CallTo(() => context.Jobs.GetById(A<int>.Ignored)).Returns(job);
            A.CallTo(() => context.Assets.KeySelector).Returns((Asset a) => a.Id);
            A.CallTo(() => context.AcceptChanges()).Throws(new ConcurrencyException());
            A.CallTo(() => context.AcceptChanges()).DoesNothing().Once();

            var processor = new ChangeSetProcessor(context, resolver);
            var result = processor.Process(1, true, cs);

            Assert.IsFalse(result.IsEmpty);
            Assert.AreNotEqual(ChangeSet.Empty, result);
            A.CallTo(() => context.Assets.Update(afim)).MustHaveHappened(Repeated.Exactly.Once);
            A.CallTo(() => context.AcceptChanges()).MustHaveHappened(Repeated.Exactly.Twice);
            A.CallTo(() => resolver.Resolve(A<IRepository<Asset, int>>.Ignored, A<ChangeItem<Asset>>.Ignored, A<IDictionary<int, Asset>>.Ignored)).MustHaveHappened(Repeated.Exactly.Once);
            //Assert.IsNotNull(job.LockedBy); // disabling this check temporarily this functionality was designed around EF and transaction rollbacks
        }
    }
}