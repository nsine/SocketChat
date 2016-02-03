using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    class Program
    {
        private static Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static List<Tuple<Socket, string>> clients = new List<Tuple<Socket, string>>();
        private static byte[] buffer = new byte[1024];

        static void Main(string[] args)
        {
            Console.Title = "Server";
            RunServer();
            Console.ReadLine();
        }

        private static void RunServer()
        {
            Console.WriteLine("Setting up server...");

            server.Bind(new IPEndPoint(IPAddress.Any, 100));
            server.Listen(10);

            server.BeginAccept(new AsyncCallback(AcceptCallback), null);

            Console.Clear();
            Console.WriteLine("Server is working");
        }

        private static void AcceptCallback(IAsyncResult result)
        {
            Socket socket = server.EndAccept(result);
            Console.WriteLine($"Client {socket.LocalEndPoint} connected");
            socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
            server.BeginAccept(new AsyncCallback(AcceptCallback), null);
        }

        private static void ReceiveCallback(IAsyncResult result)
        {
            Socket socket = (Socket)result.AsyncState;


            try {
                int receivedSize = socket.EndReceive(result);
                byte[] receivedData = new byte[receivedSize];
                Array.Copy(buffer, receivedData, receivedSize);

                string text = Encoding.ASCII.GetString(receivedData);
                Console.WriteLine($"Data received from {socket.LocalEndPoint}: {text}");

                string response;

                if (text.Split(' ')[0].ToLower() == "__reg") {
                    if (clients.Where(x => x.Item1 == socket).FirstOrDefault() == null) {
                        try {
                            string nickname = text.Split(' ')[1];
                            clients.Add(new Tuple<Socket, string>(socket, nickname));
                            response = "You are succesfully registered with nick " + nickname;

                            PublicMessage($"{nickname} connected to chat", "Alert");
                        } catch (IndexOutOfRangeException) {
                            response = "Use command as: __reg [nickname]";
                        }
                    } else {
                        response = "You are already registered";
                    }
                } else if (text.Split(' ')[0].ToLower() == "__exit") {
                    var user = clients.Where(x => x.Item1 == socket).FirstOrDefault();
                    if (user != null) {
                        clients.Remove(user);
                        response = "Bye, " + user.Item2;
                        PublicMessage($"{user.Item2} left chat", "Alert");
                    } else {
                        response = "You are not registered";
                    }
                } else {
                    var user = clients.Where(x => x.Item1 == socket).FirstOrDefault();
                    if (user != null) {
                        string nickname = user.Item2;
                        PublicMessage(text, nickname);
                        response = "";
                    } else {
                        response = "You should register before sending messages";
                    }
                }

                byte[] data = Encoding.ASCII.GetBytes(response);
                socket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendCallback), socket);
                socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
            } catch (SocketException) {
                var user = clients.Where(x => x.Item1 == socket).FirstOrDefault();
                if (user != null) {
                    clients.Remove(user);
                    PublicMessage($"{user.Item2} left chat", "Alert");
                }
                Console.WriteLine($"Client {user.Item1.LocalEndPoint} disconnected");
            }
        }

        private static void PublicMessage(string text, string nickname)
        {
            var response = $"{nickname}:\n\t{text}";
            byte[] data = Encoding.ASCII.GetBytes(response);

            foreach (var client in clients) {
                client.Item1.Send(data);
            }
        }

        private static void SendCallback(IAsyncResult result)
        {
            Socket socket = (Socket)result.AsyncState;
            socket.EndSend(result);
        }

        byte[] ObjectToByteArray(object obj)
        {
            if (obj == null)
                return null;
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream()) {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }


    }
}
