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
        public async Task<int?> ServerToServerEnum(int? yo)
        {
            var yo2 = await GetServerHubProxy<MasterServer.ServerHubs.MasterTest2>()
                .Single
                .EchoEnumAsync(yo);

            return yo2;
        }
    }
}
