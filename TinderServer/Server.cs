using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.ServiceModel;
using System.Text.RegularExpressions;
using Interface;
using Newtonsoft.Json;
using Formatting = Newtonsoft.Json.Formatting;

namespace TinderServer
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class Server : IServer
    {
        public event EventHandler<UserConnectedEventArgs> UserConnected;
        public Dictionary<int, IUser> ConnectedUsers;

        public Server()
        {
            ConnectedUsers = new Dictionary<int, IUser>();
        }
        public Tuple<bool, string> Authenticate(string username, string password)
        {
            Console.WriteLine("{0} - Client called 'Authenticate'", DateTime.Now);
            OperationContext.Current.Channel.Faulted += (sender, args) =>
                Console.WriteLine("{0} - Client '{1}' connection failed.", DateTime.Now, username);
            OperationContext.Current.Channel.Closed += (sender, args) =>
                Console.WriteLine("{0} - Client '{1}' connection closed.", DateTime.Now, username);
            

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

        public Tuple<bool, string> Signup(string username, string firstname, string password, char gender)
        {
            Console.WriteLine("{0} - Client called 'Signup'", DateTime.Now);
            OperationContext.Current.Channel.Faulted += (sender, args) =>
                Console.WriteLine("{0} - Client '{1}' connection failed.", DateTime.Now, username);
            OperationContext.Current.Channel.Closed += (sender, args) =>
                Console.WriteLine("{0} - Client '{1}' connection closed.", DateTime.Now, username);

            string errorMessage;

            if (username.Length == 0)
            {
                errorMessage = "Enter username.";
            }
            else if (firstname.Length == 0)
            {
                errorMessage = "Enter first name.";
            }
            else if (!Regex.IsMatch(firstname, @"^[A-Z][a-z][\w\.-]*"))
            {
                errorMessage =
                    "First name should start with capital letter and then contain only lowercase letters.";
            }
            else if (password.Length == 0)
            {
                errorMessage = "Enter password.";
            }
            else if (gender != 'M' && gender != 'F')
            {
                errorMessage = "Select your gender.";
            }
            else
            {
                errorMessage = "";
                var connectionString = Utils.GetConnectionString();

                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    var checkIfUserExistsQuery =
                        "SELECT * from dbo.Users WHERE Username='" + username + "'";
                    using (var checkIfUserExists = new SqlCommand(checkIfUserExistsQuery, conn))
                    {
                        checkIfUserExists.CommandType = CommandType.Text;
                        checkIfUserExists.Connection = conn;
                        var adapter = new SqlDataAdapter {SelectCommand = checkIfUserExists};
                        var dataSet = new DataSet();
                        adapter.Fill(dataSet);
                        if (dataSet.Tables[0].Rows.Count > 0)
                        {
                            errorMessage = "User with this username already exists. Choose new username.";
                        }
                        else
                        {
                            const string addUserQuery =
                                "INSERT INTO dbo.Users (Username, FirstName, PasswordHash, Gender, InterestedInMales, InterestedInFemales) " +
                                "VALUES (@Username, @FirstName, @PasswordHash, @Gender, 0, 0);";
                            using (var addUser = new SqlCommand(addUserQuery, conn))
                            {
                                addUser.CommandType = CommandType.Text;
                                addUser.Connection = conn;

                                addUser.Parameters.AddWithValue("@Username", username);
                                addUser.Parameters.AddWithValue("@FirstName", firstname);
                                addUser.Parameters.AddWithValue("@PasswordHash", password);
                                addUser.Parameters.AddWithValue("@Gender", gender);

                                addUser.ExecuteNonQuery();

                                return new Tuple<bool, string>(true, errorMessage);
                            }
                        }
                    }
                }
            }

            return new Tuple<bool, string>(false, errorMessage);
        }

        public Tuple<bool, string, string> GetProfileInfo(int uid)
        {
            Console.WriteLine("{0} - Client called 'GetProfileInfo'", DateTime.Now);
            OperationContext.Current.Channel.Faulted += (sender, args) =>
                Console.WriteLine("{0} - Client '{1}' connection failed.", DateTime.Now, uid);
            OperationContext.Current.Channel.Closed += (sender, args) =>
                Console.WriteLine("{0} - Client '{1}' connection closed.", DateTime.Now, uid);

            var connectionString = Utils.GetConnectionString();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                var query = "SELECT * from dbo.Users WHERE Id='" + uid + "'";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Connection = conn;

                    var adapter = new SqlDataAdapter {SelectCommand = cmd};
                    var results = new DataTable();
                    adapter.Fill(results);
                    if (results.Rows.Count != 1)
                    {
                        return new Tuple<bool, string, string>(false, "", "");
                    }

                    string bytesAsString;
                    if (results.Rows[0]["ProfilePicture"].Equals(DBNull.Value))
                    {
                        bytesAsString = "";
                    }
                    else
                    {
                        var imageBytes = (byte[]) results.Rows[0]["ProfilePicture"];
                        bytesAsString = Serializer.SerializeObject(imageBytes);
                    }

                    var json = JsonConvert.SerializeObject(results, Formatting.None);
                    return new Tuple<bool, string, string>(true, json, bytesAsString);
                }
            }
        }

        public void UpdateProfile(int uid, string bio, bool interestedInFemales, bool interestedInMales)
        {
            Console.WriteLine("{0} - Client called 'UpdateProfile'", DateTime.Now);
            OperationContext.Current.Channel.Faulted += (sender, args) =>
                Console.WriteLine("{0} - Client '{1}' connection failed.", DateTime.Now, uid);
            OperationContext.Current.Channel.Closed += (sender, args) =>
                Console.WriteLine("{0} - Client '{1}' connection closed.", DateTime.Now, uid);

            var connectionString = Utils.GetConnectionString();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                const string updateProfileQuery = "UPDATE dbo.Users " +
                                                  "SET Bio = @Bio, InterestedInMales = @InterestedInMales, InterestedInFemales = @InterestedInFemales " +
                                                  "WHERE Id = @Id;";
                using (var updateProfile = new SqlCommand(updateProfileQuery, conn))
                {
                    updateProfile.CommandType = CommandType.Text;
                    updateProfile.Connection = conn;

                    updateProfile.Parameters.AddWithValue("@Bio", bio);
                    updateProfile.Parameters.AddWithValue("@InterestedInFemales", interestedInFemales);
                    updateProfile.Parameters.AddWithValue("@InterestedInMales", interestedInMales);
                    updateProfile.Parameters.AddWithValue("@Id", uid);

                    updateProfile.ExecuteNonQuery();
                }
            }
        }

        public void UpdatePicture(int uid, string imageSerialized)
        {
            Console.WriteLine("{0} - Client called 'UpdatePicture'", DateTime.Now);
            OperationContext.Current.Channel.Faulted += (sender, args) =>
                Console.WriteLine("{0} - Client '{1}' connection failed.", DateTime.Now, uid);
            OperationContext.Current.Channel.Closed += (sender, args) =>
                Console.WriteLine("{0} - Client '{1}' connection closed.", DateTime.Now, uid);
           

            var connectionString = Utils.GetConnectionString();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var imageBytes = (byte[]) Serializer.DeserializeObject(imageSerialized);

                const string uploadPictureQuery = "UPDATE dbo.Users " +
                                                  "SET ProfilePicture = @ProfilePicture " +
                                                  "WHERE Id = @Id;";

                using (var uploadPicture = new SqlCommand(uploadPictureQuery, conn))
                {
                    uploadPicture.CommandType = CommandType.Text;
                    uploadPicture.Connection = conn;

                    uploadPicture.Parameters.AddWithValue("@ProfilePicture", imageBytes);
                    uploadPicture.Parameters.AddWithValue("@Id", uid);

                    uploadPicture.ExecuteNonQuery();
                }
            }
        }

        public void LikePerson(int issuingId, int receivingId)
        {
            Console.WriteLine("{0} - Client called 'LikePerson'", DateTime.Now);
            OperationContext.Current.Channel.Faulted += (sender, args) =>
                Console.WriteLine("{0} - Client '{1}' connection failed.", DateTime.Now, issuingId);
            OperationContext.Current.Channel.Closed += (sender, args) =>
                Console.WriteLine("{0} - Client '{1}' connection closed.", DateTime.Now, issuingId);
            
            AddInteration(issuingId, receivingId, '+');
            CheckIfMatched(issuingId, receivingId);
        }

        public void SkipPerson(int issuingId, int receivingId)
        {
            Console.WriteLine("{0} - Client called 'SkipPerson'", DateTime.Now);
            OperationContext.Current.Channel.Faulted += (sender, args) =>
                Console.WriteLine("{0} - Client '{1}' connection failed.", DateTime.Now, issuingId);
            OperationContext.Current.Channel.Closed += (sender, args) =>
                Console.WriteLine("{0} - Client '{1}' connection closed.", DateTime.Now, issuingId);
            
            AddInteration(issuingId, receivingId, '-');
        }

        public string FetchNewPeople(int uid)
        {
            Console.WriteLine("{0} - Client called 'FetchNewPeople'", DateTime.Now);
            OperationContext.Current.Channel.Faulted += (sender, args) =>
                Console.WriteLine("{0} - Client '{1}' connection failed.", DateTime.Now, uid);
            OperationContext.Current.Channel.Closed += (sender, args) =>
                Console.WriteLine("{0} - Client '{1}' connection closed.", DateTime.Now, uid);

            string serializedData;
            var connectionString = Utils.GetConnectionString();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                var userInfoQuery = "SELECT * from dbo.Users WHERE Id='" + uid + "'";
                char gender;
                bool interestedInMale;
                bool interestedInFemale;
                using (var cmd = new SqlCommand(userInfoQuery, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Connection = conn;

                    var adapter = new SqlDataAdapter {SelectCommand = cmd};
                    var results = new DataTable();
                    adapter.Fill(results);
                    if (results.Rows.Count != 1)
                    {
                        return "";
                    }

                    gender = Convert.ToChar(results.Rows[0]["Gender"]);
                    interestedInMale = Convert.ToBoolean(results.Rows[0]["InterestedInMales"]);
                    interestedInFemale = Convert.ToBoolean(results.Rows[0]["InterestedInFemales"]);
                }

                var isInterested = gender == 'M' ? "InterestedInMales=1" : "InterestedInFemales=1";
                string isInteresting;
                if (interestedInFemale && interestedInMale)
                {
                    isInteresting = "";
                }
                else if (interestedInFemale)
                {
                    isInteresting = " AND Gender='F'";
                }
                else
                {
                    isInteresting = " AND Gender='M'";
                }

                var subquery = "SELECT 1 FROM dbo.Interactions WHERE IssuingId=" + uid + "and ReceivingId=Id";

                var query = "SELECT Id, FirstName, Bio, ProfilePicture FROM dbo.Users " +
                            "WHERE NOT EXISTS (" + subquery + ") AND " +
                            "Id!=" + uid + " AND " +
                            isInterested + isInteresting;

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Connection = conn;
                    var adapter = new SqlDataAdapter {SelectCommand = cmd};
                    var results = new DataTable();
                    adapter.Fill(results);
                    if (results.Rows.Count == 0)
                    {
                        return "";
                    }

                    serializedData = Serializer.SerializeObject(results);
                }
            }
            return serializedData;
        }

        public string FetchPairs(int uid)
        {
            Console.WriteLine("{0} - Client called 'FetchPairs'", DateTime.Now);
            OperationContext.Current.Channel.Faulted += (sender, args) =>
                Console.WriteLine("{0} - Client '{1}' connection failed.", DateTime.Now, uid);
            OperationContext.Current.Channel.Closed += (sender, args) =>
                Console.WriteLine("{0} - Client '{1}' connection closed.", DateTime.Now, uid);

            var connectionString = Utils.GetConnectionString();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                var query = "SELECT p.User1Id, p.User2Id, u1.FirstName AS FirstName1, u2.FirstName AS FirstName2, " +
                            "u1.ProfilePicture AS ProfilePicture1, u2.ProfilePicture AS ProfilePicture2 FROM dbo.Pairs p " +
                            "JOIN dbo.Users u1 ON p.User1Id=u1.Id  " +
                            "JOIN dbo.Users u2 ON p.User2Id=u2.Id  " +
                            "WHERE User1Id=" + uid + " OR User2Id=" + uid + ";";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Connection = conn;

                    var adapter = new SqlDataAdapter {SelectCommand = cmd};
                    var results = new DataTable();
                    adapter.Fill(results);

                    return Serializer.SerializeObject(results);
                }
            }
        }

        public void SendMessage(int fromId, int toId, string content)
        {
            var connectionString = Utils.GetConnectionString();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                const string query = "INSERT INTO dbo.Messages (Author, Recipient, Timestamp, Content) " +
                                     "VALUES (@Author, @Recipient, @Timestamp, @Content);";
                using (var sendMessage = new SqlCommand(query, conn))
                {
                    sendMessage.CommandType = CommandType.Text;
                    sendMessage.Connection = conn;

                    sendMessage.Parameters.AddWithValue("@Author", fromId);
                    sendMessage.Parameters.AddWithValue("@Recipient", toId);
                    sendMessage.Parameters.AddWithValue("@Timestamp", DateTime.UtcNow.Ticks);
                    sendMessage.Parameters.AddWithValue("@Content", content);

                    sendMessage.ExecuteNonQuery();
                }
            }
            
            if (ConnectedUsers.ContainsKey(fromId))
            {
                try
                {
                    UserConnected?.Invoke(this, new UserConnectedEventArgs(ConnectedUsers[fromId]));
                }
                catch (Exception)
                {
                    Console.WriteLine("nie pyklo XD");
                }
            }
            if (ConnectedUsers.ContainsKey(toId))
            {
                try
                {
                    UserConnected?.Invoke(this, new UserConnectedEventArgs(ConnectedUsers[toId]));
                }
                catch (Exception)
                {
                    Console.WriteLine("nie pyklo XD");
                }
            }
        }


        public string LoadMessages(int fromId, int toId)
        {
            var connectionString = Utils.GetConnectionString();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                const string query =
                    "SELECT * FROM dbo.Messages WHERE (Author=@Other AND Recipient=@This) OR (Author=@This AND Recipient=@Other);";
                using (var sendMessage = new SqlCommand(query, conn))
                {
                    sendMessage.CommandType = CommandType.Text;
                    sendMessage.Connection = conn;

                    sendMessage.Parameters.AddWithValue("@This", fromId);
                    sendMessage.Parameters.AddWithValue("@Other", toId);

                    sendMessage.ExecuteNonQuery();

                    var adapter = new SqlDataAdapter {SelectCommand = sendMessage};
                    var results = new DataTable();
                    adapter.Fill(results);

                    return Serializer.SerializeObject(results);
                }
            }
        }

        public void Connect(int uid)
        {
            Console.WriteLine("{0} - Client {1} connected.", DateTime.Now, uid);

            var connectedUser = OperationContext.Current.GetCallbackChannel<IUser>();
            ConnectedUsers[uid] = connectedUser;
        }

        public void Disconnect(int uid)
        {
            Console.WriteLine("{0} - Client called 'Disconnect'", DateTime.Now);
            ConnectedUsers.Remove(uid);
        }

        private static void AddInteration(int issuingId, int receivingId, char decision)
        {
            var connectionString = Utils.GetConnectionString();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                const string addInteractionQuery =
                    "INSERT INTO dbo.Interactions (IssuingId, ReceivingId, Decision) " +
                    "VALUES (@IssuingId, @ReceivingId, @Decision);";

                using (var addUser = new SqlCommand(addInteractionQuery, conn))
                {
                    addUser.CommandType = CommandType.Text;
                    addUser.Connection = conn;

                    addUser.Parameters.AddWithValue("@IssuingId", issuingId);
                    addUser.Parameters.AddWithValue("@ReceivingId", receivingId);
                    addUser.Parameters.AddWithValue("@Decision", decision);

                    addUser.ExecuteNonQuery();
                }
            }
        }

        private static void CheckIfMatched(int issuingId, int receivingId)
        {
            var connectionString = Utils.GetConnectionString();

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                var query = "SELECT * FROM dbo.Interactions WHERE IssuingId=" + receivingId + " AND ReceivingId=" +
                            issuingId + ";";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Connection = conn;

                    var adapter = new SqlDataAdapter {SelectCommand = cmd};
                    var results = new DataTable();
                    adapter.Fill(results);

                    if (results.Rows.Count <= 0) return;

                    const string createNewPairQuery = "INSERT INTO dbo.Pairs (User1Id, User2Id) " +
                                                      "VALUES (@User1Id, @User2Id);";

                    using (var createNewPair = new SqlCommand(createNewPairQuery, conn))
                    {
                        createNewPair.CommandType = CommandType.Text;
                        createNewPair.Connection = conn;

                        createNewPair.Parameters.AddWithValue("@User1Id", issuingId);
                        createNewPair.Parameters.AddWithValue("@User2Id", receivingId);

                        createNewPair.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}