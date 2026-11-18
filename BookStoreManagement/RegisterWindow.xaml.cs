
using System;
using System.Windows;
using BookStoreManagement.Services;
using Microsoft.EntityFrameworkCore;

namespace BookStoreManagement
{
    public partial class RegisterWindow : Window
    {
        public RegisterWindow()
        {
            InitializeComponent();
        }

        // 1. REGISTER BUTTON
        private void btnRegister_Click(
            object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = pbPassword.Password;
            string confirmPassword =
                pbConfirmPassword.Password;
            string fullName = txtFullName.Text.Trim();
            string phone = txtPhone.Text.Trim();
            string email = txtEmail.Text.Trim();
            string address = txtAddress.Text.Trim();

            try
            {
                // 2. CALL SERVICE
                var service = new CustomerAccountService();

                service.RegisterCustomer(
                    username,
                    password,
                    confirmPassword,
                    fullName,
                    phone,
                    email,
                    address);

                // 3. SUCCESS
                MessageBox.Show(
                    "Account created successfully!\n" +
                    "You can now log in.",
                    "Registration Successful",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // 4. RETURN TO LOGIN
                DialogResult = true;
                Close();
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (DbUpdateException)
            {
                MessageBox.Show(
                    "Unable to create account. " +
                    "Username may already exist or " +
                    "the information violates database constraints.",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception)
            {
                MessageBox.Show(
                    "Registration failed. Please try again.",
                    "Registration Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // 5. BACK TO LOGIN
        private void btnBack_Click(
            object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
