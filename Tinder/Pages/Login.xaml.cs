using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
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
using Tinder.Pages;

namespace Tinder
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Page
    {
        public Login()
        {
            InitializeComponent();
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

                var connectionString = Utils.GetConnectionString();

                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    var query = "SELECT * from dbo.Users WHERE Username='" + username + "' and PasswordHash='" +
                                password +
                                "'";
                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.Connection = conn;

                        var adapter = new SqlDataAdapter {SelectCommand = cmd};
                        var results = new DataTable();
                        adapter.Fill(results);
                        switch (results.Rows.Count)
                        {
                            case 1:
                                var cache = MemoryCache.Default;

                                var uid = Convert.ToInt32(results.Rows[0]["Id"]);
                                var gender = Convert.ToChar(results.Rows[0]["Gender"]);
                                var interestedInMale = Convert.ToBoolean(results.Rows[0]["InterestedInMales"]);
                                var interestedInFemale = Convert.ToBoolean(results.Rows[0]["InterestedInFemales"]);

                                var policy = new CacheItemPolicy
                                {
                                    AbsoluteExpiration = ObjectCache.InfiniteAbsoluteExpiration
                                };

                                cache.Set("UserId", uid, policy);
                                cache.Set("Gender", gender, policy);
                                cache.Set("InterestedInMales", interestedInMale, policy);
                                cache.Set("InterestedInFemales", interestedInFemale, policy);

                                NavigationService?.Navigate(new Uri("Pages/NewPairs.xaml", UriKind.Relative));
                                break;
                            case 0:
                                ErrorMessage.Text = "Invalid username or password.";
                                break;
                            default:
                                ErrorMessage.Text = "Ooops, we are screwed.";
                                break;
                        }
                    }
                }
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