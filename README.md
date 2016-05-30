PhotonWire
===
Typed Asynchronous RPC Layer for Photon Server + Unity

What is PhotonWire?
---
PhotonWire is built on Exit Games's [Photon Server](https://www.photonengine.com/en/onpremise). PhotonWire provides client-server RPC with Photon Unity Native SDK and server-server RPC with Photon Server SDK. PhotonWire mainly aims to fully controll server side logic.

* TypeSafe, Server-Server uses dynamic proxy, Client-Server uses T4 pre-generate
* HighPerformance, Fully Asynchronous(Server is async/await, Client is UniRx) and pre-generated serializer by [MsgPack](http://msgpack.org/)
* Fully integrated with Visual Studio
* Tools, PhotonWire.HubInvoker can invoke API directly 

Transparent debugger in Visual Studio, Unity -> Photon Server -> Unity.

![bothdebug](https://cloud.githubusercontent.com/assets/46207/15651046/f0931f46-26b7-11e6-979c-b8a766511617.gif)

PhotonWire.HubInvoker is powerful API debugging tool.

![image](https://cloud.githubusercontent.com/assets/46207/15654696/527cbdf2-26d1-11e6-9213-063bf873fd81.png)

Getting Started -  Server
---
In Visual Studio(2015 or higher), create new .NET 4.6(or higher) `Class Library Project`. For example sample project name - `GettingStarted.Server`.

In Package Manager Console, add PhotonWire NuGet package.

* PM> Install-Package PhotonWire

It includes `PhotonWire.Server` and `PhotonWire.Analyzer`.

Package does not includes Photon SDK, please download from [Photon Server SDK](https://www.photonengine.com/en-US/OnPremise/Download). Server Project needs `lib/ExitGamesLibs.dll`, `lib/Photon.SocketServer.dll` and `lib/PhotonHostRuntimeInterfaces.dll`.

```csharp
using PhotonWire.Server;

namespace GettingStarted.Server
{
    // Application Entrypoint for Photon Server.
    public class Startup : PhotonWireApplicationBase
    {

    }
}
```

Okay, Let's create API! Add C# class file `MyFirstHub.cs`.

```csharp
using PhotonWire.Server;

namespace GettingStarted.Server
{
    [Hub(0)]
    public class MyFirstHub : Hub
    {
        [Operation(0)]
        public int Sum(int x, int y)
        {
            return x + y;
        }
    }
}
```

![image](https://cloud.githubusercontent.com/assets/46207/15641589/f703ccb2-267c-11e6-8aa2-9a919bdbbecd.png)

Hub Type needs `HubAttribute` and Method needs `OperationAttribute`. `PhotonWire.Analyzer` detects there rules. You may only follow it.

Configuration sample. config file *must be UTF8 without BOM*.

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

And modify property, Copy to Output Directory `Copy always`

![image](https://cloud.githubusercontent.com/assets/46207/15642096/125b4a1e-2680-11e6-98e5-6d9e0a0353e4.png)

Here is the result of Project Structure.

![image](https://cloud.githubusercontent.com/assets/46207/15641797/2fdba392-267e-11e6-8244-0428adbe150a.png)

Start and Debug Server Codes on Visual Studio
---
PhotonWire application is hosted by `PhotonSocketServer.exe`. `PhotonSocketServer.exe` is in Photon Server SDK, copy from `deploy/bin_64` to  `$(SolutionDir)\PhotonLibs\bin_Win64`.

Open Project Properties -> Build Events, write Post-build event commandline:

```
xcopy "$(TargetDir)*.*" "$(SolutionDir)\PhotonLibs\$(ProjectName)\bin\" /Y /Q
```

In Debug tab, set up three definitions.

![image](https://cloud.githubusercontent.com/assets/46207/15642631/894128b2-2683-11e6-88b9-69665699fd56.png)

```
// Start external program:
/* Absolute Dir Paths */\PhotonLibs\bin_Win64\PhotonSocketServer.exe

// Star Options, Command line arguments:
/debug GettingStarted /config GettingStarted.Server\bin\PhotonServer.config

// Star Options, Working directory:
/* Absolute Path */\PhotonLibs\
```

Press `F5` to start debugging. If cannot start debugging, please see log. Log exists under `PhotonLibs\log`. If encounts `Exception: CXMLDocument::LoadFromString()`, please check config file encoding, *must be UTF8 without BOM*.

Let's try to invoke from test client. `PhotonWire.HubInvoker` is hub api testing tool. It exists at `$(SolutionDir)\packages\PhotonWire.1.0.0\tools\PhotonWire.HubInvoker\PhotonWire.HubInvoker.exe`.

Configuration,

ProcessPath | Argument | WorkingDirectory is same as Visual Studio's Debug Tab.
DllPath is `/* Absolute Path */\PhotonLibs\GettingStarted.Server\bin\GettingStarted.Server.dll`

Press Reload button, you can see like following image.

![image](https://cloud.githubusercontent.com/assets/46207/15644476/4e16f7ac-268e-11e6-9053-c6d29209ca49.png)



At first, press Connect button to connect target server. And Invoke method, please try x = 100, y = 300 and press Send button.

In visual studio, if set the breakpoint, you can step debugging and see, modify variables.

![image](https://cloud.githubusercontent.com/assets/46207/15644597/f39d935c-268e-11e6-970c-67c4e1a48400.png)

and HubInvoker shows return value at log.

```
Connecting : 127.0.0.1:4530 GettingStarted
Connect:True
+ MyFirstHub/Sum:400
```
There are basic steps of create server code. 

Getting Started - Unity Client
---
Download and Import `PhotonWire.UnityClient.unitypackage` from [release page](https://github.com/neuecc/PhotonWire/releases). If encounts `Unhandled Exception: System.Reflection.ReflectionTypeLoadException: The classes in the module cannot be loaded.`, Please change Build Settings -> Optimization -> Api Compatibility Level -> .NET 2.0.

![image](https://cloud.githubusercontent.com/assets/46207/15645273/70dbb1c0-2692-11e6-8716-9076fe3d81a1.png)

PhotonWire's Unity Client needs additional SDK.

* Download [Photon Server SDK](https://www.photonengine.com/en-US/OnPremise/Download) and pick `lib/Photon3Unity3D.dll` to `Assets\Plugins\Dll`.
* Import [UniRx](https://github.com/neuecc/UniRx) from asset store.

Add Unity Generated Projects to Solution.

![image](https://cloud.githubusercontent.com/assets/46207/15650356/e005cf7a-26b2-11e6-8b08-70569471ce13.png)

> You can choose Unity generated solution based project or Standard solution based project. Benefit of Unity generated based is better integrated with Unity Editor(You can double click source code!) but solution path becomes strange. 

Search `Assets/Plugins/PhotonWire/PhotonWireProxy.tt` under `GettingStarted.UnityClient.CSharp.Plugins` and configure it, change the dll path and assemblyName. This file is typed client generator of server definition.

```
<#@ assembly name="$(SolutionDir)\GettingStarted.Server\bin\Debug\MsgPack.dll" #>
<#@ assembly name="$(SolutionDir)\GettingStarted.Server\bin\Debug\GettingStarted.Server.dll" #>
<#
    // 1. ↑Change path to Photon Server Project's DLL and Server MsgPack(not client) DLL

    // 2. Make Configuration -----------------------

    var namespaceName = "GettingStarted.Client"; // namespace of generated code
    var assemblyName = "GettingStarted.Server"; // Photon Server Project's assembly name
    var baseHubName = "Hub`1";  // <T> is `1, If you use base hub, change to like FooHub`1.
    var useAsyncSuffix = true; // If true FooAsync

    // If WPF, use "DispatcherScheduler.Current"
    // If ConsoleApp, use "CurrentThreadScheduler.Instance"
    // If Unity, use "Scheduler.MainThread"
    var mainthreadSchedulerInstance = "Scheduler.MainThread";
    
    // End of Configuration-----------------
```

Right click -> Run Custom Tool generates typed client(`PhotonWireProxy.Generated.cs`).

![image](https://cloud.githubusercontent.com/assets/46207/15650491/ea1dfb44-26b3-11e6-982b-de44be2ab99c.png)

Setup has been completed! Let's connect PhotonServer. Put uGUI Button to scene and attach following `PhotonButton` script. 

```csharp
using ExitGames.Client.Photon;
using PhotonWire.Client;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace GettingStarted.Client
{
    public class PhotonButton : MonoBehaviour
    {
        // set from inspector
        public Button button;

        ObservablePhotonPeer peer;
        MyFirstHubProxy proxy;

        void Start()
        {
            // Create Photon Connection
            peer = new ObservablePhotonPeer(ConnectionProtocol.Tcp, peerName: "PhotonTest");

            // Create typed server rpc proxy
            proxy = peer.CreateTypedHub<MyFirstHubProxy>();

            // Async Connect(return IObservable)
            peer.ConnectAsync("127.0.0.1:4530", "GettingStarted.Server")
                .Subscribe(x =>
                {
                    UnityEngine.Debug.Log("IsConnected?" + x);
                });


            button.OnClickAsObservable().Subscribe(_ =>
            {

                // Invoke.Method calls server method and receive result.
                proxy.Invoke.SumAsync(100, 300)
                    .Subscribe(x => Debug.Log("Server Return:" + x));

            });
        }

        void OnDestroy()
        {
            // disconnect peer.
            peer.Dispose();
        }
    }

}
```

and press button, you can see `Server Return:400`.

If shows Windows -> PhotonWire, you can see connection stae and graph of sent, received bytes.

![image](https://cloud.githubusercontent.com/assets/46207/15650771/fbdf81d4-26b5-11e6-87d0-811e1e77ca8f.png)

Debugging both Server and Unity, I recommend use [SwitchStartupProject](https://visualstudiogallery.msdn.microsoft.com/f4e1be8c-b2dd-4dec-b273-dd88f8818571) extension.

Create Photon + Unity multi startup.
 
![image](https://cloud.githubusercontent.com/assets/46207/15650843/6a69a288-26b6-11e6-9aa6-ca520e47afd3.png)

And debug it, top is server, bottom is unity.

![bothdebug](https://cloud.githubusercontent.com/assets/46207/15651046/f0931f46-26b7-11e6-979c-b8a766511617.gif)

Getting Started - .NET Client
---
.NET Client can use ASP.NET, ConsoleApplication, WPF, etc.

* PM> Install-Package PhotonWire
* Download [Photon Server SDK](https://www.photonengine.com/en-US/OnPremise/Download) and pick `lib/ExitGamesLibs.dll` and `lib/Photon3DotNet.dll`.

Getting Started - Sharing Classes
---
PhotonWire supports complex type serialize by MsgPack. At first, share request/response type both server and client. 

Create .NET 3.5 Class Library Project - `GettingStarted.Share` and add the Person.cs.

```csharp
namespace GettingStarted.Share
{
    public class Person
    {
        public int Age { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}
```

Open project property window, Build Events -> Post-build event command line, add the following copy dll code.
```
xcopy "$(TargetDir)*.*" "$(SolutionDir)\GettingStarted.UnityClient\Assets\Plugins\Dll\" /Y /Q
```

Move to `GettingStareted.Server`, reference project `GettingStarted.Share` and add new method in MyFirstHub.

```csharp
[Operation(1)]
public Person CreatePerson(int seed)
{
    var rand = new Random(seed);

    return new Person
    {
        FirstName = "Yoshifumi",
        LastName = "Kawai",
        Age = rand.Next(0, 100)
    };
}
```

Maybe you encount error message, response type must be DataContract. You can modify quick fix.

![image](https://cloud.githubusercontent.com/assets/46207/15651560/63aa7490-26bb-11e6-8b87-11f909eba119.png)

And add the reference `System.Runtime.Serialization` to `GettingStarted.Share`.

Build `GettingStarted.Server`, and Run Custom Tool of `PhotonWireProxy.tt`.

```csharp
// Unity Button Click
proxy.Invoke.CreatePersonAsync(Random.Range(0, 100))
    .Subscribe(x =>
    {
        UnityEngine.Debug.Log(x.FirstName + " " + x.LastName + " Age:" + x.Age);
    });
```

Response deserialization is multi threaded and finally return to main thread by UniRx so deserialization does not affect performance. Furthermore deserializer is used pre-generated optimized serializer.  

```csharp
[System.CodeDom.Compiler.GeneratedCodeAttribute("MsgPack.Serialization.CodeDomSerializers.CodeDomSerializerBuilder", "0.6.0.0")]
[System.Diagnostics.DebuggerNonUserCodeAttribute()]
public class GettingStarted_Share_PersonSerializer : MsgPack.Serialization.MessagePackSerializer<GettingStarted.Share.Person> {
        
    private MsgPack.Serialization.MessagePackSerializer<int> _serializer0;
        
    private MsgPack.Serialization.MessagePackSerializer<string> _serializer1;
        
    public GettingStarted_Share_PersonSerializer(MsgPack.Serialization.SerializationContext context) : 
            base(context) {
        MsgPack.Serialization.PolymorphismSchema schema0 = default(MsgPack.Serialization.PolymorphismSchema);
        schema0 = null;
        this._serializer0 = context.GetSerializer<int>(schema0);
        MsgPack.Serialization.PolymorphismSchema schema1 = default(MsgPack.Serialization.PolymorphismSchema);
        schema1 = null;
        this._serializer1 = context.GetSerializer<string>(schema1);
    }
        
    protected override void PackToCore(MsgPack.Packer packer, GettingStarted.Share.Person objectTree) {
        packer.PackArrayHeader(3);
        this._serializer0.PackTo(packer, objectTree.Age);
        this._serializer1.PackTo(packer, objectTree.FirstName);
        this._serializer1.PackTo(packer, objectTree.LastName);
    }
        
    protected override GettingStarted.Share.Person UnpackFromCore(MsgPack.Unpacker unpacker) {
        GettingStarted.Share.Person result = default(GettingStarted.Share.Person);
        result = new GettingStarted.Share.Person();
        
        int unpacked = default(int);
        int itemsCount = default(int);
        itemsCount = MsgPack.Serialization.UnpackHelpers.GetItemsCount(unpacker);
        System.Nullable<int> nullable = default(System.Nullable<int>);
        if ((unpacked < itemsCount)) {
            nullable = MsgPack.Serialization.UnpackHelpers.UnpackNullableInt32Value(unpacker, typeof(GettingStarted.Share.Person), "Int32 Age");
        }
        if (nullable.HasValue) {
            result.Age = nullable.Value;
        }
        unpacked = (unpacked + 1);
        string nullable0 = default(string);
        if ((unpacked < itemsCount)) {
            nullable0 = MsgPack.Serialization.UnpackHelpers.UnpackStringValue(unpacker, typeof(GettingStarted.Share.Person), "System.String FirstName");
        }
        if (((nullable0 == null) 
                    == false)) {
            result.FirstName = nullable0;
        }
        unpacked = (unpacked + 1);
        string nullable1 = default(string);
        if ((unpacked < itemsCount)) {
            nullable1 = MsgPack.Serialization.UnpackHelpers.UnpackStringValue(unpacker, typeof(GettingStarted.Share.Person), "System.String LastName");
        }
        if (((nullable1 == null) 
                    == false)) {
            result.LastName = nullable1;
        }
        unpacked = (unpacked + 1);
        
        return result;
    }
}
```

Startup Configuration
---
Override the Startup methods, you can configure options.

```csharp
public class Startup : PhotonWireApplicationBase
{
    // When throw exception, returns exception information.
    public override bool IsDebugMode
    {
        get
        {
            return true;
        }
    }

    // connected peer is server to server? 
    protected override bool IsServerToServerPeer(InitRequest initRequest)
    {
        return (initRequest.ApplicationId == "MyMaster");
    }

    // initialize, if needs server to server connection, write here.
    protected override void SetupCore()
    {
        var _ = ConnectToOutboundServerAsync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 4530), "MyMaster");
    }

    // tear down
    protected override void TearDownCore()
    {
        base.TearDownCore();
    }
}
```

More options, see [reference](https://github.com/neuecc/PhotonWire/wiki).

Hub
---
Hub concept is highly inspired by [ASP.NET SignalR](http://www.asp.net/signalr/overview/getting-started) so SignalR's document is maybe useful.

Hub supported typed client broadcast. 

```csharp
// define client interface.
public interface ITutorialClient
{
    [Operation(0)]
    void GroupBroadcastMessage(string message);
}

// Hub<TClient>
[Hub(100)]
public class Tutorial : PhotonWire.Server.Hub<ITutorialClient>
{
    [Operation(2)]
    public void BroadcastAll(string message)
    {
        // Get ClientProxy from Clients property, choose target and Invoke.
        this.Clients.All.GroupBroadcastMessage(message);
    }
}
```

Hub have two instance property, [OperationContext](https://github.com/neuecc/PhotonWire/wiki/PhotonWire.Server#operationcontext) and [Clients](https://github.com/neuecc/PhotonWire/wiki/PhotonWire.Server#hubcallerclientproxyt). OperationContext is information per operation. It has `Items` - per operation storage(`IDictionary<object, object>`), `Peer` - client connection of this operation, `Peer.Items`  - per peer lifetime storage(`ConcurrentDictionary<object, object>`) and more.

Peer.RegisterDisconnectAction is sometimes important.

```csharp
this.Context.Peer.RegisterDisconnectAction((reasonCode, readonDetail) =>
{
    // do when disconnected.
});
```

Clients is proxy of broadcaster. `All` is broadcast to all server, `Target` is only send to target peer, and more.

`Group` is multipurpose channel. You can add/remove per peer `Peer.AddGroup/RemoveGroup`. And can use from Clients.


```csharp
[Operation(3)]
public void RegisterGroup(string groupName)
{
    // Group is registered by per connection(peer)
    this.Context.Peer.AddGroup(groupName);
}

[Operation(4)]
public void BroadcastTo(string groupName, string message)
{
    // Get ITutorialClient -> Invoke method
    this.Clients.Group(groupName).GroupBroadcastMessage(message);
}
```

Operation response supports async/await.

```csharp
[Operation(1)]
public async Task<string> GetHtml(string url)
{
    var httpClient = new HttpClient();
    var result = await httpClient.GetStringAsync(url);

    // Photon's String deserialize size limitation
    var cut = result.Substring(0, Math.Min(result.Length, short.MaxValue - 5000));

    return cut;
}
```

Server to Server
---
PhotonWire supports Server to Server. Server to Server connection also use Hub system. PhotonWire provides three hubs.

* ClientPeer - Hub  
* OutboundS2SPeer - ServerHub
* InboundS2SPeer - ReceiveServerHub

Implements ServerHub.

```csharp
// 1. Inherit ServerHub
// 2. Add HubAttribute
[Hub(54)]
public class MasterTutorial : PhotonWire.Server.ServerToServer.ServerHub
{
    // 3. Create virtual, async method
    // 4. Add OperationAttribute
    [Operation(0)]
    public virtual async Task<int> Multiply(int x, int y)
    {
        return x * y;
    }
}
```

Call from Hub.

```csharp
[Operation(5)]
public async Task<int> ServerToServer(int x, int y)
{
    var mul = await GetServerHubProxy<MasterTutorial>().Single.Multiply(x, y);
    
    // If is not in Hub, You can get ClientProxy from global PeerManager
    // PeerManager.GetServerHubContext<MasterTutorial>().Clients.Single.Multiply(x, y);
    
    return mul;
}
```

GetServerHubProxy is magic by dynamic proxy.

![image](https://cloud.githubusercontent.com/assets/46207/15653376/a7139f5c-26c7-11e6-98ff-25bb77612378.png)

ReceiveServerHub is similar with ServerHub. 

```csharp
[Hub(10)]
public class BroadcasterReceiveServerHub : ReceiveServerHub
{
    [Operation(20)]
    public virtual async Task Broadcast(string group, string msg)
    {
        // Send to clients.
        this.GetClientsProxy<Tutorial, ITutorialClient>()
            .Group(group)
            .GroupBroadcastMessage(msg);
    }
}
```

Call from ServerHub.

```csharp
[Operation(1)]
public virtual async Task Broadcast(string group, string message)
{
    // Invoke all receive server hubs
    await GetReceiveServerHubProxy<BroadcasterReceiveServerHub>()
        .All.Invoke(x => x.Broadcast(group, message));
}
```

Client Receiver.

```csharp
// receive per operation
proxy.Receive.GroupBroadcastMessage.Subscribe();

// or receive per client 
proxy.RegisterListener(/* TutorialProxy.ITutorialClient */);
```

![image](https://cloud.githubusercontent.com/assets/46207/15655365/7e9aa98e-26d7-11e6-8bfb-97eeea1330f5.png)


Server Cluster
---
Server to Server connection is setup in Startup. That's all.

```csharp
public class Startup : PhotonWireApplicationBase
{
    protected override bool IsServerToServerPeer(InitRequest initRequest)
    {
        return (initRequest.ApplicationId == "MyMaster");
    }

    protected override void SetupCore()
    {
        var _ = ConnectToOutboundServerAsync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 4530), "MyMaster");
    }
}
```

You can choice own cluster type.

![image](https://cloud.githubusercontent.com/assets/46207/15654243/7d2847aa-26cd-11e6-95cc-4f77c441f213.png)

PhotonWire supports everything.

Configuration
---
PhotonWire supports app.config. Here is sample config

```xml
<configuration>
    <configSections>
        <section name="photonWire" type="PhotonWire.Server.Configuration.PhotonWireConfigurationSection, PhotonWire.Server" />
    </configSections>
    
    <photonWire>
        <connection>
            <add ipAddress="127.0.0.1" port="4530" applicationName="PhotonSample.MasterServer" />
            <add ipAddress="127.0.0.1" port="4531" applicationName="PhotonSample.MasterServer1" />
            <add ipAddress="127.0.0.1" port="4532" applicationName="PhotonSample.MasterServer2" />
        </connection>
    </photonWire>
</configuration>
```

```csharp
public class GameServerStartup : PhotonWire.Server.PhotonWireApplicationBase
{
    // Only Enables GameServer Hub.
    protected override string[] HubTargetTags
    {
        get
        {
            return new[] { "GameServer" };
        }
    }

    protected override void SetupCore()
    {
        // Load from Configuration file.
        foreach (var item in PhotonWire.Server.Configuration.PhotonWireConfigurationSection.GetSection().GetConnectionList())
        {
            var ip = new IPEndPoint(IPAddress.Parse(item.IPAddress), item.Port);
            var _ = ConnectToOutboundServerAsync(ip, item.ApplicationName);
        }
    }
}
```

Filter
---
PhotonWire supports OWIN like filter.

```csharp
public class TestFilter : PhotonWireFilterAttribute
{
    public override async Task<object> Invoke(OperationContext context, Func<Task<object>> next)
    {
        var path = context.Hub.HubName + "/" + context.Method.MethodName;
        try
        {
            Debug.WriteLine("Before:" + path + " - " + context.Peer.PeerKind);
            var result = await next();
            Debug.WriteLine("After:" + path);
            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Ex " + path + " :" + ex.ToString());
            throw;
        }
        finally
        {
            Debug.WriteLine("Finally:" + path);
        }
    }
}

[Hub(3)]
public class MasterTest : ServerHub
{
    [TestFilter] // use filter
    [Operation(5)]
    public virtual async Task<string> EchoAsync(string msg)
    {
        return msg;
    }
}
```

CustomError
---
If you want to returns custom error, you can throw `CustomErrorException` on server. It can receive client.

``csharp
// Server
[Operation(0)]
public void ServerError()
{
    throw new CustomErrorException { ErrorMessage = "Custom Error" }; 
}

// Client
proxy.Invoke.ServerError()
    .Catch((CustomErrorException ex) =>
    {
        UnityEngine.Debug.Log(ex.ErrorMessage);
    })
    .Subscribe();
```

PeerManager
---
[PeerManager](https://github.com/neuecc/PhotonWire/wiki/PhotonWire.Server#peermanager) is global storage of peer and peer groups.

Logging, Monitoring
---
Default logging uses EventSource. You can monitor easily by [EtwStream](https://github.com/neuecc/EtwStream/).

```csharp
ObservableEventListener.FromTraceEvent("PhotonWire").DumpWithColor();
```

Logging point list can see [IPhotonWireLogger reference](https://github.com/neuecc/PhotonWire/wiki/PhotonWire.Server.Diagnostics#iphotonwirelogger).

References
---
Available at [GitHub/PhotonWire/wiki](https://github.com/neuecc/PhotonWire/wiki).

Help & Contribute
---
Ask me any questions to GitHub issues.  

Author Info
---
Yoshifumi Kawai(a.k.a. neuecc) is a software developer in Japan.  
He is the Director/CTO at Grani, Inc.  
Grani is a top social game developer in Japan.  
He is awarding Microsoft MVP for Visual C# since 2011.  
He is known as the creator of [UniRx](https://github.com/neuecc/UniRx/)(Reactive Extensions for Unity)

Blog: http://neue.cc/ (Japanese)  
Twitter: https://twitter.com/neuecc (Japanese)

License
---
This library is under the MIT License.
