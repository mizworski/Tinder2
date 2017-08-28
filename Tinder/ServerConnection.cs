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

        public Tuple<bool, string> Authenticate(string username, string password)
        {
            return Channel.Authenticate(username, password);
        }

        public Tuple<bool, string> Signup(string username, string firstname, string password, char gender)
        {
            return Channel.Signup(username, firstname, password, gender);
        }

        public Tuple<bool, string, string> GetProfileInfo(int uid)
        {
            return Channel.GetProfileInfo(uid);
        }

        public void UpdateProfile(int uid, string bio, bool interestedInFemales, bool interestedInMales)
        {
            Channel.UpdateProfile(uid, bio, interestedInFemales, interestedInMales);
        }

        public void UpdatePicture(int uid, string imageSerialized)
        {
            Channel.UpdatePicture(uid, imageSerialized);
        }

        public void Disconnect()
        {
            Channel.Disconnect();
        }
    }
}