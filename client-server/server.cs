using System;
using middleware;
using System.Threading;

class serverTest{

    public static int prime_test(int v){

        int flag=0,k;
        for(k=2;k<(v/2);k++){
            if(v%k==0){
                flag=1;
                break;
            }
        }
        if(v==1){return 2;}
        else{
            if(flag==0){return 0;}
            else{return 1;}
        }
    }

    static void Main(string[] args){


        //First argument is the service_id
        //Second argument is the server_ip

        if(args.Length < 2){
            Console.WriteLine("Wrong number of arguments");
            return;
        }

        int ret;
        int service_id=int.Parse(args[0]);
        int port=int.Parse(args[1]);
        string reply_message;

        server my_server = new server();
        my_server.register(service_id,port);

        while(true){
            Thread.Sleep(1000);

            Tuple<int,Guid> request= my_server.getRequest();

            if(request.Item1!=0){
                ret=prime_test(request.Item1);

                if(ret==0){reply_message= Convert.ToString(request.Item1) + " is a prime number";}
                else if(ret==1){reply_message= Convert.ToString(request.Item1) + " is not a prime number";}
                else{reply_message= Convert.ToString(request.Item1) + " is neither a prime number nor a composite number";}

                //Console.WriteLine("reply_message is {0}",reply_message);

                my_server.sendReply(reply_message,request.Item2);

            }
            else{
                Console.WriteLine("No request found");
            }

        }
    }
}
