using System;
using System.Data;
using System.ServiceModel;

namespace Interface
{
    [ServiceContract(CallbackContract = typeof(IUser), SessionMode = SessionMode.Required)]
    public interface IServer
    {
//        [CallbackBehavior(ConcurrencyMode = ConcurrencyModel.Reentrant)]
        [OperationContract(IsInitiating = true)]
        Tuple<bool, string> Authenticate(string userName, string password);

        [OperationContract(IsInitiating = false)]
        string[] GetFavoriteWebsites();

        [OperationContract(IsInitiating = false, IsTerminating = true, IsOneWay = true)]
        void Disconnect();
    }
}


