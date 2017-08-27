using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.Caching;
using System.ServiceModel;
using System.Xml;
using Interface;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Formatting = Newtonsoft.Json.Formatting;

namespace TinderServer
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class Server : IServer
    {
        public event EventHandler<UserConnectedEventArgs> UserConnected;

        public Tuple<bool, string> Authenticate(string username, string password)
        {
            Console.WriteLine("{0} - Client called 'Authenticate'", DateTime.Now);
            OperationContext.Current.Channel.Faulted += (sender, args) =>
                Console.WriteLine("{0} - Client '{1}' connection failed.", DateTime.Now, username);
            OperationContext.Current.Channel.Closed += (sender, args) =>
                Console.WriteLine("{0} - Client '{1}' connection closed.", DateTime.Now, username);

            var connectedUser = OperationContext.Current.GetCallbackChannel<IUser>();

            UserConnected?.Invoke(this, new UserConnectedEventArgs(connectedUser));

            var connectionString = Utils.GetConnectionString();

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                var query = "SELECT * from dbo.Users " +
                            "WHERE Username='" + username + "' and PasswordHash='" + password + "'";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Connection = conn;

                    var adapter = new SqlDataAdapter {SelectCommand = cmd};
                    var results = new DataTable();
                    adapter.Fill(results);
                    string errorMessage;
                    switch (results.Rows.Count)
                    {
                        case 1:
                            var json = JsonConvert.SerializeObject(results, Formatting.None);
                            return new Tuple<bool, string>(true, json);
                        case 0:
                            errorMessage = "Invalid username or password.";
                            return new Tuple<bool, string>(false, errorMessage);
                        default:
                            errorMessage = "Ooops, we are screwed.";
                            return new Tuple<bool, string>(false, errorMessage);
                    }
                }
            }
        }


        public string[] GetFavoriteWebsites()
        {
            Console.WriteLine("{0} - Client called 'GetFavoriteWebsites'", DateTime.Now);

            return new string[]
            {
                "www.google.com",
                "www.wp.pl",
                "www.onet.pl"
            };
        }

        public void Disconnect()
        {
            Console.WriteLine("{0} - Client called 'Disconnect'", DateTime.Now);
        }
    }
}