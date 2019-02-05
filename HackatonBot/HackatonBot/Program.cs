using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace HackatonBot
{
    class Program
    {
        static void Main(string[] args)
        {

            for (int i = 0; i < 15; i++)
            {
                new Bot();
            }


        }
    }
}
