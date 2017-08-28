using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Runtime.Caching;
using System.ServiceModel;
using System.Text.RegularExpressions;
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

        public Tuple<bool, string> Signup(string username, string firstname, string password, char gender)
        {
            Console.WriteLine("{0} - Client called 'Signup'", DateTime.Now);
            OperationContext.Current.Channel.Faulted += (sender, args) =>
                Console.WriteLine("{0} - Client '{1}' connection failed.", DateTime.Now, username);
            OperationContext.Current.Channel.Closed += (sender, args) =>
                Console.WriteLine("{0} - Client '{1}' connection closed.", DateTime.Now, username);

            var connectedUser = OperationContext.Current.GetCallbackChannel<IUser>();
            UserConnected?.Invoke(this, new UserConnectedEventArgs(connectedUser));

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
                        var adapter = new SqlDataAdapter { SelectCommand = checkIfUserExists };
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

                    var adapter = new SqlDataAdapter { SelectCommand = cmd };
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
                    } else
                    {
                        var imageBytes = (byte[])results.Rows[0]["ProfilePicture"];
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

            var connectedUser = OperationContext.Current.GetCallbackChannel<IUser>();
            UserConnected?.Invoke(this, new UserConnectedEventArgs(connectedUser));

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

        public void Disconnect()
        {
            Console.WriteLine("{0} - Client called 'Disconnect'", DateTime.Now);
        }
    }
}