using System.ComponentModel;
using System.ServiceProcess;

namespace Woof.ServiceEx {

    /// <summary>
    /// Extend this class in service assembly
    /// </summary>
    [RunInstaller(true)]
    public class ServiceProcessInstallerEx : ServiceProcessInstaller {

        /// <summary>
        /// Creates configured <see cref="ServiceProcessInstaller"/> instance.
        /// </summary>
        public ServiceProcessInstallerEx() {
            var cfg = ServiceState.Configuration;
            var user = cfg.ServiceUser;
            var passwd = cfg.ServicePassword;
            if (passwd == "") passwd = null;
            switch (user) {
                case "LocalService":
                    Account = ServiceAccount.LocalService;
                    break;
                case "LocalSystem":
                    Account = ServiceAccount.LocalSystem;
                    break;
                case "NetworkService":
                case "":
                    Account = ServiceAccount.NetworkService;
                    break;
                default:
                    Account = ServiceAccount.User;
                    Username = user;
                    Password = passwd;
                    break;
            }
        }
    }

}