Package does not includes Photon SDK,
please download from [Photon Server SDK]
https://www.photonengine.com/en-US/OnPremise/Download
Add and reference following dlls.

lib/ExitGamesLibs.dll
lib/Photon.SocketServer.dll
lib/PhotonHostRuntimeInterfaces.dll

And PhotonSocketServer.exe binary copy to $(SolutionDir)\PhotonLibs\bin_Win64

PhotonWire.HubInvoker is in packages\PhotonWire\tools\

---

1. Create Startup.cs

```
using PhotonWire.Server;

public class Startup : PhotonWireApplicationBase
{

}
```

2. Create Hub

```csharp
using PhotonWire.Server;

[Hub(0)]
public class MyFirstHub : Hub
{
    [Operation(0)]
    public int Sum(int x, int y)
    {
        return x + y;
    }
}
```

3. Create PhotonServer.config (must be UTF-8 without BOM)

```xml
<?xml version="1.0" encoding="utf-8"?>
<Configuration>
    <!-- Manual -->
    <!-- http://doc.photonengine.com/en/onpremise/current/reference/server-config-settings -->

    <!-- Instances -->
    <GettingStarted>
        <IOPool>
            <NumThreads>8</NumThreads>
        </IOPool>

        <!-- .NET 4.5~6's CLRVersion is v4.0 -->
        <Runtime
            Assembly="PhotonHostRuntime, Culture=neutral"
            Type="PhotonHostRuntime.PhotonDomainManager"
            CLRVersion="v4.0"
            UnhandledExceptionPolicy="Ignore">
        </Runtime>

        <!-- Configuration of listeners -->
        <TCPListeners>
            <TCPListener
                IPAddress="127.0.0.1"
                Port="4530"
                ListenBacklog="1000"
                InactivityTimeout="60000">
            </TCPListener>
        </TCPListeners>

        <!-- Applications -->
        <Applications Default="GettingStarted.Server" PassUnknownAppsToDefaultApp="true">
            <Application
                Name="GettingStarted.Server"
                BaseDirectory="GettingStarted.Server"
                Assembly="GettingStarted.Server" 
                Type="GettingStarted.Server.Startup"
                EnableShadowCopy="true"
                EnableAutoRestart="true"
                ForceAutoRestart="true"
                ApplicationRootDirectory="PhotonLibs">
            </Application>
        </Applications>

    </GettingStarted>
</Configuration>
```

4. Setup VisualStudio debugging option

// Start external program:
/* Absolute Dir Paths */\PhotonLibs\bin_Win64\PhotonSocketServer.exe

// Star Options, Command line arguments:
/debug GettingStarted /config GettingStarted.Server\bin\PhotonServer.config

// Star Options, Working directory:
/* Absolute Path */\PhotonLibs\