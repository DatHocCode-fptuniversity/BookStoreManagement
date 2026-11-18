
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BookStoreManagement.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookStoreManagement
{
    public partial class AdminOrdersWindow : Window
    {
        public AdminOrdersWindow()
        {
            InitializeComponent();
        }

        // 1. LOAD WINDOW
        private void Window_Loaded(
            object sender, RoutedEventArgs e)
        {
            LoadOrders();
        }

        // 2. LOAD AND FILTER ALL ORDERS
        private void LoadOrders()
        {
            try
            {
                using var db = new BookStoreDbContext();

                string keyword = txtSearch.Text.Trim();

                var query = db.Invoices.AsNoTracking();

                // Search by customer name or invoice ID
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    if (int.TryParse(keyword, out int invoiceId))
                    {
                        query = query.Where(i =>
                            i.InvoiceId == invoiceId ||
                            i.Customer.FullName.Contains(keyword));
                    }
                    else
                    {
                        query = query.Where(i =>
                            i.Customer.FullName.Contains(keyword));
                    }
                }

                // Filter from date
                if (dpFrom.SelectedDate.HasValue)
                {
                    DateTime from = dpFrom.SelectedDate.Value.Date;

                    query = query.Where(i =>
                        i.InvoiceDate >= from);
                }

                // Filter to date, inclusive
                if (dpTo.SelectedDate.HasValue)
                {
                    DateTime toExclusive =
                        dpTo.SelectedDate.Value.Date.AddDays(1);

                    query = query.Where(i =>
                        i.InvoiceDate < toExclusive);
                }

                // Read all matching invoices
                var orders = query
                    .OrderByDescending(i => i.InvoiceDate)
                    .ThenByDescending(i => i.InvoiceId)
                    .Select(i => new AdminOrderItem
                    {
                        InvoiceId = i.InvoiceId,
                        CustomerName = i.Customer.FullName,
                        InvoiceDate = i.InvoiceDate,
                        TotalAmount = i.TotalAmount
                    })
                    .ToList();

                dgOrders.ItemsSource = orders;

                // Statistics for filtered results
                txtTotalOrders.Text = orders.Count.ToString();

                decimal revenue = orders.Sum(o => o.TotalAmount);

                txtTotalRevenue.Text =
                    revenue.ToString(
                        "N0", CultureInfo.CurrentCulture)
                    + " VND";

                dgDetails.ItemsSource = null;
                txtSelectedTotal.Text =
                    "Selected Invoice: 0 VND";

                if (orders.Count > 0)
                {
                    dgOrders.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Loading Orders Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // 3. SELECT AN INVOICE
        private void dgOrders_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            if (dgOrders.SelectedItem is not AdminOrderItem order)
            {
                dgDetails.ItemsSource = null;
                txtSelectedTotal.Text =
                    "Selected Invoice: 0 VND";
                return;
            }

            LoadInvoiceDetails(order.InvoiceId);
        }

        // 4. LOAD INVOICE DETAILS
        private void LoadInvoiceDetails(int invoiceId)
        {
            try
            {
                using var db = new BookStoreDbContext();

                var invoice = db.Invoices
                    .AsNoTracking()
                    .FirstOrDefault(i =>
                        i.InvoiceId == invoiceId);

                if (invoice == null)
                {
                    dgDetails.ItemsSource = null;
                    txtSelectedTotal.Text =
                        "Selected Invoice: 0 VND";
                    return;
                }

                var details = (
                    from detail in db.InvoiceDetails.AsNoTracking()
                    join book in db.Books.AsNoTracking()
                        on detail.BookId equals book.BookId
                    where detail.InvoiceId == invoiceId
                    orderby detail.InvoiceDetailId
                    select new AdminOrderDetailItem
                    {
                        BookTitle = book.Title,
                        Quantity = detail.Quantity,
                        UnitPrice = detail.UnitPrice
                    }
                ).ToList();

                dgDetails.ItemsSource = details;

                txtSelectedTotal.Text =
                    "Selected Invoice: " +
                    invoice.TotalAmount.ToString(
                        "N0", CultureInfo.CurrentCulture)
                    + " VND";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Invoice Details Error");
            }
        }

        // 5. SEARCH BUTTON
        private void btnSearch_Click(
            object sender, RoutedEventArgs e)
        {
            if (dpFrom.SelectedDate.HasValue &&
                dpTo.SelectedDate.HasValue &&
                dpFrom.SelectedDate.Value.Date >
                dpTo.SelectedDate.Value.Date)
            {
                MessageBox.Show(
                    "From Date cannot be later than To Date.");
                return;
            }

            LoadOrders();
        }

        // 6. RESET BUTTON
        private void btnReset_Click(
            object sender, RoutedEventArgs e)
        {
            txtSearch.Clear();
            dpFrom.SelectedDate = null;
            dpTo.SelectedDate = null;

            LoadOrders();
        }

        // 7. CLOSE WINDOW
        private void btnClose_Click(
            object sender, RoutedEventArgs e)
        {
            Close();
        }

        // 8. INVOICE DISPLAY MODEL
        public class AdminOrderItem
        {
            public int InvoiceId { get; set; }

            public string CustomerName { get; set; } = "";

            public DateTime InvoiceDate { get; set; }

            public decimal TotalAmount { get; set; }
        }

        // 9. INVOICE DETAIL DISPLAY MODEL
        public class AdminOrderDetailItem
        {
            public string BookTitle { get; set; } = "";

            public int Quantity { get; set; }

            public decimal UnitPrice { get; set; }

            public decimal Subtotal => Quantity * UnitPrice;
        }
    }
}
