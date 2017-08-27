using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace TinderServer
{
    class Program
    {
        static Server serviceInstance = new Server();

        static void Main()
        {
            Console.WriteLine("WCF Server");

            serviceInstance.UserConnected += new EventHandler<UserConnectedEventArgs>(serviceInstance_UserConnected);

            Console.Write("Starting WCF listener...");

            using (ServiceHost host = new ServiceHost(serviceInstance))
            {
                host.Open();
                Console.WriteLine(" started.\n");
                Console.WriteLine("Press [ENTER] to quit.\n");
                Console.ReadLine();
            }
        }

        static void serviceInstance_UserConnected(object sender, UserConnectedEventArgs e)
        {
            e.ConnectedUser.BroadcastMessage("Hello!!!");
        }
    }
}
