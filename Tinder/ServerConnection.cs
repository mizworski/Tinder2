using System;
using System.ServiceModel;
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

        public void LikePerson(int issuingId, int receivingId)
        {
            Channel.LikePerson(issuingId, receivingId);
        }

        public void SkipPerson(int issuingId, int receivingId)
        {
            Channel.SkipPerson(issuingId, receivingId);
        }

        public string FetchNewPeople(int uid)
        {
            return Channel.FetchNewPeople(uid);
        }

        public string FetchPairs(int uid)
        {
            return Channel.FetchPairs(uid);
        }

        public void SendMessage(int fromId, int toId, string content)
        {
            Channel.SendMessage(fromId, toId, content);
        }

        public string LoadMessages(int fromId, int toId)
        {
            return Channel.LoadMessages(fromId, toId);
        }

        public void Disconnect()
        {
            Channel.Disconnect();
        }
    }
}