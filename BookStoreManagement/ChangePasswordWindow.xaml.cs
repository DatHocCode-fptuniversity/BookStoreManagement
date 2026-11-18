
using System;
using System.Linq;
using System.Windows;
using BookStoreManagement.Data;
using BookStoreManagement.Entities;
using Microsoft.AspNetCore.Identity;

namespace BookStoreManagement
{
    public partial class ChangePasswordWindow : Window
    {
        private readonly int _appUserId;

        // 1. RECEIVE LOGGED-IN USER
        public ChangePasswordWindow(int appUserId)
        {
            InitializeComponent();

            _appUserId = appUserId;
        }

        // 2. UPDATE PASSWORD BUTTON
        private void btnUpdatePassword_Click(
            object sender, RoutedEventArgs e)
        {
            string currentPassword =
                pbCurrentPassword.Password;

            string newPassword =
                pbNewPassword.Password;

            string confirmPassword =
                pbConfirmPassword.Password;

            // 3. REQUIRED FIELDS
            if (string.IsNullOrWhiteSpace(currentPassword) ||
                string.IsNullOrWhiteSpace(newPassword) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                MessageBox.Show(
                    "Please fill in all fields.",
                    "Validation Error");
                return;
            }

            // 4. NEW PASSWORD LENGTH
            if (newPassword.Length < 8 ||
                newPassword.Length > 128)
            {
                MessageBox.Show(
                    "New password must contain 8-128 characters.",
                    "Validation Error");
                return;
            }

            // 5. CONFIRM NEW PASSWORD
            if (newPassword != confirmPassword)
            {
                MessageBox.Show(
                    "New passwords do not match.",
                    "Validation Error");
                return;
            }

            // 6. DO NOT REUSE OLD PASSWORD
            if (newPassword == currentPassword)
            {
                MessageBox.Show(
                    "New password must be different " +
                    "from your current password.",
                    "Validation Error");
                return;
            }

            try
            {
                // 7. CONNECT TO AUTH DATABASE
                using var db = new BookStoreAuthDbContext();

                var user = db.AppUsers.FirstOrDefault(
                    u => u.Id == _appUserId &&
                         u.Role == "Customer");

                if (user == null)
                {
                    MessageBox.Show(
                        "Customer account not found.",
                        "Account Error");
                    return;
                }

                // 8. VERIFY CURRENT PASSWORD
                var hasher = new PasswordHasher<AppUser>();

                PasswordVerificationResult result;

                try
                {
                    result = hasher.VerifyHashedPassword(
                        user,
                        user.PasswordHash,
                        currentPassword);
                }
                catch (FormatException)
                {
                    result = PasswordVerificationResult.Failed;
                }

                if (result == PasswordVerificationResult.Failed)
                {
                    MessageBox.Show(
                        "Current password is incorrect.",
                        "Validation Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // 9. HASH NEW PASSWORD
                user.PasswordHash =
                    hasher.HashPassword(user, newPassword);

                // 10. SAVE TO DATABASE
                db.SaveChanges();

                MessageBox.Show(
                    "Password changed successfully!",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // 11. CLOSE WINDOW
                DialogResult = true;
                Close();
            }
            catch (Exception)
            {
                MessageBox.Show(
                    "Unable to change password. Please try again.",
                    "Change Password Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // 12. CANCEL BUTTON
        private void btnCancel_Click(
            object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
