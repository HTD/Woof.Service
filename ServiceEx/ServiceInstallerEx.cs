using System.Collections;
using System.ComponentModel;
using System.ServiceProcess;

namespace Woof.ServiceEx {

    /// <summary>
    /// Extend this class in service assembly.
    /// </summary>
    [RunInstaller(true)]
    public class ServiceInstallerEx : ServiceInstaller {

        /// <summary>
        /// Creates configured <see cref="ServiceInstaller"/> instance.
        /// </summary>
        public ServiceInstallerEx() {
            var cfg = ServiceState.Configuration;
            ServiceName = cfg.ServiceName;
            DisplayName = cfg.DisplayName;
            Description = cfg.Description;
            DelayedAutoStart = false;
            StartType = ServiceStartMode.Automatic;
        }

        /// <summary>
        /// Starts the service immediately after installed.
        /// </summary>
        /// <param name="savedState">Irrelevant here.</param>
        protected override void OnAfterInstall(IDictionary savedState) {
            base.OnAfterInstall(savedState);
            using (var controller = new ServiceController(ServiceName)) controller.Start();
        }

    }

}