using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Woof.ServiceEx.Wcf;

namespace Woof.Service.Tests {

    static class Program {

        public static void Main(string[] args) {

            Console.WriteLine("Starting host, press Enter to stop.");
            var host = new WebServiceHost<WebService>(new Uri("http://localhost:54321"), "http://localhost", true);
            host.Open();
            Console.ReadLine();
            host.Close();
        }

    }

}
