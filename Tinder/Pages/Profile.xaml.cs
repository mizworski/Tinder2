using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Runtime.Caching;
using System.Windows;
using System.Windows.Media.Imaging;
using Interface;
using Microsoft.Win32;
using Newtonsoft.Json;
using Image = System.Drawing.Image;
using Size = System.Drawing.Size;

namespace Tinder.Pages
{
    /// <summary>
    /// Interaction logic for Profile.xaml
    /// </summary>
    public partial class Profile
    {
        public Profile()
        {
            InitializeComponent();
            SetValues();
        }

        private void SetValues()
        {
            var cache = MemoryCache.Default;
            var uid = cache["UserId"] as int?;

            if (uid is null)
            {
                Logout(null, null);
                return;
            }

            var me = new User();
            var server = new ServerConnection(me);
            var response = server.GetProfileInfo((int) uid);

            if (!response.Item1)
            {
                Logout(null, null);
                return;
            }

            var results = JsonConvert.DeserializeObject<DataTable>(response.Item2);

            var firstName = Convert.ToString(results.Rows[0]["FirstName"]);
            var bio = Convert.ToString(results.Rows[0]["Bio"]);
            var interestedInMale = Convert.ToBoolean(results.Rows[0]["InterestedInMales"]);
            var interestedInFemale = Convert.ToBoolean(results.Rows[0]["InterestedInFemales"]);

            var policy = new CacheItemPolicy
            {
                AbsoluteExpiration = ObjectCache.InfiniteAbsoluteExpiration
            };
            cache.Set("InterestedInMales", interestedInMale, policy);
            cache.Set("InterestedInFemales", interestedInFemale, policy);

            FirstName.Text = firstName;
            Bio.Text = bio;
            InterestFemale.IsChecked = interestedInFemale;
            InterestMale.IsChecked = interestedInMale;

            if (results.Rows[0]["ProfilePicture"].Equals(DBNull.Value)) return;
            if (response.Item3.Equals("")) return;

            var imageBytes = (byte[]) Serializer.DeserializeObject(response.Item3);
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

        private void UpdateProfile(object sender, RoutedEventArgs e)
        {
            var cache = MemoryCache.Default;
            var uid = cache["UserId"] as int?;

            var bio = Bio.Text;
            var previouslyInterestedInFemale = cache["InterestedInFemales"] as bool?;
            var previouslyInterestedInMale = cache["InterestedInMales"] as bool?;

            if (uid is null)
            {
                Logout(sender, e);
                return;
            }

            var me = new User();
            var server = new ServerConnection(me);

            server.UpdateProfile((int) uid, bio, InterestFemale.IsChecked ?? false, InterestMale.IsChecked ?? false);

            SetValues();

            if (previouslyInterestedInFemale != InterestFemale.IsChecked ||
                previouslyInterestedInMale != InterestMale.IsChecked)
            {
                NewPairs.FetchNewPeople();
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

        private void Logout(object sender, RoutedEventArgs e)
        {
            var cache = MemoryCache.Default;
            cache.Remove("UserId");
            cache.Remove("NewUsers");
            cache.Remove("CurrentProfileViewing");

            NavigationService?.Navigate(new Uri("Pages/Login.xaml", UriKind.Relative));
        }

        private void UploadImage(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                DefaultExt = ".jpg",
                Filter =
                    "JPG Files (*.jpg)|*.jpg|PNG Files (*.png)|*.png|JPEG Files (*.jpeg)|*.jpeg|GIF Files (*.gif)|*.gif"
            };

            var result = dlg.ShowDialog();
            if (result != true) return;

            var filename = dlg.FileName;
            var img = (Bitmap) Image.FromFile(filename, true);
            var resized = new Bitmap(img, new Size(256, 256));

            using (var stream = new MemoryStream())
            {
                resized.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);
                var imageBytes = stream.ToArray();
                var imageSerialized = Serializer.SerializeObject(imageBytes);


                var cache = MemoryCache.Default;
                var uid = cache["UserId"] as int?;
                if (uid is null)
                {
                    Logout(null, null);
                    return;
                }

                var me = new User();
                var server = new ServerConnection(me);
                server.UpdatePicture((int) uid, imageSerialized);
            }

            SetValues();
        }
    }
}