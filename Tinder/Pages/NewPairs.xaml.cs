using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Tinder.Pages
{
    /// <summary>
    /// Interaction logic for NewPairs.xaml
    /// </summary>
    public partial class NewPairs : Page
    {
        public NewPairs()
        {
            InitializeComponent();
            UpdatePage();
        }

        private void UpdatePage()
        {
            var cache = MemoryCache.Default;
            var profileViewing = cache["CurrentProfileViewing"] as DataRow;
            if (profileViewing is null)
            {
                FetchNewPeople();
            }

            ShowPerson();
        }

        private void SkipPerson(object sender, RoutedEventArgs e)
        {
            var cache = MemoryCache.Default;
            var newUsers = cache["NewUsers"] as DataTable;
            var userToShow = cache["CurrentProfileViewing"] as DataRow;

            if (userToShow is null)
            {
                FetchNewPeople();
                return;
            }

            var uid = (int) cache["UserId"];
            var profileViewing = (DataRow) cache["CurrentProfileViewing"];
            var receiving = (int) profileViewing["Id"];

            AddInteration(uid, receiving, '-');

            cache.Remove("CurrentProfileViewing");
            userToShow.Delete();
            newUsers?.AcceptChanges();

            UpdatePage();
        }

        private void LikePerson(object sender, RoutedEventArgs e)
        {
            var cache = MemoryCache.Default;
            var newUsers = cache["NewUsers"] as DataTable;
            var userToShow = cache["CurrentProfileViewing"] as DataRow;

            if (userToShow is null)
            {
                FetchNewPeople();
                return;
            }

            var uid = (int) cache["UserId"];
            var profileViewing = (DataRow) cache["CurrentProfileViewing"];
            var receiving = (int) profileViewing["Id"];

            AddInteration(uid, receiving, '+');
            CheckIfMatched(uid, receiving);
            cache.Remove("CurrentProfileViewing");
            userToShow.Delete();
            newUsers?.AcceptChanges();

            UpdatePage();
        }

        private void CheckIfMatched(int issuingId, int receivingId)
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
                    if (results.Rows.Count > 0)
                    {
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

        private void AddInteration(int issuingId, int receivingId, char decision)
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

        private void ShowPerson()
        {
            var cache = MemoryCache.Default;
            var newUsers = cache["NewUsers"] as DataTable;
            if (newUsers is null || newUsers.Rows.Count == 0)
            {
                FirstName.Text =
                    "We're sorry but there're no more users available for you. Maybe ask your friends to join?";
                Bio.Text = "¯\\_(ツ)_/¯";

                var defaultImg = new BitmapImage(new Uri("/Tinder;component/Pictures/shrug.jpg", UriKind.Relative));
                ProfilePicture.Source = defaultImg;

                return;
            }

            var randomSelector = new Random();
            var userToShow = cache["CurrentProfileViewing"] as DataRow;
            if (userToShow is null)
            {
                userToShow = newUsers.Rows[randomSelector.Next(newUsers.Rows.Count)];
                var policy = new CacheItemPolicy
                {
                    AbsoluteExpiration = ObjectCache.InfiniteAbsoluteExpiration
                };
                cache.Set("CurrentProfileViewing", userToShow, policy);
            }

            var userFirstName = userToShow["FirstName"] as string;
            var userBio = userToShow["Bio"] as string;

            FirstName.Text = userFirstName;
            Bio.Text = userBio;

            if (userToShow["ProfilePicture"].Equals(DBNull.Value)) return;

            var imageBytes = (byte[]) userToShow["ProfilePicture"];

            var image = new BitmapImage();
            using (var mem = new MemoryStream(imageBytes))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
            }
            image.Freeze();
            ProfilePicture.Source = image;
        }

        public static void FetchNewPeople()
        {
            var connectionString = Utils.GetConnectionString();

            var cache = MemoryCache.Default;
            var uid = cache["UserId"] as int?;
            var gender = cache["Gender"] as char?;
            var interestedInMale = cache["InterestedInMales"] as bool?;
            var interestedInFemale = cache["InterestedInFemales"] as bool?;

            if (interestedInFemale == false && interestedInMale == false
            ) // Does checking 'cond == false' look dumb only to me to or it's actually dumb?
            {
                cache.Remove("NewUsers");
                cache.Remove("CurrentProfileViewing");
                return;
            }

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                var isInterested = gender == 'M' ? "InterestedInMales=1" : "InterestedInFemales=1";
                string isInteresting;
                if (interestedInFemale == true && interestedInMale == true)
                {
                    isInteresting = "";
                }
                else if (interestedInFemale == true)
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

                    var policy = new CacheItemPolicy
                    {
                        AbsoluteExpiration = ObjectCache.InfiniteAbsoluteExpiration
                    };
                    cache.Set("NewUsers", results, policy);
                }
            }
        }

        private void ChangeToProfile(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new Uri("Pages/Profile.xaml", UriKind.Relative));
        }

        private void ChangeToNewPairs(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new Uri("Pages/NewPairs.xaml", UriKind.Relative));
        }

        private void ChangeToChat(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new Uri("Pages/Chat.xaml", UriKind.Relative));
        }
    }
}