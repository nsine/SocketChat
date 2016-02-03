using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    class Program
    {
        private static Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static byte[] buffer = new byte[1024];

        static void Main(string[] args)
        {
            Console.Title = "Client";

            ConnectToServer();
            SendLoop();
        }

        private static void ConnectToServer()
        {
            IPAddress ip;
            int port;

            while (true) {
                while (true) {
                    Console.WriteLine("Enter [IP]:[Port]:");
                    string input = Console.ReadLine();

                    try {
                        string[] inputArray = input.Split(':');
                        ip = IPAddress.Parse(inputArray[0]);
                        port = int.Parse(inputArray[1]);
                        break;
                    } catch {
                        Console.WriteLine("Incorrect input. Try again");
                    }
                }

                bool isConnected = LoopConnect(ip, port);
                if (isConnected) {
                    break;
                }
            }
        }

        private static bool LoopConnect(IPAddress ip, int port)
        {
            int attempts = 0;

            while (!client.Connected) {
                try {
                    attempts++;
                    client.Connect(ip, port);
                } catch (SocketException) {
                    Console.Clear();
                    Console.WriteLine("Connection attempts: " + attempts);
                    if (attempts > 5) {
                        Console.Clear();
                        Console.WriteLine("Connection failed");
                        return false;
                    }
                }
            }

            Console.Clear();
            Console.WriteLine("Connected to server");
            Console.WriteLine("Welcome to chat");
            Console.WriteLine("Enter __reg [nickname] to register");
            Console.WriteLine("Enter __exit to logout");
            return true;
        }

        private static void SendLoop()
        {
            while (true) {
                string request = Console.ReadLine();
                byte[] data = Encoding.ASCII.GetBytes(request);
                client.Send(data);

                client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), client);
                
            }
        }

        private static void SubscribeLoop()
        {
            client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), client);
        }

        private static void ReceiveCallback(IAsyncResult result)
        {
            Socket socket = (Socket)result.AsyncState;

            int receivedSize = socket.EndReceive(result);
            byte[] data = new byte[receivedSize];
            Array.Copy(buffer, data, receivedSize);
            Console.WriteLine(Encoding.ASCII.GetString(data));
            client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), client);
        }
    }
}
