using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rel.Data;
using Rel.Data.Bulk;
using Rel.Data.Diagnostics;
using Rel.Data.Models;
using Rel.Merge;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Transactions;

namespace ProofingTests
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class UnitTest1
    {
        private const double
            s_chanceCreate = 0,
            s_chanceDelete = 0,
            s_conflictGuarantee = 0,
            s_chanceLock = 0;

        private const int
            s_initialPoolSize = 10,
            s_minCsSize = 10,
            s_maxCsSize = 11;

        private static readonly PerformanceCounter[]
            s_assetCount,
            s_assetCountBase;
        private static readonly PerformanceCounter
            s_pcAttempt,
            s_pcAttemptSuccess,
            s_pcPost,
            s_pcPostSuccess;

        [ThreadStatic]
        private static Random s_rng;
        
        static UnitTest1()
        {
            if (!ChangeSetPerformanceCounters.AssertPerformanceCounterCategory("Change Set Test", "", PerformanceCounterCategoryType.MultiInstance, new CounterCreationData[]{
                new CounterCreationData("Asset Count","none", PerformanceCounterType.AverageCount64),
                new CounterCreationData("Asset Count Base","none", PerformanceCounterType.AverageBase),
                new CounterCreationData("PCAttempt","none", PerformanceCounterType.NumberOfItems32),
                new CounterCreationData("PCSuccess","none", PerformanceCounterType.NumberOfItems32),
                new CounterCreationData("PostPCAttempt","none", PerformanceCounterType.NumberOfItems32),
                new CounterCreationData("PostPCSuccess","none", PerformanceCounterType.NumberOfItems32)
            }))
                throw new InvalidOperationException();
            s_assetCount = new[]
            {
                new PerformanceCounter("Change Set Test", "Asset Count", "Job 1",false),
                new PerformanceCounter("Change Set Test", "Asset Count", "Job 2",false)
            };
            s_assetCountBase = new[]
            {
                new PerformanceCounter("Change Set Test", "Asset Count Base", "Job 1",false),
                new PerformanceCounter("Change Set Test", "Asset Count Base", "Job 2",false)
            };
            s_pcAttempt                  = new PerformanceCounter("Change Set Test", "PCAttempt", "Job 1", false);
            s_pcAttemptSuccess           = new PerformanceCounter("Change Set Test", "PCSuccess", "Job 1", false);
            s_pcPost                     = new PerformanceCounter("Change Set Test", "PostPCAttempt", "Job 1", false);
            s_pcPostSuccess              = new PerformanceCounter("Change Set Test", "PostPCSuccess", "Job 1", false);
            s_assetCount[0].RawValue     = 0;
            s_assetCount[1].RawValue     = 0;
            s_assetCountBase[0].RawValue = 0;
            s_assetCountBase[1].RawValue = 0;

            s_pcAttempt.RawValue = 0;
            s_pcAttemptSuccess.RawValue = 0;
            s_pcPost.RawValue = 0;
            s_pcPostSuccess.RawValue = 0;

            PreseedDB();
        }

        [TestMethod]
        public void Concurrent()
        {
            Worker(1, s_chanceLock, new MergeConcurrentEditsConflictResolver(new MergeOperation()));
        }

        private static long s_flag = 0;
        [TestMethod]
        public void PCLock()
        {
            Exception ex = null;
            int jobId = 1;
            long flag = Interlocked.CompareExchange(ref s_flag, 1, 0);
            var un = "fixed_unique_name";
            if (flag == 1)
            {
                Concurrent();
                return;
            }

            try
            {

                using (var uc = new FakeUserContext(un))
                {
                    bool failed = false;
                    var resolver = new RejectConcurrentEditsConflictResolver();
                    try
                    {
                        s_pcAttempt.Increment();
                        Exec(jobId, 1, 1, resolver); // test can get the lock
                        s_pcAttemptSuccess.Increment();
                    }
                    catch (ConcurrencyException)
                    {

                    }
                    catch
                    {
                        failed = true;
                        return;
                    }

                    if (!failed)
                    {
                        try
                        {
                            s_pcPost.Increment();
                            Exec(jobId, 1, 0, resolver); // test can update after acquiring
                            s_pcPostSuccess.Increment();
                        }
                        catch (Exception e)
                        {
                            ex = e;
                        }
                    }

                }
            }
            finally
            {
                try
                {
                    EnsureJobIsNotLocked(1, ex, un);
                }
                finally
                {
                    Interlocked.Exchange(ref s_flag, 0);
                }
            }
        }

        private void Worker(int jobId, double lockChance, IConflictResolver resolver)
        {
            using (var uc = new FakeUserContext())
            {
                Exception ex = null;
                try
                {
                    Exec(jobId, lockChance, s_conflictGuarantee, resolver);
                }
                catch (Exception e)
                {
                    ex = e;
                }

                EnsureJobIsNotLocked(jobId, ex, uc.Identity.Name);
            }

        }

        [TestInitialize]
        public void Initialize()
        {
            if (s_rng == null)
                s_rng = new Random((int)DateTime.UtcNow.Ticks);
        }

        [TestMethod]
        public void NonConcurrent()
        {
            Worker(2, 0, new RejectConcurrentEditsConflictResolver());
        }

        private static Asset Dirty(Asset asset)
        {
            asset.MaximumAndMinimumDecay = GetNewValuePercent(asset.MaximumAndMinimumDecay, 29);
            asset.MaxMinDecayWithStepAndTol = GetNewValuePercent(asset.MaxMinDecayWithStepAndTol, 29);
            asset.MinimumDecay = GetNewValuePercent(asset.MinimumDecay, 19);
            asset.MonotonicTolerance = GetNewValuePercent(asset.MonotonicTolerance, 9);
            asset.PercentTolerance = GetNewValuePercent(asset.PercentTolerance, 9);
            asset.StaticTolerance = GetNewValuePercent(asset.StaticTolerance, 19);

            return asset;
        }

        private static double GetNewValuePercent(double? start, int range)
        {
            if (!start.HasValue)
                return s_rng.Next(100);
            if (start == 0)
                return 0.1;
            return ((s_rng.NextDouble() * 100) % range) /
                (s_rng.Next(2) == 0 ? -100 : 100);
        }

        private static void PreseedDB()
        {
            using (var ctx = new Rel.Data.Ef6.TpContext())
            {
                var cmd = @"
DECLARE @cnt int = 0;
UPDATE Job SET LockedBy=null, LockedOn=null WHERE LockedBy IS NOT NULL AND id=@p0

DELETE FROM Asset WHERE JobId=@p0;
WHILE @cnt < @p1
BEGIN

INSERT INTO Asset (JobId, Name, ServiceArea,PercentTolerance,StaticTolerance,MonotonicTolerance,MaximumAndMinimumDecay,MaxMinDecayWithStepAndTol,MinimumDecay)
SELECT @p0, CAST(NEWID() as char(36)), CAST(NEWID() as char(36)), rand(),rand(),rand(),rand(),rand(),rand()

SET @cnt = @cnt+ 1;
END
";

                ctx.Database.ExecuteSqlCommand(cmd, 1, s_initialPoolSize);
                ctx.Database.ExecuteSqlCommand(cmd, 2, s_initialPoolSize);
            }
        }

        private ChangeSet BuildChangeSet(int jobId)
        {
            var dc = new Rel.Data.Ef6.TpContext();
            IList<Asset> assets;
            int count;
            using (var scope = new TransactionScope())
            {
                int take = s_rng.Next(s_minCsSize, s_maxCsSize);
                assets = dc.Database.SqlQuery<Asset>(
@"SELECT
Id
,JobId
,Name
,RowVersion
,ServiceArea
,PercentTolerance
,StaticTolerance
,MonotonicTolerance
,MaximumAndMinimumDecay
,MaxMinDecayWithStepAndTol
,MinimumDecay
FROM ( SELECT * FROM dbo.Asset WITH(NOLOCK) WHERE JobId=@p0 ) a
ORDER BY PercentTolerance ASC OFFSET 0 ROWS
FETCH NEXT @p1 ROWS ONLY", jobId, take).ToList();

                count = dc.Database.SqlQuery<int>(@"SELECT COUNT(Id) FROM dbo.Asset WITH(NOLOCK) WHERE JobId=@p0", jobId).Single();
                scope.Complete();
            }

            var cs = new ChangeSet()
            {
                Assets = assets
                    .Select(ModifyAsset)
                    .Union(NewAssets(jobId, s_rng.Next(1, (int)Math.Ceiling(assets.Count * 0.3)))).ToList()
            };

            s_assetCount[jobId - 1].IncrementBy(count);
            s_assetCountBase[jobId - 1].Increment();
            return cs;
        }

        private void EnsureJobIsNotLocked(int jobId, Exception ex, string username)
        {
            var dc = new Rel.Data.Ef6.TpContext();
            while (true)
            {
                try
                {
                    using (var scope = new System.Transactions.TransactionScope(TransactionScopeOption.RequiresNew))
                    {
                        using (new Timed(false, "EnsuringNoLock job {0}-{1}", jobId, username))
                        {
                            var updated = dc.Database.ExecuteSqlCommand(
                                "SET DEADLOCK_PRIORITY 10;UPDATE dbo.Job SET LockedBy=NULL,LockedOn=NULL WHERE Id=@p0 AND LockedBy=@p1",
                                jobId, username);
                        }
                        scope.Complete();
                        break;
                    }
                }
                catch (Exception)
                {
                    continue;
                }
            }

            if (ex != null)
            {
                var e = ex;
                while (e != null)
                {
                    if (e is Rel.Data.ConcurrencyException)
                        return;
                    if (e is PessimisticConcurrencyException)
                    {
                        System.Threading.Thread.Sleep(100);
                        return;
                    }
                    if (e is ValidationException)
                        return;
                    if (ex.InnerException != null)
                        ex = ex.InnerException;
                }

                throw ex;
            }
        }

        private void Exec(int jobId, double pcPercent, double conflictGuarantee, IConflictResolver resolver)
        {
            bool claimIt = false;
            if (s_rng.NextDouble() < pcPercent)
                claimIt = true;
            var cs = BuildChangeSet(jobId);
            var dc = new Rel.Data.Ef6.TpContext();
            var res = resolver;
            var csp = new ChangeSetProcessor(dc, res);
            ChangeSet result;
            if (s_rng.NextDouble() < conflictGuarantee)
            {
                Touch(jobId, cs);
            }
            using (new Timed(false, "ChangeSet {0}", res.GetType().Name))
                result = csp.Process(jobId, claimIt, cs);
        }

        private ChangeItem<Asset> ModifyAsset(Asset asset)
        {
            var choice = s_rng.NextDouble();
            ChangeItem<Asset> change;
            if (s_rng.NextDouble() < s_chanceDelete)
            {
                change = new ChangeItem<Asset>(ChangeAction.Delete, asset, null);
            }
            else
            {
                change = new ChangeItem<Asset>(ChangeAction.Update, asset, Dirty(asset));
            }
            return change;
        }

        private IEnumerable<ChangeItem<Asset>> NewAssets(int jobId, int count)
        {
            if (s_rng.NextDouble() < s_chanceCreate)
                return Rel.Data.Ef6.Migrations.AssetGenerator.CreateAssets(jobId, count)
                    .Select(_ => new ChangeItem<Asset>(ChangeAction.Create, null, _));

            return Enumerable.Empty<ChangeItem<Asset>>();
        }

        private void Touch(int jobid, ChangeSet cs)
        {
            var im = cs.Assets.First(_ => _.BFIM != null).BFIM;
            s_rng.NextBytes(im.RowVersion);
        }

        private class FakeIdent : System.Security.Principal.IIdentity
        {
            private bool _isAuthenticated;
            private string _name;

            public FakeIdent(string name, bool isAuthenticated)
            {
                _name = name;
                _isAuthenticated = isAuthenticated;
            }

            public FakeIdent(string name)
                : this(name, true)
            {
            }

            public string AuthenticationType
            {
                get { return "bogus"; }
            }

            public bool IsAuthenticated
            {
                get { return _isAuthenticated; }
            }

            public string Name
            {
                get { return _name; }
            }
        }

        private class FakeUser : System.Security.Principal.IPrincipal
        {
            private System.Security.Principal.IIdentity _ident;

            public FakeUser(System.Security.Principal.IIdentity ident)
            {
                _ident = ident;
            }

            System.Security.Principal.IIdentity System.Security.Principal.IPrincipal.Identity
            {
                get { return _ident; }
            }

            bool System.Security.Principal.IPrincipal.IsInRole(string role)
            {
                return false;
            }
        }

        private class FakeUserContext : IDisposable
        {
            private System.Security.Principal.IPrincipal _original;
            private System.Security.Principal.IPrincipal _principal;

            public FakeUserContext()
                : this(s_rng.Next().ToString())
            {

            }

            public FakeUserContext(string p)
            {
                if (string.IsNullOrEmpty(p))
                    throw new ArgumentException();

                _original = System.Threading.Thread.CurrentPrincipal;
                System.Threading.Thread.CurrentPrincipal = _principal = new FakeUser(new FakeIdent(p, true));
            }

            public IIdentity Identity { get { return _principal.Identity; } }

            void IDisposable.Dispose()
            {
                System.Threading.Thread.CurrentPrincipal = _original;
            }
        }
    }
}

/*
        //[DecaySpanMergeable("0.00:00:00.3", "0.00:00:00.3")]
        [StepMergeable(true, 0.3)]
        public double? MaximumAndMinimumDecay { get; set; }

        //[DecaySpanMergeable("0.00:00:00.3", "0.00:00:00.3")]
        [StepMergeable(true, 0.2)]
        public double? MaxMinDecayWithStepAndTol { get; set; }

        //[DecaySpanMergeable("0.00:10:00", "0.00:00:00.1")]
        [StepMergeable(true, 0.2)]
        public double MinimumDecay { get; set; }

        [StepMergeable(true, 0, 0.5)]
        public double? MonotonicTolerance { get; set; }

        [LastWriteWins]
        public string Name { get; set; }

        [LastWriteWins(false)]
        public double? PercentTolerance { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; }

        public string ServiceArea { get; set; }

        [StepMergeable(-10, 30)]
        public double? StaticTolerance { get; set; }
    }

 */