using System;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace Tinder.Pages
{
    /// <summary>
    /// Interaction logic for Signup.xaml
    /// </summary>
    public partial class Signup
    {
        public Signup()
        {
            InitializeComponent();
            Username.Focus();
        }

        public void Reset()
        {
            Username.Text = "";
            FirstName.Text = "";
            Password.Password = "";
            ConfirmPassword.Password = "";
        }

        private void GoBack(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new Uri("Pages/Login.xaml", UriKind.Relative));
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
                    ErrorMessage.Text = "Confirm your password.";
                    ConfirmPassword.Focus();
                }
                else if (password != ConfirmPassword.Password)
                {
                    ErrorMessage.Text = "Both passwords must be the same.";
                    ConfirmPassword.Focus();
                }
                else if (!(IsMale.IsChecked ?? false) && !(IsFemale.IsChecked ?? false))
                {
                    ErrorMessage.Text = "Select your gender.";
                }
                else
                {
                    ErrorMessage.Text = "";
                    var connectionString = Utils.GetConnectionString();

                    using (var conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        var checkIfUserExistsQuery =
                            "SELECT * from dbo.Users WHERE Username='" + username + "'";
                        using (var checkIfUserExists = new SqlCommand(checkIfUserExistsQuery, conn))
                        {
                            checkIfUserExists.CommandType = CommandType.Text;
                            checkIfUserExists.Connection = conn;
                            var gender = (IsMale.IsChecked ?? false) ? 'M' : 'F';
                            var adapter = new SqlDataAdapter {SelectCommand = checkIfUserExists};
                            var dataSet = new DataSet();
                            adapter.Fill(dataSet);
                            if (dataSet.Tables[0].Rows.Count > 0)
                            {
                                ErrorMessage.Text = "User with this username already exists. Choose new username.";
                            }
                            else
                            {
                                const string addUserQuery =
                                    "INSERT INTO dbo.Users (Username, FirstName, PasswordHash, Gender, InterestedInMales, InterestedInFemales) " +
                                    "VALUES (@Username, @FirstName, @PasswordHash, @Gender, 0, 0);";
                                using (var addUser = new SqlCommand(addUserQuery, conn))
                                {
                                    addUser.CommandType = CommandType.Text;
                                    addUser.Connection = conn;
                                    
                                    addUser.Parameters.AddWithValue("@Username", username);
                                    addUser.Parameters.AddWithValue("@FirstName", firstname);
                                    addUser.Parameters.AddWithValue("@PasswordHash", password);
                                    addUser.Parameters.AddWithValue("@Gender", gender);

                                    addUser.ExecuteNonQuery();

                                    NavigationService?.Navigate(new Uri("Pages/Login.xaml", UriKind.Relative));
                                }
                            }
                        }
                    }
                }
            }
        }

        private void OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                Submit_Click(sender, e);
            }
        }
    }
}