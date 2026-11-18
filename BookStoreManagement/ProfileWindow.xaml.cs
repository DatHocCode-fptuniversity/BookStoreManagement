
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using BookStoreManagement.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookStoreManagement
{
    public partial class ProfileWindow : Window
    {
        private readonly int _appUserId;

        // 1. RECEIVE LOGGED-IN USER ID
        public ProfileWindow(int appUserId)
        {
            InitializeComponent();
            _appUserId = appUserId;
        }

        // 2. LOAD WINDOW
        private void Window_Loaded(
            object sender, RoutedEventArgs e)
        {
            LoadProfile();
        }

        // 3. LOAD CUSTOMER PROFILE
        private void LoadProfile()
        {
            try
            {
                using var db = new BookStoreDbContext();

                var user = db.AppUsers
                    .AsNoTracking()
                    .FirstOrDefault(u =>
                        u.Id == _appUserId &&
                        u.Role == "Customer");

                var customer = db.Customers
                    .AsNoTracking()
                    .FirstOrDefault(c =>
                        c.AppUserId == _appUserId);

                if (user == null || customer == null)
                {
                    MessageBox.Show(
                        "Customer profile not found.",
                        "Profile Error");
                    Close();
                    return;
                }

                txtUsername.Text = user.Username;
                txtFullName.Text = customer.FullName;
                txtPhone.Text = customer.Phone ?? "";
                txtEmail.Text = customer.Email ?? "";
                txtAddress.Text = customer.Address ?? "";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Loading Profile Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Close();
            }
        }

        // 4. VALIDATE PROFILE INPUT
        private bool ValidateProfile()
        {
            string fullName = txtFullName.Text.Trim();
            string phone = txtPhone.Text.Trim();
            string email = txtEmail.Text.Trim();
            string address = txtAddress.Text.Trim();

            // ALL FIELDS REQUIRED
            if (string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(phone) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(address))
            {
                MessageBox.Show(
                    "Please fill in all required fields.",
                    "Validation Error");
                return false;
            }

            // FULL NAME
            if (fullName.Length > 150)
            {
                MessageBox.Show(
                    "Full name must not exceed 150 characters.");
                return false;
            }

            // PHONE: DIGITS ONLY, 10-20 DIGITS
            if (!Regex.IsMatch(phone, @"^[0-9]{10,20}$"))
            {
                MessageBox.Show(
                    "Phone must contain 10-20 digits only.",
                    "Validation Error");
                return false;
            }

            // EMAIL FORMAT
            if (email.Length > 150 ||
                !Regex.IsMatch(
                    email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                MessageBox.Show(
                    "Please enter a valid email address.",
                    "Validation Error");
                return false;
            }

            // ADDRESS
            if (address.Length > 255)
            {
                MessageBox.Show(
                    "Address must not exceed 255 characters.");
                return false;
            }

            return true;
        }

        private void btnChangePassword_Click(
            object sender, RoutedEventArgs e)
        {
            ChangePasswordWindow changePasswordWindow =
                new ChangePasswordWindow(_appUserId);

            changePasswordWindow.Owner = this;

            changePasswordWindow.ShowDialog();
        }


        // 5. SAVE CHANGES
        private void btnSave_Click(
            object sender, RoutedEventArgs e)
        {
            if (!ValidateProfile())
                return;

            try
            {
                using var db = new BookStoreDbContext();

                // Update ONLY the logged-in customer's profile
                var customer = db.Customers
                    .FirstOrDefault(c =>
                        c.AppUserId == _appUserId);

                if (customer == null)
                {
                    MessageBox.Show(
                        "Customer profile not found.");
                    return;
                }

                customer.FullName = txtFullName.Text.Trim();
                customer.Phone = txtPhone.Text.Trim();
                customer.Email = txtEmail.Text.Trim();
                customer.Address = txtAddress.Text.Trim();

                db.SaveChanges();

                MessageBox.Show(
                    "Profile updated successfully!",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (DbUpdateException)
            {
                MessageBox.Show(
                    "Unable to update profile. " +
                    "Please check your information.",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Update Profile Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // 6. CANCEL
        private void btnCancel_Click(
            object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
