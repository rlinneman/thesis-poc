using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace Rel.Data.Diagnostics
{
    /// <summary>
    ///   Simple disposable for remedial tracing with HPET profiler timing.
    /// </summary>
    public class Timed : IDisposable
    {
        private const string s_categoryName = "Timings";
        private static readonly ConcurrentDictionary<string, TimingCounters> s_counters;
        private static readonly bool s_enabled;
        private static long s_timed = 0;

        private long _inst;
        private string _scope;
        private long _ticks;
        private bool _usePerfCounter;

        static Timed()
        {
            s_enabled = TimingCounters.Initialize();
            s_counters = new ConcurrentDictionary<string, TimingCounters>(
                StringComparer.InvariantCultureIgnoreCase);
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="Timed"/> class.
        /// </summary>
        /// <param name="usePerfCounter">
        ///   if set to <c>true</c> [use perf counter].
        /// </param>
        /// <param name="format">The format.</param>
        /// <param name="args">The arguments.</param>
        public Timed(bool usePerfCounter, string format, params object[] args)
            : this(usePerfCounter, string.Format(format, args))
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="Timed"/> class.
        /// </summary>
        /// <param name="usePerfCounter">
        ///   if set to <c>true</c> [use perf counter].
        /// </param>
        /// <param name="format">The format.</param>
        /// <param name="arg">The argument.</param>
        public Timed(bool usePerfCounter, string format, object arg)
            : this(usePerfCounter, string.Format(format, arg))
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="Timed"/> class.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="arg">The argument.</param>
        public Timed(string format, object arg)
            : this(true, string.Format(format, arg))
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="Timed"/> class.
        /// </summary>
        /// <param name="scope">The scope.</param>
        public Timed(string scope)
            : this(true, scope)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="Timed"/> class.
        /// </summary>
        /// <param name="usePerCountr">
        ///   if set to <c>true</c> [use per countr].
        /// </param>
        /// <param name="scope">The scope.</param>
        public Timed(bool usePerCountr, string scope)
        {
            _inst = Interlocked.Increment(ref s_timed);
            _scope = scope;
            _usePerfCounter = usePerCountr;

            Debug.Print("[{0}] {1} - Timing...", _inst, scope);
            _ticks = Stopwatch.GetTimestamp();
        }

        /// <summary>
        ///   Performs application-defined tasks associated with
        ///   freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            var doneAt = Stopwatch.GetTimestamp();
            var dur = doneAt - _ticks;

            if (s_enabled && _usePerfCounter)
            {
                var counters = s_counters.GetOrAdd(_scope, TimingCounters.Create);

                counters.AvgRuntime.IncrementBy(dur);
                counters.AvgRuntimeBase.Increment();
            }

            var ts = TimeSpan.FromSeconds(dur / (double)Stopwatch.Frequency);
            Trace.TraceInformation("[{0}] {1} - {2}", _inst, _scope, ts.ToString());
        }

        /// <summary>
        ///   Provides easy access to and instance-able performance
        ///   counter for recording times.
        /// </summary>
        private class TimingCounters
        {
            internal readonly PerformanceCounter
                AvgRuntime,
                AvgRuntimeBase;

            private readonly string _name;

            private TimingCounters(string name)
            {
                _name = name;
                AvgRuntime = new PerformanceCounter(s_categoryName, "Avg. Runtime", name, false);
                AvgRuntimeBase = new PerformanceCounter(s_categoryName, "Avg. Runtime Base", name, false);

                AvgRuntime.RawValue = 0;
                AvgRuntimeBase.RawValue = 0;
            }

            internal static TimingCounters Create(string name)
            {
                return new TimingCounters(name);
            }

            internal static bool Initialize()
            {
                return ChangeSetPerformanceCounters.AssertPerformanceCounterCategory(
                       s_categoryName,
                       "Timing counters for Thesis Portal.",
                       PerformanceCounterCategoryType.MultiInstance,
                       new CounterCreationData("Avg. Runtime", "The average runtime.", PerformanceCounterType.AverageTimer32),
                       new CounterCreationData("Avg. Runtime Base", "The average runtime base.", PerformanceCounterType.AverageBase)
                       );
            }
        }
    }
}