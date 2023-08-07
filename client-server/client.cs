using System;
using middleware;
using System.Threading;
using System.IO;

class clientTest{

    static void Main(string[] args){
        //First argument is the service_id
        //Second argument is the server_port
        //Third argument is the client_reply_port

        if(args.Length < 3){
            Console.WriteLine("Wrong number of arguments");
            return;
        }

        int i,j;
        int service_id=int.Parse(args[0]);
        int server_port=int.Parse(args[1]);
        int client_reply_port=int.Parse(args[2]);
        string line;

        client my_client = new client(service_id,server_port,client_reply_port);
        StreamReader reader;
        try{
            reader = new StreamReader("primes.txt");

            for(i=0;i<1000;i++){
                for(j=0;j<5;j++){
                    line = reader.ReadLine();
                    if(line==null){return;}

                    int.TryParse(line, out int number);
                    my_client.sendRequest(number);

                }
                Thread.Sleep(1000);
                for(j=0;j<5;j++){
                    //Wait for replies
                    Tuple<int,string> reply= my_client.getReply();
                    if(reply.Item1==-1){
                        //Console.WriteLine("No replies found");
                    }
                    else{
                        Console.WriteLine("{0}",reply.Item2);
                    }
                }
            }
        }
        catch (FileNotFoundException){
            Console.WriteLine("File not found");
        }
        catch (Exception ex){
            Console.WriteLine("Error reading the file: " + ex.Message);
        }
    }
}
