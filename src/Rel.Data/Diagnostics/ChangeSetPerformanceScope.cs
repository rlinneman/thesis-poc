using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Rel.Data.Diagnostics
{
    /// <summary>
    ///   Exposes performance counters for Merge wrapped in
    ///   IDisposable units to ease of use.
    /// </summary>
    internal class ChangeSetPerformanceScope : IDisposable
    {
        private const string s_categoryName = "Change Set";
        private static readonly ConcurrentDictionary<string, CounterCollection> s_counters;
        private static readonly bool s_enabled;

        private readonly CounterCollection _counters;
        private readonly long _startTicks, _size;
        private bool _isComplete;
        private int _rejectionCount;

        /// <summary>
        ///   Initializes the <see cref="ChangeSetPerformanceScope"/>
        ///   class, setting up the performance counter category and
        ///   preparing the common total counters.
        /// </summary>
        static ChangeSetPerformanceScope()
        {
            s_enabled = CounterCollection.Initialize();

            if (!s_enabled)
                return;

            s_counters = new ConcurrentDictionary<string, CounterCollection>(StringComparer.InvariantCultureIgnoreCase);
        }

        /// <summary>
        ///   Initializes a new instance of the
        ///   <see cref="ChangeSetPerformanceScope"/> class.
        /// </summary>
        /// <param name="instanceName">The instance name.</param>
        /// <param name="changeSet">The change set.</param>
        public ChangeSetPerformanceScope(string instanceName, Bulk.ChangeSet changeSet)
        {
            if (!s_enabled)
                return;

            _counters = s_counters.GetOrAdd(instanceName, CounterCollection.Create);
            _size = changeSet.TotalItemsCount;

            _startTicks = Stopwatch.GetTimestamp();
        }

        /// <summary>
        ///   Notifies this context that all work completed
        ///   successfully so that when disposed, the proper counters
        ///   are updated.
        /// </summary>
        public void Complete()
        {
            _isComplete = true;
        }

        /// <summary>
        ///   Performs application-defined tasks associated with
        ///   freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (!s_enabled)
                return;

            var endTicks = Stopwatch.GetTimestamp();
            var hpetTicks = endTicks - _startTicks;

            // These counter increments belong in the ctor but are
            // placed here so that the load test environment collects
            // all relevant values
            _counters.Total.Increment();
            _counters.TotalItems.IncrementBy(_size);
            _counters.AvgCount.IncrementBy(_size);
            _counters.AvgCountBase.Increment();

            _counters.ProcessTime.IncrementBy(hpetTicks);

            if (_isComplete)
            {
                _counters.Accepted.Increment();
                _counters.AcceptedItems.IncrementBy(_size);

                if (_rejectionCount > 0)
                {
                    _counters.Redressed.Increment();
                    _counters.RedressedItems.IncrementBy(_size);
                }
            }
            else if (_rejectionCount > 0)
            {
                _counters.Returned.Increment();
                _counters.ReturnedItems.IncrementBy(_size);
            }
            else
            {
                _counters.Aborted.Increment();
                _counters.AbortedItems.IncrementBy(_size);
            }
        }

        /// <summary>
        ///   Times the cache building subprocess.
        /// </summary>
        /// <returns>A disposable to notify when completed.</returns>
        internal IDisposable TimeCacheBuilding()
        {
            return new CallbackDisposable(IncrementCacheBuilding);
        }

        /// <summary>
        ///   Times the redress process
        /// </summary>
        /// <returns>A disposable to notify when completed.</returns>
        internal IDisposable TimeRedress()
        {
            _rejectionCount += 1;
            return new CallbackDisposable(IncrementRedressTime);
        }

        /// <summary>
        ///   Times the replay process.
        /// </summary>
        /// <returns>A disposable to notify when completed.</returns>
        internal IDisposable TimeReplay()
        {
            return new CallbackDisposable(IncrementReplayTime);
        }

        /// <summary>
        ///   Times the conflict resolution process.
        /// </summary>
        /// <returns></returns>
        internal IDisposable TimeResolve()
        {
            return new CallbackDisposable(IncrementResolveTime);
        }

        /// <summary>
        ///   Times the save process, for use in first and second pass timing.
        /// </summary>
        /// <returns>A disposable to notify when completed.</returns>
        internal IDisposable TimeSave()
        {
            return new CallbackDisposable(IncrementSaveTime);
        }

        private void IncrementCacheBuilding(long hpetTicks)
        {
            _counters.CacheBuildTime.IncrementBy(hpetTicks);
        }

        private void IncrementRedressTime(long hpetTicks)
        {
            _counters.RedressTime.IncrementBy(hpetTicks);
        }

        private void IncrementReplayTime(long hpetTicks)
        {
            _counters.ReplayTime.IncrementBy(hpetTicks);
        }

        private void IncrementResolveTime(long hpetTicks)
        {
            _counters.ResolveTime.IncrementBy(hpetTicks);
        }

        private void IncrementSaveTime(long hpetTicks)
        {
            if (_rejectionCount == 0)
                _counters.SaveTime.IncrementBy(hpetTicks);
            else
                _counters.SaveTime2.IncrementBy(hpetTicks);
        }

        /// <summary>
        ///   Remedial disposable to pack a callback for performance timings.
        /// </summary>
        internal class CallbackDisposable : IDisposable
        {
            private readonly Action<long> _action;
            private readonly long _ticks;

            public CallbackDisposable(Action<long> action)
            {
                _action = action;
                _ticks = Stopwatch.GetTimestamp();
            }

            public void Dispose()
            {
                var end = Stopwatch.GetTimestamp();
                _action(end - _ticks);
            }
        }

        /// <summary>
        ///   A wrapper class to house instances of performance
        ///   counters by instance name.
        /// </summary>
        private class CounterCollection
        {
            internal readonly PerformanceCounter
                Total,
                Accepted,
                Redressed,
                Returned,
                Aborted,

                TotalItems,
                AcceptedItems,
                RedressedItems,
                ReturnedItems,
                AbortedItems,

                AvgCount,
                AvgCountBase,

                ProcessTime,

                ReplayTime,
                RedressTime,
                ResolveTime,
                SaveTime,
                SaveTime2,
                CacheBuildTime;

            private string _instanceName;

            /// <summary>
            ///   Initializes a new instance of the
            ///   <see cref="CounterCollection"/> class.
            /// </summary>
            /// <param name="instanceName">Name of the instance.</param>
            private CounterCollection(string instanceName)
            {
                _instanceName = instanceName;

                Total = new PerformanceCounter(s_categoryName, "_Total", instanceName, false);
                Accepted = new PerformanceCounter(s_categoryName, "Accepted", instanceName, false);
                Redressed = new PerformanceCounter(s_categoryName, "Redressed", instanceName, false);
                Returned = new PerformanceCounter(s_categoryName, "Returned", instanceName, false);
                Aborted = new PerformanceCounter(s_categoryName, "Aborted", instanceName, false);
                TotalItems = new PerformanceCounter(s_categoryName, "_Total #", instanceName, false);
                AcceptedItems = new PerformanceCounter(s_categoryName, "Accepted #", instanceName, false);
                RedressedItems = new PerformanceCounter(s_categoryName, "Redressed #", instanceName, false);
                ReturnedItems = new PerformanceCounter(s_categoryName, "Returned #", instanceName, false);
                AbortedItems = new PerformanceCounter(s_categoryName, "Aborted #", instanceName, false);
                AvgCount = new PerformanceCounter(s_categoryName, "Avg Count", instanceName, false);
                AvgCountBase = new PerformanceCounter(s_categoryName, "Base Avg Count", instanceName, false);
                ProcessTime = new PerformanceCounter(s_categoryName, "Process Time", instanceName, false);
                ReplayTime = new PerformanceCounter(s_categoryName, "Replay Time", instanceName, false);
                RedressTime = new PerformanceCounter(s_categoryName, "Redress Time", instanceName, false);
                ResolveTime = new PerformanceCounter(s_categoryName, "Resolve Time", instanceName, false);
                SaveTime = new PerformanceCounter(s_categoryName, "Save Time", instanceName, false);
                SaveTime2 = new PerformanceCounter(s_categoryName, "Save Time 2", instanceName, false);
                CacheBuildTime = new PerformanceCounter(s_categoryName, "Cache Build Time", instanceName, false);

                Total.RawValue = 0;
                Accepted.RawValue = 0;
                Redressed.RawValue = 0;
                Returned.RawValue = 0;
                Aborted.RawValue = 0;
                TotalItems.RawValue = 0;
                AcceptedItems.RawValue = 0;
                RedressedItems.RawValue = 0;
                ReturnedItems.RawValue = 0;
                AbortedItems.RawValue = 0;
                AvgCount.RawValue = 0;
                AvgCountBase.RawValue = 0;
                ProcessTime.RawValue = 0;
                ReplayTime.RawValue = 0;
                RedressTime.RawValue = 0;
                ResolveTime.RawValue = 0;
                SaveTime.RawValue = 0;
                SaveTime2.RawValue = 0;
                CacheBuildTime.RawValue = 0;
            }

            /// <summary>
            ///   Creates the specified instance name.
            /// </summary>
            /// <param name="instanceName">Name of the instance.</param>
            /// <returns></returns>
            internal static CounterCollection Create(string instanceName)
            {
                return new CounterCollection(instanceName);
            }

            /// <summary>
            ///   Initializes the performance counter category.
            /// </summary>
            /// <returns></returns>
            internal static bool Initialize()
            {
                return ChangeSetPerformanceCounters.AssertPerformanceCounterCategory(
                    s_categoryName,
                    "Performance counters for Thesis Portal change set processing.",
                    PerformanceCounterCategoryType.MultiInstance,

                    new CounterCreationData("_Total", "The total number of change sets witnessed.", PerformanceCounterType.NumberOfItems32),
                    new CounterCreationData("Accepted", "The total number of change sets accepted.", PerformanceCounterType.NumberOfItems32),
                    new CounterCreationData("Redressed", "The total number of change sets redressed.", PerformanceCounterType.NumberOfItems32),
                    new CounterCreationData("Returned", "The total number of change sets returned.", PerformanceCounterType.NumberOfItems32),
                    new CounterCreationData("Aborted", "The total number of change sets aborted.", PerformanceCounterType.NumberOfItems32),
                    new CounterCreationData("_Total #", "The total number of items.", PerformanceCounterType.NumberOfItems32),
                    new CounterCreationData("Accepted #", "The total number of items accepted.", PerformanceCounterType.NumberOfItems32),
                    new CounterCreationData("Redressed #", "The total number of items redressed.", PerformanceCounterType.NumberOfItems32),
                    new CounterCreationData("Returned #", "The total number of items returned.", PerformanceCounterType.NumberOfItems32),
                    new CounterCreationData("Aborted #", "The total number of items aborted.", PerformanceCounterType.NumberOfItems32),
                    new CounterCreationData("Avg Count", "The average number of changes contained within a change set.", PerformanceCounterType.AverageCount64),
                    new CounterCreationData("Base Avg Count", "The base average number of changes contained within a change set.", PerformanceCounterType.AverageBase),

                    new CounterCreationData("Process Time", "The total time taken to process a change set.", PerformanceCounterType.NumberOfItems64),
                    new CounterCreationData("Replay Time", "The average time taken to replay a change set when first received.", PerformanceCounterType.NumberOfItems64),
                    new CounterCreationData("Save Time", "The total percent time taken to saving data to the db.", PerformanceCounterType.NumberOfItems64),
                    new CounterCreationData("Redress Time", "The total percent time taken to redress all change sets when first received.", PerformanceCounterType.NumberOfItems64),
                    new CounterCreationData("Resolve Time", "The total percent time taken by the conflict resolver.", PerformanceCounterType.NumberOfItems64),
                    new CounterCreationData("Cache Build Time", "The total percent time taken by the conflict resolver.", PerformanceCounterType.NumberOfItems64),
                    new CounterCreationData("Save Time 2", "The total percent time taken saving data to the db after redressing.", PerformanceCounterType.NumberOfItems64)
                    );
            }
        }
    }
}