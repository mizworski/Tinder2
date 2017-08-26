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
    /// Interaction logic for Chat.xaml
    /// </summary>
    public partial class Chat : Page
    {
        public Chat()
        {
            InitializeComponent();
            LoadPairs();
        }

        private void LoadPairs()
        {
            Pairs.Items.Clear();

            var cache = MemoryCache.Default;
            var uid = cache["UserId"] as int?;
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

                    foreach (DataRow pairData in results.Rows)
                    {
                        var otherUserNumber = (int) pairData["User2Id"] == uid ? 1 : 2;

                        var pair = new StackPanel {Orientation = Orientation.Horizontal};
                        var name = new TextBlock
                        {
                            Text = pairData["FirstName2"] as string,
                            Padding = new Thickness
                            {
                                Left = 10,
                                Top = 25,
                                Bottom = 25
                            }
                        };
                        var profilePicture = new Image
                        {
                            Height = 64,
                            Width = 64
                        };

                        if (otherUserNumber == 1)
                        {
                            name.Text = pairData["FirstName1"] as string;
                            if (pairData["ProfilePicture1"].Equals(DBNull.Value))
                            {
                                var defaultImg = new BitmapImage(new Uri("/Tinder;component/Pictures/default.jpg",
                                    UriKind.Relative));
                                profilePicture.Source = defaultImg;
                            }
                            else
                            {
                                var imageBytes = (byte[]) pairData["ProfilePicture1"];
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
                                profilePicture.Source = image;
                            }
                        }
                        else
                        {
                            name.Text = pairData["FirstName2"] as string;

                            if (pairData["ProfilePicture2"].Equals(DBNull.Value))
                            {
                                var defaultImg =
                                    new BitmapImage(new Uri("/Tinder;component/Pictures/default.jpg",
                                        UriKind.Relative));
                                profilePicture.Source = defaultImg;
                            }
                            else
                            {
                                var imageBytes = (byte[]) pairData["ProfilePicture2"];
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
                                profilePicture.Source = image;
                            }
                        }
                        pair.Children.Add(profilePicture);
                        pair.Children.Add(name);
                        Pairs.Items.Add(pair);
                    }
                }
            }
        }

        private void SendMessage(object sender, RoutedEventArgs e)
        {
            var message = MessageText.Text;

            ChatHistory.Inlines.Add(message);
            ChatHistory.Inlines.Add("\n");

            ChatScroller.ScrollToBottom();
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