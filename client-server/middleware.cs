using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.Collections.Generic;
using MyLibrary;

namespace middleware{

    class client{

        private string my_reply_ip;
        private int seq_num;
        private int my_reply_port;
        private int my_service_id;
        private int server_port;
        private int multicast_port;
        private string multicast_ip;
        private Guid my_private_id;
        private static int MAX_RETRANSMISSIONS=5;
        private List<node> servers_list;
        private List<reply_message> reply_message_list;
        private static object lockObject;


        public client(int service_id,int server_port_num,int client_reply_port){
            string hostName = Dns.GetHostName();
            my_reply_ip = Dns.GetHostByName(hostName).AddressList[0].ToString();

            my_service_id=service_id;
            my_reply_port=client_reply_port;

            Console.WriteLine("Client IP Address is {0}:{1} ", my_reply_ip,my_reply_port);

            server_port=server_port_num;
            multicast_port=10000;
            multicast_ip="224.0.0.1";
            seq_num=0;
            my_private_id = Guid.NewGuid();
            servers_list = new List<node>();
            reply_message_list = new List<reply_message>();
            lockObject = new object();

            ThreadStart thread1_ref = new ThreadStart(replyReceiver);
            Thread receiver_thread = new Thread(thread1_ref);
            receiver_thread.Start();

        }

        public byte[] SerializeObject(object obj){
            var stream = new System.IO.MemoryStream();
            var formatter = new BinaryFormatter();
            formatter.Serialize(stream, obj);
            return stream.ToArray();
        }

        private T DeserializeObject<T>(byte[] buffer){
            var stream = new System.IO.MemoryStream(buffer);
            var formatter = new BinaryFormatter();
            return (T)formatter.Deserialize(stream);
        }

        public int sendRequest(int number){

            int exit_flag=0,min_capacity=1000000,server_port=0,available_server=0,i;
            string server_ip="";
            discovery_message multicast_msg = new discovery_message(my_service_id);
            byte[] dataToSend = SerializeObject(multicast_msg),receivedData;
            Task<UdpReceiveResult> receiveTask;
            Task timeoutTask = Task.Delay(500); // 500 milliseconds (0.5 seconds) timeout
            // Set up the UDP client
            UdpClient udpClient = new UdpClient();
            udpClient.Send(dataToSend, dataToSend.Length, new IPEndPoint(IPAddress.Parse(multicast_ip), multicast_port));

            while(true){
                receiveTask = udpClient.ReceiveAsync();
                timeoutTask = Task.Delay(500);

                Task.WhenAny(receiveTask, timeoutTask).ContinueWith(task =>{
                    if (task.Result == receiveTask){
                        // Data received before timeout
                        receivedData = receiveTask.Result.Buffer;
                        // Deserialize the received data back to the object
                        discovery_reply_message receivedObject = DeserializeObject<discovery_reply_message>(receivedData);
                        //Console.WriteLine("Received discovery_reply_message from server with IP {0}, port {1}, and capacity is {2}.", receivedObject.getServerIP(), receivedObject.getServerPort(),receivedObject.getCapacity());

                        if(receivedObject.getCapacity() < min_capacity){
                            min_capacity=receivedObject.getCapacity();
                            server_ip=receivedObject.getServerIP();
                            server_port=receivedObject.getServerPort();
                            available_server=1;
                        }
                    }
                    else{
                        exit_flag=1;
                    }
                }).Wait();

                if(exit_flag==1){break;}

            }
            udpClient.Close();
            if(available_server==0){
                Console.WriteLine("No available server, exiting...");
                return(-1);
            }

            udpClient = new UdpClient();
            seq_num++;
            message new_msg = new message(seq_num,number,my_reply_ip,my_reply_port,my_private_id);
            // Serialize the object to a byte array
            dataToSend = SerializeObject(new_msg);

            exit_flag=0;
            timeoutTask = Task.Delay(5000); // 500 milliseconds (0.5 seconds) timeout

            for(i=0;i<MAX_RETRANSMISSIONS;i++){
                // Send the data to the receiver
                udpClient.Send(dataToSend, dataToSend.Length, new IPEndPoint(IPAddress.Parse(server_ip), server_port));

                // Wait for data to arrive with a timeout
                receiveTask = udpClient.ReceiveAsync();
                timeoutTask = Task.Delay(5000);

                Task.WhenAny(receiveTask, timeoutTask).ContinueWith(task =>{
                    if (task.Result == receiveTask){
                        // Data received before timeout
                        receivedData = receiveTask.Result.Buffer;

                        // Deserialize the received data back to the object
                        ack_message receivedObject  = DeserializeObject<ack_message>(receivedData);
                        //Console.WriteLine("Received ack for sequence number {0}",receivedObject.getSeqNum());

                        if(receivedObject.getSeqNum()==seq_num){
                            exit_flag=1;
                        }
                    }
                    else{
                        // Timeout occurred, no data received
                        Console.WriteLine("No reply received. Timeout occurred.");
                    }
                }).Wait();

                if(exit_flag==1){break;}
            }

            return -1;
        }

        public void replyReceiver(){

            int i,served_sequence=0;
             // Set up the UDP server

            UdpClient udpServer = new UdpClient(my_reply_port); // Replace with the server's port number
            // Listen for incoming data
            IPEndPoint senderEndPoint = new IPEndPoint(IPAddress.Any,0);

            while(true){
                byte[] receivedData = udpServer.Receive(ref senderEndPoint);

                // Deserialize the received data back to the object
                reply_message receivedObject  = DeserializeObject<reply_message>(receivedData);
                //Console.WriteLine("Received message: {0} from server with id {1}", receivedObject.getReply(),receivedObject.getPrivateID());

                for(i=0;i<servers_list.Count;i++){
                    if(servers_list[i].getPrivateID()==receivedObject.getPrivateID()){
                        served_sequence=servers_list[i].getSeqNum();
                        //Console.WriteLine("Have previous messages from the same server, served_sequence is {0}", served_sequence);
                        break;
                    }
                }

                if(i==servers_list.Count){
                    node new_node= new node(receivedObject.getSeqNum()-1,receivedObject.getPrivateID());
                    servers_list.Add(new_node);
                    Console.WriteLine("New server,add his server_id to the server list");

                    served_sequence=receivedObject.getSeqNum()-1;
                }


                if(receivedObject.getSeqNum()>served_sequence){
                    for(i=0;i<servers_list.Count;i++){
                        if(servers_list[i].getPrivateID()==receivedObject.getPrivateID()){
                            servers_list[i].setSeqNum(receivedObject.getSeqNum());
                            break;
                        }
                    }
                }

                lock (lockObject){
                    receivedObject.setUniqueID(Guid.NewGuid());
                    reply_message_list.Add(receivedObject);
                }
                ack_message ack = new ack_message(receivedObject.getSeqNum());
                byte[] replyData = SerializeObject(ack);
                udpServer.Send(replyData, replyData.Length, senderEndPoint);
            }
        }

        public Tuple<int,string> getReply(){

            lock (lockObject){

                if(reply_message_list.Count==0){
                    return Tuple.Create(-1,"");
                }
                string reply_string=reply_message_list[0].getReply();
                reply_message_list.Remove(reply_message_list[0]);
                return Tuple.Create(1,reply_string);
            }

        }
    }


    class server{

        private string my_ip;
        private int my_port;
        private int my_service_id;
        private int multicast_port;
        private string multicast_ip;
        private Guid my_private_id;
        private List<node> client_list;
        private List<message> message_list;
        private static object lockObject;
        private int reply_seq_num;
        private static int MAX_RETRANSMISSIONS;

        public server(){
            string hostName = Dns.GetHostName();
            my_ip = Dns.GetHostByName(hostName).AddressList[0].ToString();
            Console.WriteLine("IP Address is : " + my_ip);
            multicast_port=10000;
            multicast_ip="224.0.0.1";
            my_private_id = Guid.NewGuid();
            client_list = new List<node>();
            message_list = new List<message>();
            lockObject = new object();
            reply_seq_num=0;
            MAX_RETRANSMISSIONS=5;
        }

        public byte[] SerializeObject(object obj){
            var stream = new System.IO.MemoryStream();
            var formatter = new BinaryFormatter();
            formatter.Serialize(stream, obj);
            return stream.ToArray();
        }

        private T DeserializeObject<T>(byte[] buffer)
        {
            using (var stream = new System.IO.MemoryStream(buffer)){
                var formatter = new BinaryFormatter();
                return (T)formatter.Deserialize(stream);
            }
        }

        public void receiver(){

            int i,served_sequence=0;
             // Set up the UDP server
            UdpClient udpServer = new UdpClient(my_port); // Replace with the server's port number

            // Listen for incoming data
            IPEndPoint senderEndPoint = new IPEndPoint(IPAddress.Any,0);

            while(true){
                byte[] receivedData = udpServer.Receive(ref senderEndPoint);

                // Deserialize the received data back to the object
                message receivedObject  = DeserializeObject<message>(receivedData);
                //Console.WriteLine("Received number {0} from client with reply_ip {1}, reply_port {2} and id {3}", receivedObject.getNumber(),receivedObject.getReplyIP(),receivedObject.getReplyPort(),receivedObject.getPrivateID());

                for(i=0;i<client_list.Count;i++){
                    if(client_list[i].getPrivateID()==receivedObject.getPrivateID()){
                        served_sequence=client_list[i].getSeqNum();
                        //Console.WriteLine("Have previous messages from the same client, served_sequence is {0}", served_sequence);
                        break;
                    }
                }

                if(i==client_list.Count){
                    node new_node= new node(receivedObject.getSeqNum()-1,receivedObject.getPrivateID());
                    client_list.Add(new_node);
                    Console.WriteLine("New client,add his client_id to the sequence_number_list");

                    served_sequence=receivedObject.getSeqNum()-1;
                }


                if(receivedObject.getSeqNum()>served_sequence){
                    for(i=0;i<client_list.Count;i++){
                        if(client_list[i].getPrivateID()==receivedObject.getPrivateID()){
                            client_list[i].setSeqNum(receivedObject.getSeqNum());
                            break;
                        }
                    }
                }

                lock (lockObject){
                    receivedObject.setUniqueID(Guid.NewGuid());
                    message_list.Add(receivedObject);
                }
                ack_message ack = new ack_message(receivedObject.getSeqNum());
                byte[] replyData = SerializeObject(ack);
                udpServer.Send(replyData, replyData.Length, senderEndPoint);
            }
        }

        public void sendReply(string reply_message,Guid id){
            int i,reply_port=0,exit_flag=0;
            string reply_ip="";

            byte[] dataToSend,receivedData;
            Task<UdpReceiveResult> receiveTask;
            Task timeoutTask = Task.Delay(5000); // 500 milliseconds (0.5 seconds) timeout
            // Set up the UDP client
            UdpClient udpClient = new UdpClient();

            lock (lockObject){

                for(i=0;i<message_list.Count;i++){
                    if(message_list[i].getUniqueID()==id){
                        reply_ip=message_list[i].getReplyIP();
                        reply_port=message_list[i].getReplyPort();
                        message_list.Remove(message_list[i]);
                        break;
                    }
                }
            }


            reply_seq_num++;
            reply_message new_msg = new reply_message(reply_seq_num,reply_message,my_private_id);
            // Serialize the object to a byte array
            dataToSend = SerializeObject(new_msg);

            //Console.WriteLine("Reply to client with ip {0} and port {1}",reply_ip,reply_port);


            for(i=0;i<MAX_RETRANSMISSIONS;i++){
                // Send the data to the receiver
                udpClient.Send(dataToSend, dataToSend.Length, new IPEndPoint(IPAddress.Parse(reply_ip), reply_port));

                // Wait for data to arrive with a timeout
                receiveTask = udpClient.ReceiveAsync();
                timeoutTask = Task.Delay(5000);

                Task.WhenAny(receiveTask, timeoutTask).ContinueWith(task =>{
                    if (task.Result == receiveTask){
                        // Data received before timeout
                        receivedData = receiveTask.Result.Buffer;

                        // Deserialize the received data back to the object
                        ack_message receivedObject  = DeserializeObject<ack_message>(receivedData);
                        //Console.WriteLine("Received ack for sequence number {0}",receivedObject.getSeqNum());

                        if(receivedObject.getSeqNum()==reply_seq_num){
                            exit_flag=1;
                        }
                    }
                    else{
                        // Timeout occurred, no data received
                        Console.WriteLine("No reply received. Timeout occurred.");
                    }
                }).Wait();

                if(exit_flag==1){break;}
            }



        }


        public void multicast_receiver(){

            IPAddress multicastGroup = IPAddress.Parse(multicast_ip);
            int multicastPort = multicast_port;

            // Set up the UDP server for multicast
            UdpClient udpServer = new UdpClient();
            udpServer.ExclusiveAddressUse = false; // Allow multiple sockets to bind to the same port
            udpServer.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true); // Reuse the address
            udpServer.Client.Bind(new IPEndPoint(IPAddress.Any, multicastPort)); // Bind to any available address and the multicast port

            // Join the multicast group
            udpServer.JoinMulticastGroup(multicastGroup);

            // Listen for incoming data
            IPEndPoint senderEndPoint = new IPEndPoint(IPAddress.Any,0);

            while(true){
                byte[] receivedData = udpServer.Receive(ref senderEndPoint);

                // Deserialize the received data back to the object
                discovery_message receivedObject  = DeserializeObject<discovery_message>(receivedData);

                if(receivedObject.getServiceID()==my_service_id){
                    lock (lockObject){
                        discovery_reply_message discovery_reply = new discovery_reply_message(my_service_id,message_list.Count,my_ip,my_port);
                        byte[] replyData = SerializeObject(discovery_reply);
                        udpServer.Send(replyData, replyData.Length, senderEndPoint);
                    }
                }
            }
        }

        public Tuple<int, Guid> getRequest(){

            lock (lockObject){

                //Console.WriteLine("message_list len is {0}",message_list.Count);
                if(message_list.Count==0){return  Tuple.Create(0,Guid.NewGuid());}
                return  Tuple.Create(message_list[0].getNumber(),message_list[0].getUniqueID());

            }
        }


        public void register(int service_id, int port){

            my_port=port;
            my_service_id=service_id;

            ThreadStart thread1_ref = new ThreadStart(receiver);
            Thread receiver_thread = new Thread(thread1_ref);
            receiver_thread.Start();

            ThreadStart thread2_ref = new ThreadStart(multicast_receiver);
            Thread multicast_receiver_thread = new Thread(thread2_ref);
            multicast_receiver_thread.Start();
        }
    }
}
