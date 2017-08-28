using System;
using System.Data;
using System.ServiceModel;

namespace Interface
{
    [ServiceContract(CallbackContract = typeof(IUser), SessionMode = SessionMode.Required)]
    public interface IServer
    {
        [OperationContract(IsInitiating = true, IsTerminating = true)]
        Tuple<bool, string> Authenticate(string username, string password);

        [OperationContract(IsInitiating = true, IsTerminating = true)]
        Tuple<bool, string> Signup(string username, string firstname, string password, char gender);

        [OperationContract(IsInitiating = true, IsTerminating = true)]
        Tuple<bool, string, string> GetProfileInfo(int uid);
        
        [OperationContract(IsInitiating = true, IsTerminating = true, IsOneWay = true)]
        void UpdateProfile(int uid, string bio, bool interestedInFemales, bool interestedInMales);

        [OperationContract(IsInitiating = true, IsTerminating = true, IsOneWay = true)]
        void UpdatePicture(int uid, string imageSerialized);
        
        [OperationContract(IsInitiating = false, IsTerminating = true, IsOneWay = true)]
        void Disconnect();
    }
}


