# Woof.Service quick usage guide

## To create self-installing Windows Service using Woof.Service

- Start new `.NET Framework Console Application`.
- Edit `Application`/`Assembly Information`, fill **every** fied, make sure `Title` and `Product` fields **are not the same**.
- The `Title` field will be used as service display name, the `Product` as the service strong name, `Company` as Windows log name.
- Get `Woof.Service`

    ```pm
    Install-Package Woof.Service -Version 2.0.0
    ```

- Create new `ServiceInstaller.cs` file in main directory with following content:
  
    ```cs
    using Woof.ServiceEx;
    public sealed class ServiceInstaller : ServiceInstallerEx { }
    public sealed class ServiceProcessInstaller : ServiceProcessInstallerEx { }
    ```

- Create `Messages` directory.
- Create empty `Service.resx` file.
- Create main service class `Service.cs` extending `ServiceBaseEx`:

    ```cs
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Woof.ServiceEx;

    namespace ServiceTest {

        class Service : ServiceBaseEx {

            public new static Service Instance => ServiceBaseEx.Instance as Service;

            public Service() : base(Messages.Service.ResourceManager) { }

            protected override void OnStart(string[] args) {
                base.OnStart(args);
                // here goes the code to perform on service start...
            }

            protected override void OnStop() {
                base.OnStop();
                // here goes the clean up on service stop...
            }

        }

    }
    ```

- Edit `Program.cs` file like this:

    ```cs
    using Woof.ServiceEx;

    namespace ServiceTest {

        class Program {

            static int Main(string[] args) => new ServiceConsole<Service>(args).ReturnValue;

        }

    }
    ```

- Compile...
- Test with `-t` switch, install with `-i` switch, uninstall with `-u` switch. More help with `-h` switch.
- Refer to `Woof.ServiceEx.ServiceBaseEx.Signal()` and `Woof.ServiceEx.ServiceBaseEx.WriteEvent()` XML documentation to write to the service log correctly.
- Use `Messages\Service.resx` resource file to define standard localized event messages like:

    | Name  | Value                          | Comment     |
    |:------|:-------------------------------|:------------|
    | I1001 | My dummy info event message    |             |
    | W2001 | My dummy warning event message |             |
    | E3001 | My dummy error event message   |             |

- Use `ResXManager` extension to localize the messages.
- The event names must consist of severity character being one of `'I'`, `'W'`, or `'E'` and numeric event identifier.
- The message values can contain `{0}...{n}` parameter placeholders in this exact format.
- The pre-configured event messages are available via `Signal()` method.
- For all other events use `WriteEvent()` method with `0` as event identifier.
- **WARNING:** Consider `CurrentDirectory` is set to `C:\Windows\System32` when refering to any files.
- To refer to a file in service `exe` directory use following:

    ```cs
    var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
    ```

- Note the service started as `LocalSystem` user **cannot use GUI**, however user interaction is possible using inter-process communication available in `Woof.Core` package, `Woof.SystemEx` namespace. `WPF` or `WinForms` application can interact with the service.

## Q&A

---

- **Is there a easier or simpler way of doing this?**
- Nope. Not for now but stay tuned, maybe a sample package / project template will be released soon.

---

- **How safe and stable are those libraries?**
- Tested for years on real production server environments.

---

- **.NET Core...?**
- Nope. Windows OS (desktop / server) is required.

## Disclaimer

Installing a Windows Service on a server is a serious thing.
Be warned you can do some serious damage to the system using this code. Get a testing environment (like your local PC) to test the service properly before deploying to production. CodeDog Ltd is not responsible for any damage caused using this library. You are.
