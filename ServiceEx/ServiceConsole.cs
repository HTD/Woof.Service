using System;
using System.Collections;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Windows.Forms;
using Woof.ConsoleEx;

namespace Woof.ServiceEx {

    /// <summary>
    /// Windows Service with built in command-line installer.
    /// </summary>
    public class ServiceConsole<T> where T : ServiceBaseEx, new() {

        /// <summary>
        /// Separator line matching console window width
        /// </summary>
        protected string Line => "".PadRight(Console.IsOutputRedirected ? 80 : Console.WindowWidth, '-');

        /// <summary>
        /// Service information string
        /// </summary>
        protected virtual string InfoString {
            get {
                var builder = new StringBuilder();
                builder.Append("{0}");
                builder.AppendLine("{1} v{2}");
                builder.Append("{0}");
                builder.AppendLine("Description:{3}");
                builder.AppendLine("Service log:{4}");
                builder.AppendLine("Service name:{5}");
                builder.AppendLine("Service user:{6}");
                builder.Append("{0}");
                var cfg = ServiceState.Configuration;
                return String.Format(
                    builder.ToString().Replace(":{", "\t:  {"),
                    Line,
                    cfg.DisplayName,
                    cfg.Version,
                    cfg.Description,
                    cfg.Company,
                    cfg.ServiceName,
                    cfg.ServiceUser
                );
            }
        }

        /// <summary>
        /// Installer help string
        /// </summary>
        string HelpString => String.Format(
                    Messages.InstallerHelp,
                    ServiceState.Configuration.DisplayName,
                    ServiceState.Configuration.Description,
                    Path.GetFileName(Application.ExecutablePath)
                );

        /// <summary>
        /// Entry assembly (exe, not dll).
        /// </summary>
        private static readonly Assembly EntryAssembly = Assembly.GetEntryAssembly();

        /// <summary>
        /// Program command line arguments
        /// </summary>
        private readonly CommandLine ProgramArgs;

        /// <summary>
        /// Value which should be returned from Main()
        /// </summary>
        public int ReturnValue;

        /// <summary>
        /// Creates new WindowsService console starter / installer.
        /// </summary>
        /// <param name="commandLine">Program <see cref="CommandLine"/> instance.</param>
        public ServiceConsole(CommandLine commandLine) {
            ServiceState.Configuration = ServiceConfiguration.Default;
            ProgramArgs = commandLine;
            Init();
        }

        /// <summary>
        /// Creates new WindowsService console starter / installer.
        /// </summary>
        /// <param name="args">Program command line arguments.</param>
        public ServiceConsole(string[] args) : this(new CommandLine(args)) { }

        /// <summary>
        /// Reads configuration and interprets commandline options to eihter run, install, uninstall or test the service.
        /// </summary>
        /// <returns></returns>
        public virtual int Init() {
            if (ProgramArgs.IsEmpty) ServiceBase.Run(new T());
            else if (ProgramArgs.HasOption("i")) Install();
            else if (ProgramArgs.HasOption("u")) Uninstall();
            else if (ProgramArgs.HasOption("t")) Test();
            else Help();
            return ReturnValue;
        }

        /// <summary>
        /// Installs the service
        /// </summary>
        void Install() {
            Console.Write(InfoString);
            var eventSource = ServiceState.Configuration.DisplayName;
            var eventLog = ServiceState.Configuration.Company;
            var localMachine = ".";
            if (eventSource == ServiceState.Configuration.ServiceName) {
                Console.WriteLine(Messages.InvalidEventSourceName);
                ReturnValue = 1;
                return;
            }
            if (EventLog.SourceExists(eventSource)) {
                var actualLog = EventLog.LogNameFromSourceName(eventSource, localMachine);
                if (actualLog != eventLog) {
                    EventLog.DeleteEventSource(eventSource);
                    Console.WriteLine(String.Format(Messages.EventSourceExists, eventSource, actualLog));
                    Console.WriteLine(Messages.RestartRequired);
                    ReturnValue = 1;
                    return;
                }
            }
            try {
                using (var i = new AssemblyInstaller(EntryAssembly, null) { UseNewContext = true }) {
                    var s = new Hashtable();
                    try {
                        i.Install(s);
                        i.Commit(s);
                    } catch {
                        try {
                            i.Rollback(s);
                        } catch { }
                        throw;
                    }
                }
                Console.WriteLine(Messages.Done);
            } catch (Exception x) {
                Console.Error.WriteLine(x.Message);
                ReturnValue = 1;
            }
        }

        /// <summary>
        /// Handles uninstall options.
        /// </summary>
        void Uninstall() {
            var eventSource = ServiceState.Configuration.DisplayName;
            if (EventLog.SourceExists(eventSource)) {
                EventLog.DeleteEventSource(eventSource);
                Console.WriteLine(Messages.RestartAdvised);
            }
            string s = ProgramArgs.HasOption("s") ? ProgramArgs.Options["s"] : null;
            string e = ProgramArgs.HasOption("e") ? ProgramArgs.Options["e"] : null;
            string l = ProgramArgs.HasOption("l") ? ProgramArgs.Options["l"] : null;
            if (string.IsNullOrEmpty(s)) {
                if (String.IsNullOrEmpty(e) && String.IsNullOrEmpty(l)) UninstallThis();
            } else UninstallService(s);
            if (!String.IsNullOrEmpty(e)) {
                if (EventLog.SourceExists(e)) {
                    Console.Write(String.Format(Messages.DeletingEventSource, e));
                    EventLog.DeleteEventSource(e);
                    Console.WriteLine(Messages.OK);
                } else {
                    Console.WriteLine(String.Format(Messages.NoEventSource, e));
                    ReturnValue = 1;
                }
            }
            if (!String.IsNullOrEmpty(l)) {
                if (EventLog.Exists(l)) {
                    Console.Write(String.Format(Messages.DeletingEventLog, l));
                    EventLog.Delete(l);
                    Console.WriteLine(Messages.OK);
                } else {
                    Console.WriteLine(String.Format(Messages.NoEventLog, l));
                    ReturnValue = 1;
                }
            }
        }

        /// <summary>
        /// Uninstalls this service
        /// </summary>
        void UninstallThis() {
            try {
                using (var i = new AssemblyInstaller(EntryAssembly, null) { UseNewContext = true }) {
                    IDictionary s = new Hashtable();
                    try {
                        i.Uninstall(s);
                    } catch {
                        try {
                            i.Rollback(s);
                        } catch { }
                        throw;
                    }
                }
                Console.WriteLine(Messages.Done);
            } catch (Exception x) {
                Console.Error.WriteLine(x.Message);
                ReturnValue = 1;
            }
        }

        /// <summary>
        /// Uninstalls specified service
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        void UninstallService(string name) {
            try {
                using (var i = new System.ServiceProcess.ServiceInstaller() { Context = new InstallContext(), ServiceName = name }) {
                    try {
                        i.Uninstall(null);
                    } catch {
                        try {
                            i.Rollback(null);
                        } catch { }
                        throw;
                    }
                    Console.WriteLine(Messages.Done);
                }
            } catch (Exception x) {
                Console.Error.WriteLine(x.Message);
                ReturnValue = 1;
            }
        }

        /// <summary>
        /// Tests service in console
        /// </summary>
        void Test() {
            ServiceState.IsInTestMode = true;
            try {
                Console.WindowWidth = 160;
                Console.WindowHeight = 50;
            }
            catch { }
            Console.Write(InfoString);
            Console.WriteLine(Messages.InitializingService);
            var service = new T();
            Console.WriteLine(Messages.Starting);
            service.Start();

            ConsoleEx.ConsoleEx.WaitForCtrlC();
            

            service.Shutdown();
            Console.WriteLine(Messages.Done);
        }

        /// <summary>
        /// Displays installer help
        /// </summary>
        void Help() => Console.WriteLine(HelpString);

    }

}