using System;
using System.ServiceModel;
using Interface;

namespace Tinder
{
    [CallbackBehavior(UseSynchronizationContext = false)]
    public class ChatUser : IUser
    {
        public void LoadMessages()
        {
            Console.WriteLine(@"Server Said");
        }
    }
}
