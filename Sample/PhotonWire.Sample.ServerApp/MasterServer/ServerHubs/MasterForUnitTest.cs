#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

using PhotonWire.Sample.ServerApp.Hubs;
using PhotonWire.Server;
using PhotonWire.Server.ServerToServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotonWire.Sample.ServerApp.MasterServer.ServerHubs
{
    [Hub(122)]
    public class MasterForUnitTest : ServerHub
    {
        // PhotonSupported

        [Operation(0)]
        public virtual async Task<int> Echo(int x) => x;
        [Operation(1)]
        public virtual async Task<byte> Echo(byte x) => x;
        [Operation(2)]
        public virtual async Task<bool> Echo(bool x) => x;
        [Operation(3)]
        public virtual async Task<short> Echo(short x) => x;
        [Operation(4)]
        public virtual async Task<long> Echo(long x) => x;
        [Operation(5)]
        public virtual async Task<float> Echo(float x) => x;
        [Operation(6)]
        public virtual async Task<double> Echo(double x) => x;
        [Operation(7)]
        public virtual async Task<int[]> Echo(int[] x) => x;
        [Operation(8)]
        public virtual async Task<string> Echo(string x) => x;
        [Operation(9)]
        public virtual async Task<byte[]> Echo(byte[] x) => x;

        // Extra

        [Operation(10)]
        public virtual async Task<DateTime> Echo(DateTime x) => x;

        [Operation(11)]
        public virtual async Task<Uri> Echo(Uri x) => x;
        [Operation(12)]
        public virtual async Task<int?> Echo(int? x) => x;
        [Operation(13)]
        public virtual async Task<double?> Echo(double? x) => x;

        // Collection, Array, ComplexType, etc...

        [Operation(14)]
        public virtual async Task<double[]> Echo(double[] x) => x;
        [Operation(15)]
        public virtual async Task<List<double>> Echo(List<double> x) => x;
        [Operation(16)]
        public virtual async Task<Dictionary<string, int>> Echo(Dictionary<string, int> x) => x;
        [Operation(17)]
        public virtual async Task<MyClass> Echo(MyClass x) => x;

        // Enum...

        [Operation(18)]
        public virtual async Task<string> Echo(Yo yo) => yo.ToString();
        [Operation(19)]
        public virtual async Task<string> Echo(Yo? yo) => yo?.ToString() ?? "null";
    }
}

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously