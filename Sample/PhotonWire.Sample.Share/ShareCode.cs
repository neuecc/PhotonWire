using System;

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

    public class MyClass
    {
        public int MyPropertyA { get; set; }
        public string MyPropertyB { get; set; }
        public MyClass2 MyPropertyC { get; set; }
    }

    public class MyClass2
    {
        public int MyProperty { get; set; }
    }
}
