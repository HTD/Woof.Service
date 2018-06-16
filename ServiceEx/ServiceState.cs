namespace Woof.ServiceEx {

    /// <summary>
    /// Common static state container for all service classes in the service application.
    /// </summary>
    public static class ServiceState {

        /// <summary>
        /// Gets or sets a value indicating whether the service is in the test mode.
        /// </summary>
        public static bool IsInTestMode { get; set; }

        /// <summary>
        /// Gets or sets the service configuration.
        /// </summary>
        public static ServiceConfiguration Configuration { get; set; }

    }

}
