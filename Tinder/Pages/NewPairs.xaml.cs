using System;
using System.Data;
using System.IO;
using System.Runtime.Caching;
using System.Windows;
using System.Windows.Media.Imaging;
using Interface;

namespace Tinder.Pages
{
    /// <summary>
    /// Interaction logic for NewPairs.xaml
    /// </summary>
    public partial class NewPairs
    {
        public NewPairs()
        {
            InitializeComponent();
            UpdatePage();
        }

        private void UpdatePage()
        {
            var cache = MemoryCache.Default;
            var newUsers = cache["NewUsers"] as DataTable;
            if (newUsers is null || newUsers.Rows.Count == 0)
            {
                FetchNewPeople();
            }

            ShowPerson();
        }

        private void Skip(object sender, RoutedEventArgs e)
        {
            var cache = MemoryCache.Default;
            var newUsers = cache["NewUsers"] as DataTable;
            var profileViewing = cache["CurrentProfileViewing"] as DataRow;

            if (newUsers is null || profileViewing is null)
            {
                UpdatePage();
                return;
            }

            var uid = (int) cache["UserId"];
            var receiving = (int) profileViewing["Id"];

            var me = new User();
            var server = new ServerConnection(me);
            server.SkipPerson(uid, receiving);

            cache.Remove("CurrentProfileViewing");
            profileViewing.Delete();
            newUsers.AcceptChanges();

            UpdatePage();
        }

        private void Like(object sender, RoutedEventArgs e)
        {
            var cache = MemoryCache.Default;
            var newUsers = cache["NewUsers"] as DataTable;
            var profileViewing = cache["CurrentProfileViewing"] as DataRow;

            if (newUsers is null || profileViewing is null)
            {
                UpdatePage();
                return;
            }

            var uid = (int) cache["UserId"];
            var receiving = (int) profileViewing["Id"];

            var me = new User();
            var server = new ServerConnection(me);
            server.LikePerson(uid, receiving);


            cache.Remove("CurrentProfileViewing");
            profileViewing.Delete();
            newUsers.AcceptChanges();

            UpdatePage();
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

            if (userToShow["ProfilePicture"].Equals(DBNull.Value))
            {
                var defaultImg = new BitmapImage(new Uri("/Tinder;component/Pictures/default.jpg", UriKind.Relative));
                ProfilePicture.Source = defaultImg;
                return;
            }

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
            var cache = MemoryCache.Default;
            var uid = cache["UserId"] as int?;
            var interestedInMale = cache["InterestedInMales"] as bool?;
            var interestedInFemale = cache["InterestedInFemales"] as bool?;

            if (interestedInFemale == false && interestedInMale == false
            ) // Does checking 'cond == false' look dumb only to me to or it's actually dumb?
            {
                cache.Remove("NewUsers");
                cache.Remove("CurrentProfileViewing");
                return;
            }
            if (uid is null)
            {
                return;
            }

            var me = new User();
            var server = new ServerConnection(me);
            var response = server.FetchNewPeople((int) uid);

            if (response.Equals("")) return;

            var policy = new CacheItemPolicy
            {
                AbsoluteExpiration = ObjectCache.InfiniteAbsoluteExpiration
            };
            var results = (DataTable) Serializer.DeserializeObject(response);
            cache.Set("NewUsers", results, policy);
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