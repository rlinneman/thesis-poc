using System;
using System.Configuration;

namespace Rel.Data.Configuration
{
    /// <summary>
    ///   Configuration container for the Thesis Portal application.
    /// </summary>
    public class DataConfigurationSection : ConfigurationSection
    {
        private static readonly Lazy<DataConfigurationSection> _defaultConfig = new Lazy<DataConfigurationSection>(InitDefaultConfig);

        /// <summary>
        ///   Gets the default.
        /// </summary>
        /// <value>The default configuration instance.</value>
        public static DataConfigurationSection Default { get { return _defaultConfig.Value; } }

        /// <summary>
        ///   Gets the configuration for the Job check in sub system.
        /// </summary>
        /// <value>The job check in.</value>
        [ConfigurationProperty("changeSets", IsRequired = false, IsKey = false)]
        public ChangeSetProcessingConfigurationElement ChangeSets
        {
            get { return (ChangeSetProcessingConfigurationElement)this["changeSets"]; }
        }

        /// <summary>
        ///   Initializes the default configuration.
        /// </summary>
        /// <returns>The default configuration instance.</returns>
        private static DataConfigurationSection InitDefaultConfig()
        {
            return (DataConfigurationSection)ConfigurationManager.GetSection("Rel.Data");
        }
    }
}