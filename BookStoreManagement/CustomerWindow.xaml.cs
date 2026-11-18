
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BookStoreManagement.Entities;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace BookStoreManagement
{
    public partial class CustomerWindow : Window
    {
        // 1. LOGGED-IN CUSTOMER
        private readonly int _appUserId;
        private readonly int _customerId;

        // Shopping cart stored temporarily in memory
        private readonly List<CartItem> _cart = new();

        public CustomerWindow(int appUserId)
        {
            InitializeComponent();

            _appUserId = appUserId;

            using var db = new BookStoreDbContext();

            var customer = db.Customers
                .AsNoTracking()
                .FirstOrDefault(c => c.AppUserId == _appUserId);

            if (customer == null)
            {
                throw new InvalidOperationException(
                    "Customer profile not found for this account.");
            }

            _customerId = customer.CustomerId;
        }

        // 2. LOAD WINDOW
        private void Window_Loaded(
            object sender, RoutedEventArgs e)
        {
            try
            {
                LoadCategories();
                LoadBooks();
                RefreshCart();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Loading Error");
            }
        }

        // 3. LOAD CATEGORIES
        private void LoadCategories()
        {
            using var db = new BookStoreDbContext();

            var categories = db.Categories
                .AsNoTracking()
                .OrderBy(c => c.CategoryName)
                .ToList();

            var filters = new List<Category>
            {
                new Category
                {
                    CategoryId = 0,
                    CategoryName = "All"
                }
            };

            filters.AddRange(categories);

            cbCategory.ItemsSource = filters;
            cbCategory.SelectedValue = 0;
        }

        // 4. LOAD BOOKS
        private void LoadBooks()
        {
            using var db = new BookStoreDbContext();

            string keyword = txtSearch.Text.Trim();

            int categoryId =
                cbCategory.SelectedValue is int id ? id : 0;

            var query = db.Books
                .AsNoTracking()
                .Include(b => b.Category)
                .Include(b => b.Author)
                .Include(b => b.Publisher)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(b =>
                    b.Title.Contains(keyword));
            }

            if (categoryId > 0)
            {
                query = query.Where(b =>
                    b.CategoryId == categoryId);
            }

            dgBooks.ItemsSource = query
                .OrderBy(b => b.Title)
                .ToList();
        }

        // 5. SEARCH
        private void btnSearch_Click(
            object sender, RoutedEventArgs e)
        {
            try
            {
                LoadBooks();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Search Error");
            }
        }

        // 6. RESET
        private void btnReset_Click(
            object sender, RoutedEventArgs e)
        {
            txtSearch.Clear();
            cbCategory.SelectedValue = 0;

            btnSearch_Click(sender, e);
        }

        // 7. SELECT BOOK
        private void dgBooks_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            if (dgBooks.SelectedItem is Book book)
            {
                Title = $"Book Store - {book.Title}";
            }
            else
            {
                Title = "Book Store - Customer";
            }
        }

        // 8. ADD BOOK TO CART
        private void btnAddToCart_Click(
            object sender, RoutedEventArgs e)
        {
            if (dgBooks.SelectedItem is not Book book)
            {
                MessageBox.Show("Please select a book.");
                return;
            }

            if (!int.TryParse(txtQuantity.Text, out int quantity)
                || quantity <= 0)
            {
                MessageBox.Show(
                    "Quantity must be greater than 0.");
                return;
            }

            try
            {
                using var db = new BookStoreDbContext();

                // Check latest stock in database
                var currentBook = db.Books
                    .AsNoTracking()
                    .FirstOrDefault(b => b.BookId == book.BookId);

                if (currentBook == null)
                {
                    MessageBox.Show("Book no longer exists.");
                    LoadBooks();
                    return;
                }

                var cartItem = _cart.FirstOrDefault(
                    c => c.BookId == book.BookId);

                int existingQuantity = cartItem?.Quantity ?? 0;

                if ((long)existingQuantity + quantity >
                    currentBook.Quantity)
                {
                    MessageBox.Show(
                        "Not enough stock available.");
                    return;
                }

                if (cartItem == null)
                {
                    _cart.Add(new CartItem
                    {
                        BookId = currentBook.BookId,
                        Title = currentBook.Title,
                        UnitPrice = currentBook.Price,
                        Quantity = quantity
                    });
                }
                else
                {
                    cartItem.Quantity += quantity;
                    cartItem.UnitPrice = currentBook.Price;
                }

                RefreshCart();

                txtQuantity.Text = "1";

                MessageBox.Show(
                    "Book added to cart successfully!");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Cart Error");
            }
        }

        // 9. INCREASE QUANTITY
        private void btnIncrease_Click(
            object sender, RoutedEventArgs e)
        {
            if (dgCart.SelectedItem is not CartItem item)
            {
                MessageBox.Show(
                    "Please select an item in your cart.");
                return;
            }

            try
            {
                using var db = new BookStoreDbContext();

                var book = db.Books
                    .AsNoTracking()
                    .FirstOrDefault(b => b.BookId == item.BookId);

                if (book == null)
                {
                    MessageBox.Show("Book no longer exists.");
                    return;
                }

                if (item.Quantity >= book.Quantity)
                {
                    MessageBox.Show(
                        "Not enough stock available.");
                    return;
                }

                item.Quantity++;
                item.UnitPrice = book.Price;

                RefreshCart();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Cart Error");
            }
        }

        // 10. DECREASE QUANTITY
        private void btnDecrease_Click(
            object sender, RoutedEventArgs e)
        {
            if (dgCart.SelectedItem is not CartItem item)
            {
                MessageBox.Show(
                    "Please select an item in your cart.");
                return;
            }

            if (item.Quantity > 1)
            {
                item.Quantity--;
            }
            else
            {
                _cart.Remove(item);
            }

            RefreshCart();
        }

        // 11. REMOVE ITEM
        private void btnRemove_Click(
            object sender, RoutedEventArgs e)
        {
            if (dgCart.SelectedItem is not CartItem item)
            {
                MessageBox.Show(
                    "Please select an item to remove.");
                return;
            }

            _cart.Remove(item);

            RefreshCart();
        }

        // 12. REFRESH CART AND TOTAL
        private void RefreshCart()
        {
            dgCart.ItemsSource = null;
            dgCart.ItemsSource = _cart;

            decimal total = _cart.Sum(item =>
                item.Subtotal);

            txtTotal.Text = total.ToString(
                "N0", CultureInfo.CurrentCulture) + " VND";

            // Enable Checkout only when cart is not empty
            btnCheckout.IsEnabled = _cart.Count > 0;
        }
        // 13. CHECKOUT
        private void btnCheckout_Click(
            object sender, RoutedEventArgs e)
        {
            // 1. Check cart
            if (_cart.Count == 0)
            {
                MessageBox.Show("Your cart is empty.");
                return;
            }

            decimal cartTotal = _cart.Sum(item => item.Subtotal);

            // 2. Confirm checkout
            var confirm = MessageBox.Show(
                $"Total: {cartTotal:N0} VND\n" +
                "Do you want to place this order?",
                "Confirm Checkout",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            btnCheckout.IsEnabled = false;

            try
            {
                using var db = new BookStoreDbContext();

                // Serializable transaction protects the
                // stock checks and updates as one operation.
                using var transaction =
                    db.Database.BeginTransaction(
                        IsolationLevel.Serializable);

                // 3. Verify customer
                bool customerExists = db.Customers.Any(c =>
                    c.CustomerId == _customerId &&
                    c.AppUserId == _appUserId);

                if (!customerExists)
                {
                    throw new InvalidOperationException(
                        "Customer account is not valid.");
                }

                // 4. Get latest books from database
                var bookIds = _cart
                    .Select(c => c.BookId)
                    .ToList();

                var books = db.Books
                    .Where(b => bookIds.Contains(b.BookId))
                    .OrderBy(b => b.BookId)
                    .ToList();

                decimal totalAmount = 0;

                // 5. Validate stock and current prices
                foreach (var item in _cart)
                {
                    var book = books.FirstOrDefault(
                        b => b.BookId == item.BookId);

                    if (book == null)
                    {
                        throw new InvalidOperationException(
                            $"Book '{item.Title}' no longer exists.");
                    }

                    if (item.Quantity <= 0 ||
                        item.Quantity > book.Quantity)
                    {
                        throw new InvalidOperationException(
                            $"Not enough stock for '{book.Title}'. " +
                            $"Available: {book.Quantity}");
                    }

                    // Do not charge a changed price
                    // without customer confirmation.
                    if (book.Price != item.UnitPrice)
                    {
                        throw new InvalidOperationException(
                            $"Price of '{book.Title}' has changed. " +
                            "Please remove it and add it again.");
                    }

                    totalAmount = checked(
                        totalAmount +
                        (book.Price * item.Quantity));
                }

                // 6. Create invoice
                var invoice = new Invoice
                {
                    CustomerId = _customerId,
                    InvoiceDate = DateTime.Now,
                    TotalAmount = totalAmount
                };

                db.Invoices.Add(invoice);
                db.SaveChanges();

                // 7. Create invoice details and reduce stock
                foreach (var item in _cart)
                {
                    var book = books.First(
                        b => b.BookId == item.BookId);

                    var detail = new InvoiceDetail
                    {
                        InvoiceId = invoice.InvoiceId,
                        BookId = book.BookId,
                        Quantity = item.Quantity,
                        UnitPrice = book.Price
                    };

                    db.InvoiceDetails.Add(detail);

                    book.Quantity -= item.Quantity;
                }

                // 8. Save details and stock changes
                db.SaveChanges();

                // 9. Commit everything
                transaction.Commit();

                // 10. Clear cart after successful checkout
                _cart.Clear();
                RefreshCart();
                LoadBooks();

                MessageBox.Show(
                    $"Order placed successfully!\n" +
                    $"Invoice ID: {invoice.InvoiceId}\n" +
                    $"Total: {totalAmount:N0} VND",
                    "Checkout Successful",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Checkout failed.\n{ex.Message}\n\n" +
                    "Your cart has been kept.",
                    "Checkout Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                btnCheckout.IsEnabled = _cart.Count > 0;
            }
        }

        private void btnMyProfile_Click(
            object sender, RoutedEventArgs e)
        {
            ProfileWindow profileWindow =
                new ProfileWindow(_appUserId);

            profileWindow.Owner = this;

            profileWindow.ShowDialog();
        }

        private void btnMyOrders_Click(object sender, RoutedEventArgs e)
        {
            MyOrdersWindow ordersWindow =
                new MyOrdersWindow(_customerId);

            ordersWindow.Owner = this;

            ordersWindow.ShowDialog();
        }
        // 14. LOGOUT
        private void btnLogout_Click(
            object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show(
                "Are you sure you want to logout?",
                "Confirm Logout",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            LoginWindow loginWindow = new LoginWindow();

            Application.Current.MainWindow = loginWindow;
            loginWindow.Show();

            Close();
        }

        // 15. CART ITEM MODEL
        public class CartItem
        {
            public int BookId { get; set; }

            public string Title { get; set; } = "";

            public decimal UnitPrice { get; set; }

            public int Quantity { get; set; }

            public decimal Subtotal
            {
                get
                {
                    return UnitPrice * Quantity;
                }
            }
        }
    }
}
