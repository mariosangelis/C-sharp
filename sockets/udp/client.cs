using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using MyLibrary;

class Program
{
    static void Main()
    {

        // Create an object of MyClass
        MyClass myObject = new MyClass { Number = 42, Text = "Hello, World!" };

        // Serialize the object to a byte buffer
        byte[] buffer = SerializeObject(myObject);

        UdpClient udpClient = new UdpClient();

        // Specify the IP address and port of the receiver
        IPAddress receiverIP = IPAddress.Parse("192.168.1.232"); // Replace with the receiver's IP address
        int receiverPort = 8000; // Replace with the receiver's port number

        // Send the data to the receiver
        udpClient.Send(buffer, buffer.Length, new IPEndPoint(receiverIP, receiverPort));

        Console.WriteLine("Object sent over UDP.,objectsize="+buffer.Length);

        // Deserialize the byte buffer back to an object
        //MyClass deserializedObject = DeserializeObject<MyClass>(buffer);

        // Print the deserialized object
        //Console.WriteLine("Deserialized Object:");
        //Console.WriteLine("Number: " + deserializedObject.Number);
        //Console.WriteLine("Text: " + deserializedObject.Text);
    }

    // Serialize an object to a byte buffer
    static byte[] SerializeObject<T>(T obj)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, obj);
            return stream.ToArray();
        }
    }

    // Deserialize a byte buffer to an object
    static T DeserializeObject<T>(byte[] buffer)
    {
        using (MemoryStream stream = new MemoryStream(buffer))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            return (T)formatter.Deserialize(stream);
        }
    }
}
