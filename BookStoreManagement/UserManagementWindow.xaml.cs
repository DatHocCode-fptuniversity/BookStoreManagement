
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BookStoreManagement.Data;
using BookStoreManagement.Entities;
using BookStoreManagement.Services;
using Microsoft.EntityFrameworkCore;

namespace BookStoreManagement
{
    public partial class UserManagementWindow : Window
    {
        public UserManagementWindow()
        {
            InitializeComponent();
        }

        // 1. LOAD WINDOW
        private void Window_Loaded(
            object sender, RoutedEventArgs e)
        {
            LoadUsers();
        }

        // 2. LOAD USERS
        private void LoadUsers()
        {
            try
            {
                using var authDb = new BookStoreAuthDbContext();
                using var storeDb = new BookStoreDbContext();

                var accounts = authDb.AppUsers
                    .AsNoTracking()
                    .OrderBy(u => u.Id)
                    .ToList();

                var customers = storeDb.Customers
                    .AsNoTracking()
                    .Where(c => c.AppUserId != null)
                    .ToList();

                var users = accounts.Select(u =>
                {
                    var customer = customers.FirstOrDefault(
                        c => c.AppUserId == u.Id);

                    return new UserListItem
                    {
                        Id = u.Id,
                        Username = u.Username,
                        Role = u.Role,
                        CustomerName = customer?.FullName ?? "-",
                        Phone = customer?.Phone ?? "-",
                        Email = customer?.Email ?? "-"
                    };
                }).ToList();

                // Statistics
                txtTotalAccounts.Text = users.Count.ToString();

                txtCustomerAccounts.Text = users.Count(
                    u => u.Role == "Customer").ToString();

                // Search by username
                string keyword = txtSearch.Text.Trim();

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    users = users.Where(u =>
                        u.Username.Contains(
                            keyword,
                            StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                // Filter by role
                string selectedRole =
                    (cbRole.SelectedItem as ComboBoxItem)?
                    .Content?.ToString() ?? "All";

                if (selectedRole != "All")
                {
                    users = users.Where(u =>
                        u.Role == selectedRole).ToList();
                }

                dgUsers.ItemsSource = users;
            }
            catch (Exception)
            {
                MessageBox.Show(
                    "Unable to load user accounts.",
                    "Loading Users Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // 3. SEARCH
        private void btnSearch_Click(
            object sender, RoutedEventArgs e)
        {
            LoadUsers();
        }

        // 4. RESET
        private void btnReset_Click(
            object sender, RoutedEventArgs e)
        {
            txtSearch.Clear();
            cbRole.SelectedIndex = 0;

            LoadUsers();
        }

        // 5. CREATE CUSTOMER USING SHARED SERVICE
        private void btnCreateCustomer_Click(
            object sender, RoutedEventArgs e)
        {
            string username = txtNewUsername.Text.Trim();
            string password = pbNewPassword.Password;
            string fullName = txtFullName.Text.Trim();
            string phone = txtPhone.Text.Trim();
            string email = txtEmail.Text.Trim();
            string address = txtAddress.Text.Trim();

            try
            {
                var service = new CustomerAccountService();

                // Admin form currently has no Confirm Password.
                // Pass password twice to reuse the same service.
                service.RegisterCustomer(
                    username,
                    password,
                    password,
                    fullName,
                    phone,
                    email,
                    address);

                MessageBox.Show(
                    "Customer account created successfully!",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                ClearForm();

                // Display the newly created account
                txtSearch.Clear();
                cbRole.SelectedIndex = 0;

                LoadUsers();
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
                    "Unable to create customer. " +
                    "Username may already exist or " +
                    "the data violates database constraints.",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception)
            {
                MessageBox.Show(
                    "Unable to create customer account.",
                    "Create Customer Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // 6. CLEAR FORM
        private void btnClearForm_Click(
            object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void ClearForm()
        {
            txtNewUsername.Clear();
            pbNewPassword.Clear();
            txtFullName.Clear();
            txtPhone.Clear();
            txtEmail.Clear();
            txtAddress.Clear();
        }

        // 7. CLOSE WINDOW
        private void btnClose_Click(
            object sender, RoutedEventArgs e)
        {
            Close();
        }

        // 8. USER DISPLAY MODEL
        public class UserListItem
        {
            public int Id { get; set; }

            public string Username { get; set; } = "";

            public string Role { get; set; } = "";

            public string CustomerName { get; set; } = "";

            public string Phone { get; set; } = "";

            public string Email { get; set; } = "";
        }
    }
}
