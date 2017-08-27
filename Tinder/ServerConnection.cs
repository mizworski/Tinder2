using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Interface;

namespace Tinder
{
    public class ServerConnection : DuplexClientBase<IServer>, IServer
    {
        public ServerConnection(IUser callbackInstance) : base(callbackInstance)
        {
            //
        }

        public Tuple<bool, string> Authenticate(string userName, string password)
        {
            return Channel.Authenticate(userName, password);
        }

        public string[] GetFavoriteWebsites()
        {
            return Channel.GetFavoriteWebsites();
        }

        public void Disconnect()
        {
            Channel.Disconnect();
        }
    }
}