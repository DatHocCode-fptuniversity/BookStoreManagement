
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BookStoreManagement.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookStoreManagement
{
    public partial class MyOrdersWindow : Window
    {
        private readonly int _customerId;

        // 1. RECEIVE LOGGED-IN CUSTOMER
        public MyOrdersWindow(int customerId)
        {
            InitializeComponent();

            _customerId = customerId;
        }

        // 2. LOAD WINDOW
        private void Window_Loaded(
            object sender, RoutedEventArgs e)
        {
            LoadOrders();
        }

        // 3. LOAD ONLY THIS CUSTOMER'S ORDERS
        private void LoadOrders()
        {
            try
            {
                using var db = new BookStoreDbContext();

                var orders = db.Invoices
                    .AsNoTracking()
                    .Where(i => i.CustomerId == _customerId)
                    .OrderByDescending(i => i.InvoiceDate)
                    .ThenByDescending(i => i.InvoiceId)
                    .Select(i => new OrderItem
                    {
                        InvoiceId = i.InvoiceId,
                        InvoiceDate = i.InvoiceDate,
                        TotalAmount = i.TotalAmount
                    })
                    .ToList();

                dgOrders.ItemsSource = orders;

                dgOrderDetails.ItemsSource = null;
                txtOrderTotal.Text = "0 VND";

                if (orders.Count > 0)
                {
                    dgOrders.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Loading Orders Error");
            }
        }

        // 4. SELECT AN ORDER
        private void dgOrders_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            if (dgOrders.SelectedItem is not OrderItem order)
            {
                dgOrderDetails.ItemsSource = null;
                txtOrderTotal.Text = "0 VND";
                return;
            }

            LoadOrderDetails(order.InvoiceId);
        }

        // 5. LOAD ORDER DETAILS
        private void LoadOrderDetails(int invoiceId)
        {
            try
            {
                using var db = new BookStoreDbContext();

                // Check that the order belongs to this customer.
                var invoice = db.Invoices
                    .AsNoTracking()
                    .FirstOrDefault(i =>
                        i.InvoiceId == invoiceId &&
                        i.CustomerId == _customerId);

                if (invoice == null)
                {
                    dgOrderDetails.ItemsSource = null;
                    txtOrderTotal.Text = "0 VND";

                    MessageBox.Show(
                        "Order not found for this customer.");
                    return;
                }

                // Read invoice details and book titles.
                var details = (
                    from detail in db.InvoiceDetails.AsNoTracking()
                    join book in db.Books.AsNoTracking()
                        on detail.BookId equals book.BookId
                    where detail.InvoiceId == invoiceId
                    orderby detail.InvoiceDetailId
                    select new OrderDetailItem
                    {
                        BookTitle = book.Title,
                        Quantity = detail.Quantity,
                        UnitPrice = detail.UnitPrice
                    }
                ).ToList();

                dgOrderDetails.ItemsSource = details;

                txtOrderTotal.Text =
                    invoice.TotalAmount.ToString(
                        "N0", CultureInfo.CurrentCulture)
                    + " VND";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Loading Details Error");
            }
        }

        // 6. CLOSE WINDOW
        private void btnClose_Click(
            object sender, RoutedEventArgs e)
        {
            Close();
        }

        // 7. ORDER DISPLAY MODEL
        public class OrderItem
        {
            public int InvoiceId { get; set; }

            public DateTime InvoiceDate { get; set; }

            public decimal TotalAmount { get; set; }
        }

        // 8. ORDER DETAIL DISPLAY MODEL
        public class OrderDetailItem
        {
            public string BookTitle { get; set; } = "";

            public int Quantity { get; set; }

            public decimal UnitPrice { get; set; }

            public decimal Subtotal => Quantity * UnitPrice;
        }
    }
}
