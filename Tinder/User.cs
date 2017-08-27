using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Interface;

namespace Tinder
{
    [CallbackBehavior(UseSynchronizationContext = false)]
    public class User : IUser
    {
        public void BroadcastMessage(string message)
        {
            Console.WriteLine("Server Said: {0}", message);
        }
    }
}
