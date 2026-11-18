
using System;
using System.Linq;
using System.Windows;
using BookStoreManagement.Data;
using BookStoreManagement.Entities;
using Microsoft.AspNetCore.Identity;

namespace BookStoreManagement
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(
            object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password;

            // 1. VALIDATE INPUT
            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show(
                    "Please enter username and password.",
                    "Validation Error");
                return;
            }

            try
            {
                // 2. CONNECT TO AUTH DATABASE
                using var db = new BookStoreAuthDbContext();

                // 3. FIND USER BY USERNAME
                var user = db.AppUsers.FirstOrDefault(
                    u => u.Username == username);

                if (user == null)
                {
                    MessageBox.Show(
                        "Invalid username or password!",
                        "Login Failed");
                    return;
                }

                // 4. VERIFY HASHED PASSWORD
                var hasher = new PasswordHasher<AppUser>();

                PasswordVerificationResult result;

                try
                {
                    result = hasher.VerifyHashedPassword(
                        user,
                        user.PasswordHash,
                        password);
                }
                catch (FormatException)
                {
                    result = PasswordVerificationResult.Failed;
                }

                if (result == PasswordVerificationResult.Failed)
                {
                    MessageBox.Show(
                        "Invalid username or password!",
                        "Login Failed");
                    return;
                }

                // 5. UPDATE PASSWORD HASH IF NEEDED
                if (result == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    user.PasswordHash =
                        hasher.HashPassword(user, password);

                    db.SaveChanges();
                }

                // 6. ROLE-BASED NAVIGATION
                Window nextWindow;

                if (user.Role == "Admin")
                {
                    nextWindow = new MainWindow();
                }
                else if (user.Role == "Customer")
                {
                    nextWindow = new CustomerWindow(user.Id);
                }
                else
                {
                    MessageBox.Show(
                        "Unknown user role.",
                        "Login Error");
                    return;
                }

                // 7. OPEN CORRECT WINDOW
                Application.Current.MainWindow = nextWindow;

                nextWindow.Show();

                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Login Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void btnOpenRegister_Click(
            object sender, RoutedEventArgs e)
        {
            RegisterWindow registerWindow = new RegisterWindow();

            registerWindow.Owner = this;

            bool? result = registerWindow.ShowDialog();

            if (result == true)
            {
                txtUsername.Clear();
                txtPassword.Clear();

                txtUsername.Focus();
            }
        }

    }
}
