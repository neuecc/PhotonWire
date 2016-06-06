using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using ExitGames.Client.Photon;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PhotonWire.Client;
using PhotonWire.Sample.ServerApp.Hubs;

namespace PhotonWire.Server.Test
{
    [TestClass]
    public class EchoTest
    {
        [TestMethod]
        public async Task BuiltinType()
        {
            var hub = Startup.Peer.CreateTypedHub<ForUnitTestProxy>();

            (await hub.Invoke.EchoAsync("aaa")).Is("aaa");
            (await hub.Invoke.EchoAsync(true)).Is(true);
            (await hub.Invoke.EchoAsync(false)).Is(false);
            (await hub.Invoke.EchoAsync((byte)10)).Is((byte)10);
            (await hub.Invoke.EchoAsync((short)1000)).Is((short)1000);
            (await hub.Invoke.EchoAsync(100000L)).Is(100000L);
            (await hub.Invoke.EchoAsync(5.32f)).Is(5.32f);
            (await hub.Invoke.EchoAsync(new[] { 1, 10, 100, 1000, 1000 })).Is(1, 10, 100, 1000, 1000);
            (await hub.Invoke.EchoAsync(new byte[] { 5, 23, 41, 5, 6, 100 })).Is(new byte[] { 5, 23, 41, 5, 6, 100 });
            var now = DateTime.Now;
            (await hub.Invoke.EchoAsync(now)).Is(now);
            (await hub.Invoke.EchoAsync(new Uri("http://hogemoge.com/"))).Is(new Uri("http://hogemoge.com/"));

            (await hub.Invoke.EchoAsync(new Nullable<int>(1000))).Is(1000);
            (await hub.Invoke.EchoAsync(new Nullable<double>(1000.5))).Is(1000.5);
            (await hub.Invoke.EchoAsync((int?)null)).Is((int?)null);

            (await hub.Invoke.EchoAsync(new Nullable<int>())).IsNull();

            (await hub.Invoke.EchoAsync(Yo.C)).Is("C");
            (await hub.Invoke.EchoAsync((Yo?)Yo.B)).Is("B");
        }

        [TestMethod]
        public async Task CollectionType()
        {
            var hub = Startup.Peer.CreateTypedHub<ForUnitTestProxy>();

            (await hub.Invoke.EchoAsync(new[] { 10.5, 20.3, 40.5 })).Is(10.5, 20.3, 40.5);
            (await hub.Invoke.EchoAsync(new List<double> { 10.9, 20.3, 40.5 })).Is(10.9, 20.3, 40.5);
            var dict = (await hub.Invoke.EchoAsync(new Dictionary<string, int> { { "a", 1 }, { "b", 2 } }));
            dict["a"].Is(1);
            dict["b"].Is(2);
        }

        [TestMethod]
        public async Task ComplexType()
        {
            var hub = Startup.Peer.CreateTypedHub<ForUnitTestProxy>();

            var r = await hub.Invoke.EchoAsync(new MyClass
            {
                MyPropertyA = 1000,
                MyPropertyB = "hogehoge",
                MyPropertyC = new MyClass2 { MyProperty = 5000 }
            });

            r.MyPropertyA.Is(1000);
            r.MyPropertyB.Is("hogehoge");
            r.MyPropertyC.MyProperty.Is(5000);
        }

        [TestMethod]
        public void ErrorIfTypeIsInvalid()
        {
            var x = 100; // server's x == string
            short hubId = 0;
            byte opCode = 8;
            var parameter = new System.Collections.Generic.Dictionary<byte, object>();
            parameter.Add(ReservedParameterNo.RequestHubId, hubId);
            parameter.Add(0, PhotonSerializer.Serialize(x));


            var response = AssertEx.Catch<ServerResponseErrorException>(() =>
            {
                Startup.Peer.OpCustomAsync(opCode, parameter, true).Wait();
            });

            response.ReturnCode.Is((short)-1);
        }
    }


    [TestClass]
    public class EchoS2STest
    {
        [TestMethod]
        public async Task BuiltinType()
        {
            var hub = Startup.Peer.CreateTypedHub<ForUnitTestProxy>();

            (await hub.Invoke.Echo2Async("aaa")).Is("aaa");
            (await hub.Invoke.Echo2Async(true)).Is(true);
            (await hub.Invoke.Echo2Async(false)).Is(false);
            (await hub.Invoke.Echo2Async((byte)10)).Is((byte)10);
            (await hub.Invoke.Echo2Async((short)1000)).Is((short)1000);
            (await hub.Invoke.Echo2Async(100000L)).Is(100000L);
            (await hub.Invoke.Echo2Async(5.32f)).Is(5.32f);
            (await hub.Invoke.Echo2Async(new[] { 1, 10, 100, 1000, 1000 })).Is(1, 10, 100, 1000, 1000);
            (await hub.Invoke.Echo2Async(new byte[] { 5, 23, 41, 5, 6, 100 })).Is(new byte[] { 5, 23, 41, 5, 6, 100 });
            var now = DateTime.Now;
            (await hub.Invoke.Echo2Async(now)).Is(now);
            (await hub.Invoke.Echo2Async(new Uri("http://hogemoge.com/"))).Is(new Uri("http://hogemoge.com/"));

            (await hub.Invoke.Echo2Async(new Nullable<int>(1000))).Is(1000);
            (await hub.Invoke.Echo2Async(new Nullable<double>(1000.5))).Is(1000.5);
            (await hub.Invoke.Echo2Async((int?)null)).Is((int?)null);

            (await hub.Invoke.Echo2Async(new Nullable<int>())).IsNull();

            (await hub.Invoke.Echo2Async(Yo.C)).Is("C");
            (await hub.Invoke.Echo2Async((Yo?)Yo.B)).Is("B");
        }

        [TestMethod]
        public async Task CollectionType()
        {
            var hub = Startup.Peer.CreateTypedHub<ForUnitTestProxy>();

            (await hub.Invoke.Echo2Async(new[] { 10.5, 20.3, 40.5 })).Is(10.5, 20.3, 40.5);
            (await hub.Invoke.Echo2Async(new List<double> { 10.9, 20.3, 40.5 })).Is(10.9, 20.3, 40.5);
            var dict = (await hub.Invoke.Echo2Async(new Dictionary<string, int> { { "a", 1 }, { "b", 2 } }));
            dict["a"].Is(1);
            dict["b"].Is(2);
        }

        [TestMethod]
        public async Task ComplexType()
        {
            var hub = Startup.Peer.CreateTypedHub<ForUnitTestProxy>();

            var r = await hub.Invoke.Echo2Async(new MyClass
            {
                MyPropertyA = 1000,
                MyPropertyB = "hogehoge",
                MyPropertyC = new MyClass2 { MyProperty = 5000 }
            });

            r.MyPropertyA.Is(1000);
            r.MyPropertyB.Is("hogehoge");
            r.MyPropertyC.MyProperty.Is(5000);
        }
    }
}
