using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Runtime.Caching;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
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

            var me = new User();
            var server = new ServerConnection(me);
            var resposne = server.FetchPairs((int) uid);
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

            var me = new User();
            var server = new ServerConnection(me);
            server.SendMessage((int) uid, (int) pairUserId, message);

            RefreshChat(sender, e);
        }

        private void LoadMessages(int pairUserId, string pairFirstName)
        {
            var cache = MemoryCache.Default;
            var uid = cache["UserId"] as int?;
            if (uid is null) return;

            var me = new User();
            var server = new ServerConnection(me);
            var response = server.LoadMessages((int) uid, pairUserId);
            var results = (DataTable) Serializer.DeserializeObject(response);

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
                if (!(child is PairInfo)) continue;
                pairUserId = (child as PairInfo).Id;
                pairFirstName = (child as PairInfo).Text;
            }
            if (pairUserId is null)
            {
                return;
            }

            LoadMessages((int) pairUserId, pairFirstName);
            ReceiveMessages();
        }

        private void ReceiveMessages()
        {

            ISerializer aSerializer = new DataContractJsonStringSerializer();

            // Create broker.
            IDuplexBrokerFactory aBrokerFactory = new DuplexBrokerFactory();
            IDuplexBroker aBroker = aBrokerFactory.CreateBroker();

            // Communicate using WebSockets.
            IMessagingSystemFactory aMessaging = new WebSocketMessagingSystemFactory();
            IDuplexInputChannel anInputChannel =
                aMessaging.CreateDuplexInputChannel("ws://127.0.0.1:8843/CpuUsage/");

            anInputChannel.ResponseReceiverConnected += (x, y) =>
            {
                Console.WriteLine("Connected client: " + y.ResponseReceiverId);
            };
            anInputChannel.ResponseReceiverDisconnected += (x, y) =>
            {
                Console.WriteLine("Disconnected client: " + y.ResponseReceiverId);
            };

            // Attach input channel and start listeing.
            aBroker.AttachDuplexInputChannel(anInputChannel);

            // Start working thread monitoring the CPU usage.
            bool aStopWorkingThreadFlag = false;
            Thread aWorkingThread = new Thread(() =>
            {
                PerformanceCounter aCpuCounter =
                    new PerformanceCounter("Processor", "% Processor Time", "_Total");

                while (!aStopWorkingThreadFlag)
                {
                    CpuUpdateMessage aMessage = new CpuUpdateMessage();
                    aMessage.Usage = aCpuCounter.NextValue();

                    //Console.WriteLine(aMessage.Usage);

                    // Serialize the message.
                    object aSerializedMessage =
                        aSerializer.Serialize<CpuUpdateMessage>(aMessage);

                    // Notify subscribers via the broker.
                    // Note: The broker will forward the message to subscribed clients.
                    aBroker.SendMessage("MyCpuUpdate", aSerializedMessage);

                    Thread.Sleep(500);
                }
            });
            aWorkingThread.Start();
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