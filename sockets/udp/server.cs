using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using MyLibrary;

class Program
{
    static void Main()
    {
        // Set up the UDP server
        UdpClient udpServer = new UdpClient(8000); // Replace with the server's port number

        // Listen for incoming data
        IPEndPoint senderEndPoint = new IPEndPoint(IPAddress.Parse("192.168.1.232"),8000);
        byte[] receivedData = udpServer.Receive(ref senderEndPoint);

        // Deserialize the received data back to the object
        MyClass receivedObject = DeserializeObject<MyClass>(receivedData);

        // Process the received object
        Console.WriteLine("Received Object:");
        Console.WriteLine("Number: " + receivedObject.Number);
        Console.WriteLine("Text: " + receivedObject.Text);
    }

    // Deserialize a byte array to an object
    static T DeserializeObject<T>(byte[] buffer)
    {
        using (var stream = new System.IO.MemoryStream(buffer))
        {
            var formatter = new BinaryFormatter();
            return (T)formatter.Deserialize(stream);
        }
    }
}
