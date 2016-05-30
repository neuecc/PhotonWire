using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ExitGames.Client.Photon;
using PhotonWire.Client;
using System.Runtime.Serialization;
using PhotonWire.Sample.ServerApp.Hubs;

namespace PhotonWire.Sample.ConsoleApp
{
    class ___Test
    {
        [System.Runtime.Serialization.DataMember(Order = 0)]
        public int MyProperty { get; set; }
    }

    class Program// : PhotonWire.Client.TutorialProxy.ITutorialClient
    {
        static void Main(string[] args)
        {
            //var observablePeer = new ObservablePhotonPeer(ConnectionProtocol.Tcp);
            //observablePeer.Timeout = TimeSpan.FromMinutes(15); // toooooo long

            //// observablePeer.DebugOut = DebugLevel.ALL;
            //observablePeer.Connect("127.0.0.1:4530", "ServerApp");

            //Observable.Interval(TimeSpan.FromMilliseconds(50)).Subscribe(_ => observablePeer.Service());

            //StatusCode lastStatus = StatusCode.Disconnect;
            //observablePeer.ObserveStatusChanged().Subscribe(x =>
            //{
            //    lastStatus = x;
            //    Console.WriteLine(x);
            //});

            //Observable.Interval(TimeSpan.FromSeconds(1)).Subscribe(_ =>
            //{
            //    if (lastStatus != StatusCode.Connect)
            //    {
            //        observablePeer.Connect("127.0.0.1:4530", "ServerApp");
            //    }
            //});


            //// Create Typed Proxy
            //var hub = observablePeer.CreateTypedHub<TestHubProxy>();
            //// hub.RegisterListener(new Program()/* this */);

            //while (true)
            //{
            //    var message = Console.ReadLine();

            //    hub.Invoke.EchoAsync("hogehoge").Subscribe(x => Console.WriteLine("R:" + x), ex => Console.WriteLine(ex));

            //    // switch
            //    observablePeer.SwitchConnectionAsync("127.0.0.1:4530", "ServerApp").Subscribe();

            //}
        }
    }
}