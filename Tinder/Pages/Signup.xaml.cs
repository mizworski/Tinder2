using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Interaction logic for Signup.xaml
    /// </summary>
    public partial class Signup : Page
    {
        public Signup()
        {
            InitializeComponent();
        }

        public void Reset()
        {
            Username.Text = "";
            FirstName.Text = "";
            Password.Password = "";
            ConfirmPassword.Password = "";
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            if (Username.Text.Length == 0)
            {
                ErrorMessage.Text = "Enter username.";
                Username.Focus();
            }
            else if (FirstName.Text.Length == 0)
            {
                ErrorMessage.Text = "Enter first name.";
                FirstName.Focus();
            }
            else if (!Regex.IsMatch(FirstName.Text, @"^[A-Z][a-z][\w\.-]*"))
            {
                ErrorMessage.Text =
                    "First name should start with capital letter and then contain only lowercase letters.";
                FirstName.Select(0, FirstName.Text.Length);
                FirstName.Focus();
            }
            else
            {
                var username = Username.Text;
                var firstname = FirstName.Text;
                var password = Password.Password;
                if (password.Length == 0)
                {
                    ErrorMessage.Text = "Enter password.";
                    Password.Focus();
                }
                else if (ConfirmPassword.Password.Length == 0)
                {
                    ErrorMessage.Text = "Enter Confirm password.";
                    ConfirmPassword.Focus();
                }
                else if (password != ConfirmPassword.Password)
                {
                    ErrorMessage.Text = "Confirm password must be same as password.";
                    ConfirmPassword.Focus();
                }
                else
                {
                    ErrorMessage.Text = "";
                    var connectionString = GetConnectionString();

                    using (var conn = new SqlConnection(connectionString))
                    {
                        conn.Open();

                        var query = "INSERT INTO dbo.Users (Username, FirstName, PasswordHash) " +
                                       "VALUES (@Username, @FirstName, @PasswordHash);";
                        using (var cmd = new SqlCommand(query, conn))
                        {
                            cmd.CommandType = CommandType.Text;
                            cmd.Connection = conn;
                            
                            cmd.Parameters.AddWithValue("@Username", Username.Text);
                            cmd.Parameters.AddWithValue("@FirstName", FirstName.Text);
                            cmd.Parameters.AddWithValue("@PasswordHash", Password.Password);

                            cmd.ExecuteNonQuery(); 
                        }
                    }
                    ErrorMessage.Text = "You have Registered successfully.";
                    Reset();
                }
            }
        }

        private static string GetConnectionString()
        {
            return @"Data Source=(LocalDB)\MSSQLLocalDB;
                    AttachDbFilename=C:\Users\Michal\documents\visual studio 2017\Projects\Tinder\Tinder\TinderDatabase.mdf;
                    Integrated Security=True";
        }
    }
}