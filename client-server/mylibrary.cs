using System;

namespace MyLibrary{

    public class node{
        private Guid client_private_id;
        private int seq;

        public node(int seq_num,Guid id){
            seq=seq_num;
            client_private_id=id;
        }

        public int getSeqNum(){
            return seq;
        }

        public void setSeqNum(int new_seq_num){
            seq=new_seq_num;
        }

        public Guid getPrivateID(){
            return client_private_id;
        }
    }

    [Serializable]
    public class message{
        private int seq;
        private int number;
        private string reply_ip;
        private int reply_port;
        private Guid client_private_id;
        private Guid unique_id;

        public message(int sequence,int num,string ip, int port, Guid id){
            seq=sequence;
            number=num;
            reply_ip=ip;
            reply_port=port;
            client_private_id=id;
        }

        public Guid getUniqueID(){
            return unique_id;
        }

        public void setUniqueID(Guid id){
            unique_id=id;
        }

        public int getNumber(){
            return number;
        }

        public int getSeqNum(){
            return seq;
        }

        public string getReplyIP(){
            return reply_ip;
        }

        public int getReplyPort(){
            return reply_port;
        }

        public Guid getPrivateID(){
            return client_private_id;
        }
    }

    [Serializable]
    public class reply_message{
        private int seq;
        private string message;
        private Guid unique_id;
        private Guid server_id;

        public reply_message(int sequence,string reply,Guid id){
            seq=sequence;
            message=reply;
            server_id=id;
        }

        public Guid getPrivateID(){
            return server_id;
        }

        public Guid getUniqueID(){
            return unique_id;
        }

        public void setUniqueID(Guid id){
            unique_id=id;
        }

        public string getReply(){
            return message;
        }

        public int getSeqNum(){
            return seq;
        }
    }


    [Serializable]
    public class discovery_message{
        private int service_id;

        public discovery_message(int svcid){
            service_id=svcid;
        }

        public int getServiceID(){
            return service_id;
        }
    }

    [Serializable]
    public class ack_message{
        private int seq_num;

        public ack_message(int seq){
            seq_num=seq;
        }

        public int getSeqNum(){
            return seq_num;
        }
    }

    [Serializable]
    public class discovery_reply_message{
        private int service_id;
        private int capacity;
        private string ip;
        private int port;

        public discovery_reply_message(int svcid,int cap,string server_ip,int server_port){
            service_id=svcid;
            capacity=cap;
            ip=server_ip;
            port=server_port;
        }

        public int getServiceID(){
            return service_id;
        }

        public int getCapacity(){
            return capacity;
        }

        public string getServerIP(){
            return ip;
        }

        public int getServerPort(){
            return port;
        }

    }

}
