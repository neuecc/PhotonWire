using PhotonWire.Sample.ServerApp.MasterServer.ServerHubs;
using PhotonWire.Server;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhotonWire.Sample.ServerApp.Hubs
{
    // Called from UnitTest

    [Hub(0)]
    public class ForUnitTest : Hub
    {
        // PhotonSupported

        [Operation(0)]
        public int Echo(int x) => x;
        [Operation(1)]
        public byte Echo(byte x) => x;
        [Operation(2)]
        public bool Echo(bool x) => x;
        [Operation(3)]
        public short Echo(short x) => x;
        [Operation(4)]
        public long Echo(long x) => x;
        [Operation(5)]
        public float Echo(float x) => x;
        [Operation(6)]
        public double Echo(double x) => x;
        [Operation(7)]
        public int[] Echo(int[] x) => x;
        [Operation(8)]
        public string Echo(string x) => x;
        [Operation(9)]
        public byte[] Echo(byte[] x) => x;

        // Extra

        [Operation(10)]
        public DateTime Echo(DateTime x) => x;
        [Operation(11)]
        public Uri Echo(Uri x) => x;
        [Operation(12)]
        public int? Echo(int? x) => x;
        [Operation(13)]
        public double? Echo(double? x) => x;

        // Collection, Array, ComplexType, etc...

        [Operation(14)]
        public double[] Echo(double[] x) => x;
        [Operation(15)]
        public List<double> Echo(List<double> x) => x;
        [Operation(16)]
        public Dictionary<string, int> Echo(Dictionary<string, int> x) => x;
        [Operation(17)]
        public MyClass Echo(MyClass x) => x;

        // Enum...

        [Operation(18)]
        public string Echo(Yo yo) => yo.ToString();
        [Operation(19)]
        public string Echo(Yo? yo) => yo?.ToString() ?? "null";

        [Operation(20)]
        public Task<int> Echo2(int x) => GetServerHubProxy<MasterForUnitTest>().Single.Echo(x);

        [Operation(21)]
        public Task<byte> Echo2(byte x) => GetServerHubProxy<MasterForUnitTest>().Single.Echo(x);
        [Operation(22)]
        public Task<bool> Echo2(bool x) => GetServerHubProxy<MasterForUnitTest>().Single.Echo(x);
        [Operation(23)]
        public Task<short> Echo2(short x) => GetServerHubProxy<MasterForUnitTest>().Single.Echo(x);
        [Operation(24)]
        public Task<long> Echo2(long x) => GetServerHubProxy<MasterForUnitTest>().Single.Echo(x);
        [Operation(25)]
        public Task<float> Echo2(float x) => GetServerHubProxy<MasterForUnitTest>().Single.Echo(x);
        [Operation(26)]
        public Task<double> Echo2(double x) => GetServerHubProxy<MasterForUnitTest>().Single.Echo(x);
        [Operation(27)]
        public Task<int[]> Echo2(int[] x) => GetServerHubProxy<MasterForUnitTest>().Single.Echo(x);
        [Operation(28)]
        public Task<string> Echo2(string x) => GetServerHubProxy<MasterForUnitTest>().Single.Echo(x);
        [Operation(29)]
        public Task<byte[]> Echo2(byte[] x) => GetServerHubProxy<MasterForUnitTest>().Single.Echo(x);

        // Extra

        [Operation(30)]
        public Task<DateTime> Echo2(DateTime x) => GetServerHubProxy<MasterForUnitTest>().Single.Echo(x);
        [Operation(31)]
        public Task<Uri> Echo2(Uri x) => GetServerHubProxy<MasterForUnitTest>().Single.Echo(x);
        [Operation(32)]
        public Task<int?> Echo2(int? x) => GetServerHubProxy<MasterForUnitTest>().Single.Echo(x);
        [Operation(33)]
        public Task<double?> Echo2(double? x) => GetServerHubProxy<MasterForUnitTest>().Single.Echo(x);

        // Collection, Array, ComplexType, etc...

        [Operation(34)]
        public Task<double[]> Echo2(double[] x) => GetServerHubProxy<MasterForUnitTest>().Single.Echo(x);
        [Operation(35)]
        public Task<List<double>> Echo2(List<double> x) => GetServerHubProxy<MasterForUnitTest>().Single.Echo(x);
        [Operation(36)]
        public Task<Dictionary<string, int>> Echo2(Dictionary<string, int> x) => GetServerHubProxy<MasterForUnitTest>().Single.Echo(x);
        [Operation(37)]
        public Task<MyClass> Echo2(MyClass x) => GetServerHubProxy<MasterForUnitTest>().Single.Echo(x);

        // Enum...

        [Operation(38)]
        public Task<string> Echo2(Yo yo) => GetServerHubProxy<MasterForUnitTest>().Single.Echo(yo);
        [Operation(39)]
        public Task<string> Echo2(Yo? yo) => GetServerHubProxy<MasterForUnitTest>().Single.Echo(yo);
    }
}
