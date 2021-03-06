﻿using System;
using System.ServiceModel;

namespace TinderServer
{
    internal class Program
    {
        private static readonly Server ServiceInstance = new Server();

        private static void Main()
        {
            Console.WriteLine("Tinder server is now running.");

            ServiceInstance.UserConnected += serviceInstance_UserSentMessage;

            Console.Write("Starting WCF listener...");

            using (var host = new ServiceHost(ServiceInstance))
            {
                host.Open();
                Console.WriteLine(" started.\n");
                Console.WriteLine("Press [ENTER] to quit.\n");
                Console.ReadLine();
            }
        }

        private static void serviceInstance_UserSentMessage(object sender, UserConnectedEventArgs e)
        {
            e.ConnectedUser.LoadMessages();
        }
    }
}
