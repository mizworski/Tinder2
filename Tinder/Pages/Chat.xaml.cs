using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Runtime.Caching;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Tinder.Pages
{
    /// <summary>
    /// Interaction logic for Chat.xaml
    /// </summary>
    public partial class Chat
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
                        var pairUserId = (int) (otherUserNumber == 1 ? pairData["User1Id"] : pairData["User2Id"]);
                        var pair = new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                        };

                        var name = new PairInfo
                        {
                            Id = pairUserId,
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
            MessageText.Clear();

            var selectedPair = Pairs.SelectedItem as StackPanel;
            int? pairUserId = null;

            if (selectedPair is null) return;

            foreach (var child in selectedPair.Children)
            {
                if (child is PairInfo)
                {
                    pairUserId = (child as PairInfo).Id;
                }
            }

            if (pairUserId is null)
            {
                return;
            }

            var cache = MemoryCache.Default;
            var uid = cache["UserId"] as int?;
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

                    sendMessage.Parameters.AddWithValue("@Author", uid);
                    sendMessage.Parameters.AddWithValue("@Recipient", pairUserId);
                    sendMessage.Parameters.AddWithValue("@Timestamp", DateTime.UtcNow.Ticks);
                    sendMessage.Parameters.AddWithValue("@Content", message);

                    sendMessage.ExecuteNonQuery();
                }
            }
            RefreshChat(sender, e);
        }

        private void LoadMessages(int pairUserId, string pairFirstName)
        {
            var cache = MemoryCache.Default;
            var uid = cache["UserId"] as int?;

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

                    sendMessage.Parameters.AddWithValue("@This", uid);
                    sendMessage.Parameters.AddWithValue("@Other", pairUserId);

                    sendMessage.ExecuteNonQuery();

                    var adapter = new SqlDataAdapter {SelectCommand = sendMessage};
                    var results = new DataTable();
                    adapter.Fill(results);

                    ChatHistory.Text = "";

                    foreach (DataRow res in results.Rows)
                    {
                        var fromId = (int) res["Author"];
                        var from = fromId == uid ? "You" : pairFirstName;
                        var timestamp = (long) res["Timestamp"];
                        var date = new DateTime(timestamp).ToString("HH:mm");
                        var content = (string) res["Content"];
                        var message = $"{from} {date}: {content}\n";

                        ChatHistory.Inlines.Add(message);
                    }

                    ChatScroller.ScrollToBottom();
                }
            }
        }

        private void RefreshChat(object sender, RoutedEventArgs e)
        {
            var selectedPair = Pairs.SelectedItem as StackPanel;
            int? pairUserId = null;
            var pairFirstName = "";

            if (selectedPair is null)
            {
                return;
            }
            foreach (var child in selectedPair.Children)
            {
                if (child is PairInfo)
                {
                    pairUserId = (child as PairInfo).Id;
                    pairFirstName = (child as PairInfo).Text;
                }
            }
            if (pairUserId is null)
            {
                return;
            }

            LoadMessages((int) pairUserId, pairFirstName);
        }

        private void OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                SendMessage(sender, e);
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