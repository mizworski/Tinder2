using System;
using System.ServiceModel;


namespace Interface
{
    public interface IUser
    {
        [OperationContract(IsOneWay = true)]
        void BroadcastMessage(string message);
    }
}
