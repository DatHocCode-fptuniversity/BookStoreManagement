
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using BookStoreManagement.Data;
using BookStoreManagement.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookStoreManagement
{
    public partial class CustomerManagementWindow : Window
    {
        private int? _selectedCustomerId;

        public CustomerManagementWindow()
        {
            InitializeComponent();
        }

        // 1. LOAD WINDOW
        private void Window_Loaded(
            object sender, RoutedEventArgs e)
        {
            LoadCustomers();
        }

        // 2. LOAD CUSTOMER LIST
        private void LoadCustomers(int? selectCustomerId = null)
        {
            try
            {
                using var storeDb = new BookStoreDbContext();
                using var authDb = new BookStoreAuthDbContext();

                var customers = storeDb.Customers
                    .AsNoTracking()
                    .OrderBy(c => c.CustomerId)
                    .ToList();

                // Get account usernames for linked customers
                var accounts = authDb.AppUsers
                    .AsNoTracking()
                    .Select(u => new
                    {
                        u.Id,
                        u.Username
                    })
                    .ToList();

                var rows = customers.Select(c =>
                {
                    var account = accounts.FirstOrDefault(
                        a => c.AppUserId == a.Id);

                    return new CustomerRow
                    {
                        CustomerId = c.CustomerId,
                        FullName = c.FullName,
                        Username = account?.Username ?? "-",
                        Phone = c.Phone ?? "",
                        Email = c.Email ?? "",
                        Address = c.Address ?? ""
                    };
                }).ToList();

                // Statistics: all customers in database
                txtTotalCustomers.Text =
                    $"Total Customers: {rows.Count}";

                // Search name, phone or email
                string keyword = txtSearch.Text.Trim();

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    rows = rows.Where(c =>
                        c.FullName.Contains(
                            keyword,
                            StringComparison.OrdinalIgnoreCase) ||
                        c.Phone.Contains(
                            keyword,
                            StringComparison.OrdinalIgnoreCase) ||
                        c.Email.Contains(
                            keyword,
                            StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                dgCustomers.ItemsSource = rows;

                ClearSelection();

                // Restore the edited customer's selection
                if (selectCustomerId.HasValue)
                {
                    var selected = rows.FirstOrDefault(c =>
                        c.CustomerId == selectCustomerId.Value);

                    if (selected != null)
                    {
                        dgCustomers.SelectedItem = selected;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Loading Customers Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // 3. SELECT CUSTOMER
        private void dgCustomers_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            if (dgCustomers.SelectedItem is not CustomerRow row)
            {
                _selectedCustomerId = null;
                ClearFields();
                return;
            }

            _selectedCustomerId = row.CustomerId;

            txtFullName.Text = row.FullName;
            txtPhone.Text = row.Phone;
            txtEmail.Text = row.Email;
            txtAddress.Text = row.Address;
        }

        // 4. VALIDATE CUSTOMER INFORMATION
        private bool ValidateCustomer()
        {
            string fullName = txtFullName.Text.Trim();
            string phone = txtPhone.Text.Trim();
            string email = txtEmail.Text.Trim();
            string address = txtAddress.Text.Trim();

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

            if (fullName.Length > 150)
            {
                MessageBox.Show(
                    "Full name must not exceed 150 characters.");
                return false;
            }

            if (!Regex.IsMatch(phone, @"^[0-9]{10,20}$"))
            {
                MessageBox.Show(
                    "Phone must contain 10-20 digits only.",
                    "Validation Error");
                return false;
            }

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

            if (address.Length > 255)
            {
                MessageBox.Show(
                    "Address must not exceed 255 characters.");
                return false;
            }

            return true;
        }

        // 5. SAVE CHANGES
        private void btnSave_Click(
            object sender, RoutedEventArgs e)
        {
            if (!_selectedCustomerId.HasValue)
            {
                MessageBox.Show(
                    "Please select a customer first.");
                return;
            }

            if (!ValidateCustomer())
                return;

            try
            {
                using var db = new BookStoreDbContext();

                int customerId = _selectedCustomerId.Value;

                var customer = db.Customers.FirstOrDefault(
                    c => c.CustomerId == customerId);

                if (customer == null)
                {
                    MessageBox.Show(
                        "Customer no longer exists.");
                    LoadCustomers();
                    return;
                }

                customer.FullName = txtFullName.Text.Trim();
                customer.Phone = txtPhone.Text.Trim();
                customer.Email = txtEmail.Text.Trim();
                customer.Address = txtAddress.Text.Trim();

                db.SaveChanges();

                MessageBox.Show(
                    "Customer updated successfully!",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                LoadCustomers(customerId);
            }
            catch (DbUpdateException)
            {
                MessageBox.Show(
                    "Unable to update customer information.",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Update Customer Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // 6. SEARCH BUTTON
        private void btnSearch_Click(
            object sender, RoutedEventArgs e)
        {
            LoadCustomers();
        }

        // 7. RESET BUTTON
        private void btnReset_Click(
            object sender, RoutedEventArgs e)
        {
            txtSearch.Clear();
            LoadCustomers();
        }

        // 8. CLEAR SELECTION BUTTON
        private void btnClear_Click(
            object sender, RoutedEventArgs e)
        {
            ClearSelection();
        }

        private void ClearSelection()
        {
            dgCustomers.SelectedItem = null;
            _selectedCustomerId = null;
            ClearFields();
        }

        private void ClearFields()
        {
            txtFullName.Clear();
            txtPhone.Clear();
            txtEmail.Clear();
            txtAddress.Clear();
        }

        // 9. CLOSE
        private void btnClose_Click(
            object sender, RoutedEventArgs e)
        {
            Close();
        }

        // 10. DATAGRID DISPLAY MODEL
        public class CustomerRow
        {
            public int CustomerId { get; set; }

            public string FullName { get; set; } = "";

            public string Username { get; set; } = "";

            public string Phone { get; set; } = "";

            public string Email { get; set; } = "";

            public string Address { get; set; } = "";
        }
    }
}
