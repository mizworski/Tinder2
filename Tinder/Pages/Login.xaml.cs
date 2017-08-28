using System;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.Caching;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json;
using Tinder.Pages;

namespace Tinder
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login
    {
        public Login()
        {
            InitializeComponent();
            Username.Focus();

        }

        private void ChangeToSignup(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new Uri("Pages/Signup.xaml", UriKind.Relative));
        }

        private void TryLogin(object sender, RoutedEventArgs e)
        {
            if (Username.Text.Length == 0)
            {
                ErrorMessage.Text = "Enter username.";
                Username.Focus();
            }
            else
            {
                var username = Username.Text;
                var password = Password.Password;


                var me = new User();
                var server = new ServerConnection(me);
                var response = server.Authenticate(username, password);

                if (!response.Item1)
                {
                    ErrorMessage.Text = response.Item2;
                    return;
                }

                var cache = MemoryCache.Default;
                var json = response.Item2;
                var results = JsonConvert.DeserializeObject<DataTable>(json);

                var uid = Convert.ToInt32(results.Rows[0]["Id"]);
                var gender = Convert.ToChar(results.Rows[0]["Gender"]);
                var interestedInMale = Convert.ToBoolean(results.Rows[0]["InterestedInMales"]);
                var interestedInFemale = Convert.ToBoolean(results.Rows[0]["InterestedInFemales"]);

                var policy = new CacheItemPolicy
                {
                    AbsoluteExpiration = ObjectCache.InfiniteAbsoluteExpiration
                };

                cache.Remove("CurrentProfileViewing");
                cache.Remove("NewUsers");
                cache.Set("UserId", uid, policy);
                cache.Set("Gender", gender, policy);
                cache.Set("InterestedInMales", interestedInMale, policy);
                cache.Set("InterestedInFemales", interestedInFemale, policy);
                
                NavigationService?.Navigate(new Uri("Pages/NewPairs.xaml", UriKind.Relative));

            }
        }

        private void OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                TryLogin(sender, e);
            }
        }
    }
}