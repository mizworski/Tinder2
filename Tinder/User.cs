using System;
using System.ServiceModel;
using Interface;

namespace Tinder
{
    [CallbackBehavior(UseSynchronizationContext = false)]
    public class User : IUser
    {
        public void BroadcastMessage(string message)
        {
//            Console.WriteLine(@"Server Said: {0}", message); XD
        }
    }
}
