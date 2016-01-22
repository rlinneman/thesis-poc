using FakeItEasy;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rel.Data.Bulk;
using Rel.Data.Models;
using Rel.Merge;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rel.Data.Test.Bulk
{

    [TestClass]
    public class MergeConflictTests
    {
        private IMergeProvider Provider<T>(IMergeResolution<T> resolution = null)
        {
            var provider = A.Fake<IMergeProvider>();
            if (resolution == null)
            {
                resolution = A.Fake<IMergeResolution<T>>();
            }

            A.CallTo(() => provider.Merge<T>(
                A<MergeKind>.Ignored,
                A<T>.Ignored,
                A<T>.Ignored,
                A<T>.Ignored))
                .Returns(resolution);
            return provider;
        }
        private IMergeProvider Provider<T>(IMergeProvider provider, IMergeResolution<T> resolution)
        {
            if (provider == null)
            {
                provider = A.Fake<IMergeProvider>();
            }
            if (resolution == null)
            {
                resolution = A.Fake<IMergeResolution<T>>();
            }

            A.CallTo(() => provider.Merge<T>(
                A<MergeKind>.Ignored,
                A<T>.Ignored,
                A<T>.Ignored,
                A<T>.Ignored))
                .Returns(resolution);
            return provider;
        }
        private IMergeResolution<T> Resolution<T>(MergeActionResult result, T value=default(T))
        {
            var resolution = A.Fake<IMergeResolution<T>>();
            A.CallTo(() => resolution.Result).Returns(result);
            A.CallTo(() => resolution.ResolvedValue).Returns(value);

            return resolution;
        }
        private IDictionary<TKey, TEntity> Index<TKey, TEntity>(Func<TEntity, TKey> key, params TEntity[] items)
        {
            var stub = A<TEntity>.Ignored;
            var index = A.Fake<IDictionary<TKey,TEntity>>();
            //A.CallTo(() => index.TryGetValue(A<TKey>.Ignored, out stub)).
                
            foreach (var item in items)
            {
                A.CallTo(() => index.TryGetValue(key(item), out stub))
                    .Returns(true)
                    .AssignsOutAndRefParameters(item);
                    //.AssignsOutAndRefParameters(default(TEntity))
            }
            return index;
        }
        private IRepository<T, TKey> Repository<T, TKey>(Func<T, TKey> key, params T[] items)
        {
            var repo = A.Fake<IRepository<T, TKey>>();
            A.CallTo(() => repo.GetAll()).Returns(items.AsQueryable());
            foreach (var item in items)
            {
                A.CallTo(() => repo.GetById(key(item))).Returns(item);
            }
            return repo;
        }

        [TestMethod]
        public void Dirty_merged_update_resolves()
        {
            Asset bfim = A.Dummy<Asset>(),
                current = A.Dummy<Asset>(),
                afim = A.Dummy<Asset>();

            var index = Index(_ => _.Id, current);
            var repository = Repository(_ => _.Id, current);
            var resolution = Resolution(MergeActionResult.Update, afim);
            var provider = Provider(resolution);
            var change = new ChangeItem<Asset>(ChangeAction.Update, bfim, afim);


            var resolver = new MergeConcurrentEditsConflictResolver(provider);
            var result = resolver.Resolve(repository, change, index);

            Assert.IsTrue(result);
            A.CallTo(() => repository.Update(afim)).MustHaveHappened();
            A.CallTo(() => repository.Update(A<Asset>.Ignored)).MustHaveHappened(Repeated.Exactly.Once);
            A.CallTo(() => repository.Create(A<Asset>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => repository.Delete(A<Asset>.Ignored)).MustNotHaveHappened();
        }
        [TestMethod]
        public void Failed_merged_update_rejects()
        {
            Asset bfim = A.Dummy<Asset>(),
                current = A.Dummy<Asset>(),
                afim = A.Dummy<Asset>();


            var index = Index(_ => _.Id, current);
            var repository = Repository(_ => _.Id, current);
            var resolution = Resolution<Asset>(MergeActionResult.Unresolved);
            var provider = Provider(resolution);
            var change = new ChangeItem<Asset>(ChangeAction.Update, bfim, afim);


            var resolver = new MergeConcurrentEditsConflictResolver(provider);
            var result = resolver.Resolve(repository, change, index);

            Assert.IsFalse(result);
            A.CallTo(() => repository.Update(A<Asset>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => repository.Create(A<Asset>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => repository.Delete(A<Asset>.Ignored)).MustNotHaveHappened();
        }

        [TestMethod]
        public void Merged_delete_resolves()
        {
            Asset bfim = A.Dummy<Asset>(), 
                current = A.Dummy<Asset>(),
                afim = A.Dummy<Asset>();


            var index = Index(_ => _.Id, current);
            var repository = Repository(_ => _.Id, current);
            var resolution = Resolution(MergeActionResult.Delete, current);
            var provider = Provider(resolution);
            var change = new ChangeItem<Asset>(ChangeAction.Delete, bfim, afim);


            var resolver = new MergeConcurrentEditsConflictResolver(provider);
            var result = resolver.Resolve(repository, change, index);

            Assert.IsTrue(result);
            A.CallTo(() => provider.Merge(MergeKind.Auto, bfim, current, afim)).MustHaveHappened(Repeated.Exactly.Once);
            A.CallTo(() => repository.Delete(current)).MustHaveHappened();
            A.CallTo(() => repository.Delete(A<Asset>.Ignored)).MustHaveHappened(Repeated.Exactly.Once);
            A.CallTo(() => repository.Create(A<Asset>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => repository.Update(A<Asset>.Ignored)).MustNotHaveHappened();
        }

        [TestMethod]
        public void Failed_merged_delete_rejects()
        {
            Asset bfim = A.Dummy<Asset>(),
                current = A.Dummy<Asset>(),
                afim = A.Dummy<Asset>();


            var index = Index(_ => _.Id, current);
            var repository = Repository(_ => _.Id, current);
            var resolution = Resolution(MergeActionResult.Unresolved, current);
            var provider = Provider(resolution);
            var change = new ChangeItem<Asset>(ChangeAction.Delete, bfim, afim);

            var resolver = new MergeConcurrentEditsConflictResolver(provider);
            var result = resolver.Resolve(repository, change, index);

            Assert.IsFalse(result);
            A.CallTo(() => provider.Merge(MergeKind.Auto, bfim, current, afim)).MustHaveHappened(Repeated.Exactly.Once);
            A.CallTo(() => repository.Update(A<Asset>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => repository.Create(A<Asset>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => repository.Delete(A<Asset>.Ignored)).MustNotHaveHappened();
        }

        [TestMethod]
        public void Create_never_merges_always_resolves()
        {
            Asset bfim = null,
                   current = null,
                   afim = A.Dummy<Asset>();


            var index = A.Fake < IDictionary<int, Asset>>();
            var repository = Repository<Asset, int>(_ => _.Id);
            var resolution = Resolution(MergeActionResult.Create, current);
            var provider = Provider(resolution);
            var change = new ChangeItem<Asset>(ChangeAction.Create, bfim, afim);


            var resolver = new MergeConcurrentEditsConflictResolver(provider);
            var result = resolver.Resolve(repository, change, index);

            Assert.IsTrue(result);
            A.CallTo(() => provider.Merge(MergeKind.Auto, bfim, current, afim)).MustNotHaveHappened();
            A.CallTo(() => repository.Create(afim)).MustHaveHappened();
            A.CallTo(() => repository.Create(A<Asset>.Ignored)).MustHaveHappened(Repeated.Exactly.Once);
            A.CallTo(() => repository.Delete(A<Asset>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => repository.Update(A<Asset>.Ignored)).MustNotHaveHappened();
        }

        [TestMethod]
        public void Invalid_action_never_merges_always_rejects()
        {
            Asset bfim = A.Dummy<Asset>(),
                current = A.Dummy<Asset>(),
                afim = A.Dummy<Asset>();


            var index = Index(_ => _.Id, current);
            var repository = Repository(_ => _.Id, current);
            var resolution = Resolution(MergeActionResult.Update, afim);
            var provider = Provider(resolution);
            var change = new ChangeItem<Asset>(ChangeAction.Initialize, bfim, afim);

            var resolver = new MergeConcurrentEditsConflictResolver(provider);
            var result = resolver.Resolve(repository, change, index);

            Assert.IsFalse(result);
            A.CallTo(() => provider.Merge(MergeKind.Auto, bfim, current, afim)).MustNotHaveHappened();
            A.CallTo(() => repository.Create(A<Asset>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => repository.Delete(A<Asset>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => repository.Update(A<Asset>.Ignored)).MustNotHaveHappened();

            resolver = new MergeConcurrentEditsConflictResolver(provider);
            result = resolver.Resolve(repository, change, index);

            Assert.IsFalse(result);
            A.CallTo(() => provider.Merge(MergeKind.Auto, bfim, current, afim)).MustNotHaveHappened();
            A.CallTo(() => repository.Create(A<Asset>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => repository.Delete(A<Asset>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => repository.Update(A<Asset>.Ignored)).MustNotHaveHappened();
        }

        [TestMethod]
        public void Dirty_update_on_delted_item_merged_to_create_resolves_with_create()
        {
            Asset bfim = A.Dummy<Asset>(),
                current = A.Dummy<Asset>(),
                afim = A.Dummy<Asset>();


            var index = Index(_ => _.Id, current);
            var repository = Repository(_ => _.Id, current);
            var resolution = Resolution(MergeActionResult.Create, afim);
            var provider = Provider(resolution);
            var change = new ChangeItem<Asset>(ChangeAction.Update, bfim, afim);


            var resolver = new MergeConcurrentEditsConflictResolver(provider);
            var result = resolver.Resolve(repository, change, index);

            Assert.IsTrue(result);
            A.CallTo(() => repository.Create(afim)).MustHaveHappened();
            A.CallTo(() => repository.Create(A<Asset>.Ignored)).MustHaveHappened(Repeated.Exactly.Once);

            A.CallTo(() => repository.Update(A<Asset>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => repository.Delete(A<Asset>.Ignored)).MustNotHaveHappened();
        }

        
        [TestMethod]
        public void Delete_on_hidden_delete_merged_to_noop_resolves_with_noop()
        {
            Asset bfim = A.Dummy<Asset>(),
                current = A.Dummy<Asset>(),
                afim = A.Dummy<Asset>();


            var index = Index(_ => _.Id, current);
            var repository = Repository(_ => _.Id, current);
            var resolution = Resolution<Asset>(MergeActionResult.Resolved);
            var provider = Provider(resolution);
            var change = new ChangeItem<Asset>(ChangeAction.Delete, bfim, afim);


            

            var resolver = new MergeConcurrentEditsConflictResolver(provider);
            var result = resolver.Resolve(repository, change, index);

            Assert.IsTrue(result);
            A.CallTo(() => repository.Create(A<Asset>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => repository.Update(A<Asset>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => repository.Delete(A<Asset>.Ignored)).MustNotHaveHappened();
        }
    }
}
