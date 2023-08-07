using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;

namespace MyLibrary{

    [Serializable] // Marking the class as serializable
    public class MyClass
    {
        public int Number { get; set; }
        public string Text { get; set; }
    }
}
