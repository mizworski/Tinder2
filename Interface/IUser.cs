﻿using System.ServiceModel;


namespace Interface
{
    [ServiceContract]
    public interface IUser
    {
        [OperationContract(IsOneWay = true)]
        void LoadMessages();
    }
}
