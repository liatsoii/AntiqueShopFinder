using AntiqueShopFinder.Services;
using System.Windows;
using System.Windows.Controls;

namespace AntiqueShopFinder
{
    public partial class AdminLoginWindow : Window
    {
        private readonly DatabaseService _databaseService;

        public bool IsAuthenticated { get; private set; }

        public AdminLoginWindow()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
            UsernameTextBox.Focus();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(username))
            {
                ShowError("Введите логин");
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowError("Введите пароль");
                return;
            }

            if (_databaseService.ValidateAdmin(username, password))
            {
                IsAuthenticated = true;
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                ShowError("Неверный логин или пароль");
                PasswordBox.Password = "";
                PasswordBox.Focus();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.Visibility = Visibility.Visible;
        }

        private void UsernameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClearError();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ClearError();
        }

        private void ClearError()
        {
            ErrorTextBlock.Visibility = Visibility.Collapsed;
        }
    }
}