using System.Configuration;

namespace Rel.Data.Configuration
{
    /// <summary>
    /// Configuration container for Job check in.
    /// </summary>
    public class ChangeSetProcessingConfigurationElement : ConfigurationElement
    {
        /// <summary>
        ///   Gets the maximum change set size to witness before
        ///   requiring a lock. Set to 0 to disable.
        /// </summary>
        /// <value>The lock threshold.</value>
        [IntegerValidator(MinValue = 0, MaxValue = short.MaxValue, ExcludeRange = false)]
        [ConfigurationProperty("maxOpenSize", IsRequired = false, DefaultValue = 100)]
        public int LockThreshold
        {
            get { return (int)this["maxOpenSize"]; }
        }
    }
}