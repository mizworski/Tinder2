using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
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
using Microsoft.Win32;
using Image = System.Drawing.Image;
using Size = System.Drawing.Size;

namespace Tinder.Pages
{
    /// <summary>
    /// Interaction logic for Profile.xaml
    /// </summary>
    public partial class Profile : Page
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
                    if (results.Rows.Count == 1)
                    {
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

                        var imageBytes = (byte[])results.Rows[0]["ProfilePicture"];

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
                    else
                    {
                        Logout(null, null);
                    }
                }
            }
        }

        private void UpdateProfile(object sender, RoutedEventArgs e)
        {
            var cache = MemoryCache.Default;
            var uid = cache["UserId"] as int?;

            var bio = Bio.Text;
            var previouslyInterestedInFemale = cache["InterestedInFemales"] as bool?;
            var previouslyInterestedInMale = cache["InterestedInMales"] as bool?;

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
                    updateProfile.Parameters.AddWithValue("@InterestedInMales", InterestMale.IsChecked);
                    updateProfile.Parameters.AddWithValue("@InterestedInFemales", InterestFemale.IsChecked);
                    updateProfile.Parameters.AddWithValue("@Id", uid);

                    updateProfile.ExecuteNonQuery();
                }
            }

            SetValues();

            if (previouslyInterestedInFemale != InterestFemale.IsChecked || previouslyInterestedInMale != InterestMale.IsChecked)
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

                var connectionString = Utils.GetConnectionString();

                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    var cache = MemoryCache.Default;
                    var uid = cache["UserId"] as int?;

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

            SetValues();
        }


    }
}