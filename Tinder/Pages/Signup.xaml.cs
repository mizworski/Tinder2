using System;
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
            else if (Password.Password.Length == 0)
            {
                ErrorMessage.Text = "Enter password.";
                Password.Focus();
            }
            else if (ConfirmPassword.Password.Length == 0)
            {
                ErrorMessage.Text = "Confirm your password.";
                ConfirmPassword.Focus();
            }
            else if (Password.Password != ConfirmPassword.Password)
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
                var username = Username.Text;
                var firstname = FirstName.Text;
                var password = Password.Password;
                var gender = (IsMale.IsChecked ?? false) ? 'M' : 'F';

                var me = new User();
                var server = new ServerConnection(me);
                var response = server.Signup(username, firstname, password, gender);

                if (!response.Item1)
                {
                    ErrorMessage.Text = response.Item2;
                    Username.Focus();
                    return;
                }

                NavigationService?.Navigate(new Uri("Pages/Login.xaml", UriKind.Relative));
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