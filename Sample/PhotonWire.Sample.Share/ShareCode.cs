using System;
using System.Runtime.Serialization;

namespace PhotonWire.Sample.ServerApp.Hubs
{
    public class Yappy
    {
        public MoreYappy[] YappyArray { get; set; }
        public int MyProperty { get; set; }
        public string Moge { get; set; }
        public MoreYappy MoreMoreMore { get; set; }
    }

    public class MoreYappy
    {
        public int Dupe { get; set; }
        public DateTime None { get; set; }
    }


    public enum Yo
    {
        A, B, C
    }

    public class Takox
    {
        public int MyTakox { get; set; }
    }

    [DataContract]
    public class MyClass
    {
        [DataMember(Order = 0)]
        public int MyPropertyA { get; set; }
        [DataMember(Order = 1)]
        public string MyPropertyB { get; set; }
        [DataMember(Order = 2)]
        public MyClass2 MyPropertyC { get; set; }
    }

    [DataContract]
    public class MyClass2
    {
        [DataMember(Order = 0)]
        public int MyProperty { get; set; }
    }
}
