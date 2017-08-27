using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Interface;

namespace TinderServer
{
    public class UserConnectedEventArgs : EventArgs
    {
        public UserConnectedEventArgs(IUser connectedUser)
        {
            ConnectedUser = connectedUser;
        }

        public IUser ConnectedUser { get; set; }
    }
}
