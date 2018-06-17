using System;
using System.Reflection;
using Woof.AssemblyEx;

namespace Woof.ServiceEx {

    /// <summary>
    /// Service configuration container.
    /// </summary>
    public class ServiceConfiguration {

        #region Configuration properties

        /// <summary>
        /// Gets or sets company property of the service metadata.
        /// </summary>
        public string Company { get; set; }

        /// <summary>
        /// Gets or sets the service name (identifier) property of the service metadata.
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// Gets or sets the service version string.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the service user name.
        /// </summary>
        public string ServiceUser { get; set; }

        /// <summary>
        /// Gets or sets the service user password.
        /// </summary>
        public string ServicePassword { get; set; }

        /// <summary>
        /// Gets or sets the service display name.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the service description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the action executed on service start.
        /// </summary>
        public Action StartAction { get; set; }

        /// <summary>
        /// Gets or sets the action executed on service stop.
        /// </summary>
        public Action StopAction { get; set; }

        #endregion

        /// <summary>
        /// Gets default instance of the service configuration created from entry assembly.
        /// </summary>
        public static ServiceConfiguration Default => _Default ?? (_Default = new ServiceConfiguration(Assembly.GetEntryAssembly()));

        /// <summary>
        /// Creates default service configuration from assembly specified.
        /// </summary>
        private ServiceConfiguration(Assembly assembly) {
            var i = new AssemblyInfo(assembly);
            ServiceUser = "LocalSystem";
            ServiceName = i.Product;
            Version = i.Version.ToString();
            DisplayName = i.Title;
            Description = i.Description;
            Company = i.Company;
        }

        private static ServiceConfiguration _Default;

    }

}