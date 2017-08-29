using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.WebSocketMessagingSystem;
using Eneter.Messaging.Nodes.Broker;
using Interface;

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
            var cache = MemoryCache.Default;
            var uid = cache["UserId"] as int?;
            if (uid is null)
            {
                return;
            }

            var me = new User(this, Pairs, ChatHistory, ChatScroller);
            var server = new ServerConnection(me);
            server.Connect((int) uid);

            var policy = new CacheItemPolicy
            {
                AbsoluteExpiration = ObjectCache.InfiniteAbsoluteExpiration
            };
            cache.Set("ServerConnection", server, policy);
            LoadPairs();
        }

        private void LoadPairs()
        {
            Pairs.Items.Clear();

            var cache = MemoryCache.Default;
            var uid = cache["UserId"] as int?;
            if (uid is null)
            {
                return;
            }
            var server = cache["ServerConnection"] as ServerConnection;
            var resposne = server?.FetchPairs((int) uid);
            var results = (DataTable) Serializer.DeserializeObject(resposne);

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

        private void SendMessage(object sender, RoutedEventArgs e)
        {
            var cache = MemoryCache.Default;
            var message = MessageText.Text;
            if (message.Length == 0)
            {
                return;
            }

            MessageText.Clear();

            var selectedPair = Pairs.SelectedItem as StackPanel;
            var uid = cache["UserId"] as int?;
            int? pairUserId = null;

            if (selectedPair is null) return;

            foreach (var child in selectedPair.Children)
            {
                if (child is PairInfo)
                {
                    pairUserId = (child as PairInfo).Id;
                }
            }

            if (pairUserId is null || uid is null)
            {
                return;
            }

            var server = cache["ServerConnection"] as ServerConnection;
            server?.SendMessage((int) uid, (int) pairUserId, message);

//            RefreshChat(Pairs, ChatHistory, ChatScroller);
        }

        private static void LoadMessages(int pairUserId, string pairFirstName, TextBlock chat, ScrollViewer scroller)
        {
            var cache = MemoryCache.Default;
            var uid = cache["UserId"] as int?;
            if (uid is null) return;

            var server = cache["ServerConnection"] as ServerConnection;
            var response = server?.LoadMessages((int) uid, pairUserId);
            var results = (DataTable) Serializer.DeserializeObject(response);

            chat.Text = "";

            foreach (DataRow res in results.Rows)
            {
                var fromId = (int) res["Author"];
                var from = fromId == uid ? "You" : pairFirstName;
                var timestamp = (long) res["Timestamp"];
                var date = new DateTime(timestamp).ToString("HH:mm");
                var content = (string) res["Content"];
                var message = $"{from} {date}: {content}\n";

                chat.Inlines.Add(message);
            }
            scroller.ScrollToBottom();
        }

        private void SelectionChangedPairs(object sender, RoutedEventArgs e)
        {
            RefreshChat(Pairs, ChatHistory, ChatScroller);
        }
       

        public void RefreshChat(ListBox pairs, TextBlock chat, ScrollViewer scroller)
        {

            //            Dispatcher.CurrentDispatcher.Invoke(() =>
            //            {
//            if (ListBox)
//            {
//                this.lblCounter.BeginInvoke((MethodInvoker)delegate () { this.lblCounter.Text = this.index.ToString(); ; });
//            }
//            else
//            {
//                this.lblCounter.Text = this.index.ToString(); ;
//            }
            Dispatcher.Invoke(() =>
            {
                Console.WriteLine("dupa");
                var selectedPair = pairs.SelectedItem as StackPanel;
                int? pairUserId = null;
                var pairFirstName = "";

                if (selectedPair is null)
                {
                    return;
                }
                foreach (var child in selectedPair.Children)
                {
                    if (!(child is PairInfo)) continue;
                    pairUserId = (child as PairInfo).Id;
                    pairFirstName = (child as PairInfo).Text;
                }
                if (pairUserId is null)
                {
                    return;
                }
                LoadMessages((int)pairUserId, pairFirstName, chat, scroller);
            });
//            });

//            Application.Current.Dispatcher.Invoke(() =>
//                {
//                    var selectedPair = pairs.SelectedItem as StackPanel;
//                    int? pairUserId = null;
//                    var pairFirstName = "";
//
//                    if (selectedPair is null)
//                    {
//                        return;
//                    }
//                    foreach (var child in selectedPair.Children)
//                    {
//                        if (!(child is PairInfo)) continue;
//                        pairUserId = (child as PairInfo).Id;
//                        pairFirstName = (child as PairInfo).Text;
//                    }
//                    if (pairUserId is null)
//                    {
//                        return;
//                    }
//                    LoadMessages((int) pairUserId, pairFirstName, chat, scroller);
//                }, DispatcherPriority.ContextIdle
//            );
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
            var cache = MemoryCache.Default;
            var server = cache["ServerConnection"] as ServerConnection;
            server?.Disconnect();
            NavigationService?.Navigate(new Uri("Pages/Profile.xaml", UriKind.Relative));
        }

        private void ChangeToNewPairs(object sender, RoutedEventArgs e)
        {
            var cache = MemoryCache.Default;
            var server = cache["ServerConnection"] as ServerConnection;
            server?.Disconnect();
            NavigationService?.Navigate(new Uri("Pages/NewPairs.xaml", UriKind.Relative));
        }

        private void ChangeToChat(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new Uri("Pages/Chat.xaml", UriKind.Relative));
        }
    }
}