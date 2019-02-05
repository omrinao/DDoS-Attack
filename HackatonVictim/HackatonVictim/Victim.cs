using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HackatonVictim
{
    class Victim
    {
        // static randomizer
        static Random _random = new Random();

        private List<DateTime> connectionsTime;
        public TcpListener server = null;
        private string password;
        private int port;
        IPAddress localAddr;

        public Victim()
        {
            
            port = choosePort();
            localAddr = GetLocalIPAddress();

            // TcpListener server = new TcpListener(port);
            server = new TcpListener(localAddr, port);
            connectionsTime = new List<DateTime>();

            password = choosePassword();
            Console.WriteLine("Server listening on IP:" + localAddr.ToString() + ", port: " + port + ", password is " + password);

            Thread t = new Thread(StartListening);
            t.Start();
        }

        /// <summary>
        /// getting the local ip address
        /// </summary>
        /// <returns>ipadress of local host</returns>
        private IPAddress GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }


        /// <summary>
        /// method to chose a random 6 digit password
        /// </summary>
        /// <returns>6 digit password</returns>
        private string choosePassword()
        {
           
            string pass = "";
            for (int i = 0; i < 6; i++) { 
                int num = _random.Next(0, 26); // Zero to 25
                char let = (char)('a' + num);
                pass = pass + let;
            }
            return pass;
        }

        /// <summary>
        /// method to get a random open port
        /// </summary>
        /// <returns>port to be used</returns>
        private int choosePort()
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        /// <summary>
        /// method to list a connection from a bot
        /// </summary>
        /// <param name="messageFromBot"></param>
        public void manageConnections(string messageFromBot)
        {
            int numOfConnections = 0;
            DateTime now = DateTime.Now;

            connectionsTime.Add(DateTime.Now);

            List<DateTime> toRemove = new List<DateTime>();

            foreach (DateTime time in connectionsTime)
            {
                if ((now - time).TotalSeconds > 1)
                {
                    toRemove.Add(time);
                }
                else
                {
                    numOfConnections++;
                }
            }

            foreach (DateTime remove in toRemove)
            {
                connectionsTime.Remove(remove);
            }

            if (numOfConnections >= 10)
            {
                Console.WriteLine(messageFromBot);
                connectionsTime.Clear();
            }
        }


        /// <summary>
        /// method to listen to clients connecting
        /// </summary>
        private void StartListening()
        {
            server.Start();

            while (true)
                {
                    //Console.WriteLine("Waiting for a connection... ");

                    // Perform a blocking call to accept requests.
                    // You could also user server.AcceptSocket() here.
                    TcpClient client = server.AcceptTcpClient();
                    operate(client);
                }
        }


        static void Main(string[] args)
        {
            //V1
            for (int i=0; i< 1; i++)
                new Victim();
        }    

        /// <summary>
        /// method used to handle a connected client
        /// </summary>
        /// <param name="client"></param>
        public void operate(TcpClient client){
            //Console.WriteLine("Connected!");
            try {
                String data = null;

                byte[] bytes = new byte[client.ReceiveBufferSize];

                // Get a stream object for reading and writing (V2)
                NetworkStream stream = client.GetStream();
                byte[] pass = Encoding.ASCII.GetBytes("Please enter your password\r\n");
                stream.Write(pass, 0, pass.Length);

                int i;

                // Loop to receive all the data sent by the client.
                i = stream.Read(bytes, 0, bytes.Length);
                    
                // Translate data bytes to a ASCII string. (V3)
                data = Encoding.ASCII.GetString(bytes, 0, i);
                if (data.Equals(password+"\r\n"))
                {
                    byte[] access = Encoding.ASCII.GetBytes("Access granted");
                    //Console.WriteLine("Access granted");
                    stream.Write(access, 0, access.Length);
                }
                else
                {
                    connectionsTime.Clear();
                    return;
                }

                i = stream.Read(bytes, 0, bytes.Length);
                data = Encoding.ASCII.GetString(bytes, 0, i);

                manageConnections(data);//V4
                    

                    
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e);
            }

            finally
            {
                // Shutdown and end connection
                if (client != null)
                    client.Close();
            }
        }
    }
}

