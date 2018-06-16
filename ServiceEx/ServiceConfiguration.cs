using System;
using System.Reflection;
using Woof.AssemblyEx;

namespace Woof.ServiceEx {

    /// <summary>
    /// Service configuration container.
    /// </summary>
    public class ServiceConfiguration {

        #region Configuration properties

        public string Company { get; set; }

        public string ServiceName { get; set; }

        public string Version { get; set; }

        public string ServiceUser { get; set; }

        public string ServicePassword { get; set; }

        public string DisplayName { get; set; }

        public string Description { get; set; }

        public Action StartAction { get; set; }

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