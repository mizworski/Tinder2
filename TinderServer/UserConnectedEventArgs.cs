using System;
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
