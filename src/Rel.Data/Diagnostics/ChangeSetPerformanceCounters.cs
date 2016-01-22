using System;
using System.Diagnostics;

namespace Rel.Data.Diagnostics
{
    /// <summary>
    ///   Utility code for easing the setup of performance counters.
    /// </summary>
    internal class ChangeSetPerformanceCounters
    {
        /// <summary>
        ///   Ensures that the given performance counter category name
        ///   exists and that all counters specified are available
        ///   within the category.
        /// </summary>
        /// <param name="categoryName">Name of the category.</param>
        /// <param name="categoryDescription">The category description.</param>
        /// <param name="instancingMode">
        ///   The instancing mode expected from counters. Only
        ///   evaluated if the category gets created..
        /// </param>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">
        ///   categoryName or data
        /// </exception>
        /// <exception cref="System.ArgumentException">data</exception>
        internal static bool AssertPerformanceCounterCategory(
            string categoryName,
            string categoryDescription,
            PerformanceCounterCategoryType instancingMode,
            params CounterCreationData[] data)
        {
            if (categoryName == null)
                throw new ArgumentNullException("categoryName");
            if (data == null)
                throw new ArgumentNullException("data");
            if (data.Length == 0)
                throw new ArgumentException("data");

            try
            {
                if (PerformanceCounterCategory.Exists(categoryName))
                {
                    foreach (var def in data)
                    {
                        if (!PerformanceCounterCategory.CounterExists(def.CounterName, categoryName))
                        {
                            PerformanceCounterCategory.Delete(categoryName);
                            break;
                        }
                    }
                }

                if (!PerformanceCounterCategory.Exists(categoryName))
                {
                    PerformanceCounterCategory.Create(categoryName,
                        categoryDescription,
                        instancingMode,
                        new CounterCreationDataCollection(data));
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Failed to initialize performance counter category. {0}", ex.ToString());
                return false;
            }
        }
    }
}