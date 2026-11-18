
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
    public partial class MainWindow : Window
    {
        private int selectedBookId = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        // 1. LOAD DATA WHEN WINDOW OPENS
        private void Window_Loaded(
            object sender, RoutedEventArgs e)
        {
            try
            {
                LoadCategories();
                LoadBooks();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Loading Error");
            }
        }

        // 2. LOAD CATEGORIES
        private void LoadCategories()
        {
            using var db = new BookStoreDbContext();

            var categories = db.Categories
                .AsNoTracking()
                .OrderBy(c => c.CategoryName)
                .ToList();

            cbCategory.ItemsSource = categories;

            var filterCategories = new List<Category>
            {
                new Category
                {
                    CategoryId = 0,
                    CategoryName = "All"
                }
            };

            filterCategories.AddRange(categories);

            cbFilterCategory.ItemsSource = filterCategories;
            cbFilterCategory.SelectedValue = 0;
        }

        // 3. LOAD BOOKS
        private void LoadBooks()
        {
            using var db = new BookStoreDbContext();

            string keyword = txtSearch.Text.Trim();

            int categoryId =
                cbFilterCategory.SelectedValue is int id
                ? id : 0;

            var query = db.Books.AsNoTracking();

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

            var books = query
                .OrderBy(b => b.BookId)
                .Select(b => new BookListItem
                {
                    BookId = b.BookId,
                    Title = b.Title,
                    Price = b.Price,
                    Quantity = b.Quantity,

                    CategoryId = b.CategoryId,
                    AuthorId = b.AuthorId,
                    PublisherId = b.PublisherId,

                    CategoryName = b.Category.CategoryName,
                    AuthorName = b.Author.AuthorName,
                    PublisherName = b.Publisher.PublisherName
                })
                .ToList();

            dgBooks.ItemsSource = books;
        }

        // 4. SEARCH
        private void btnSearch_Click(
            object sender, RoutedEventArgs e)
        {
            try
            {
                ClearForm();
                LoadBooks();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Search Error");
            }
        }

        // 5. RESET FILTER
        private void btnReset_Click(
            object sender, RoutedEventArgs e)
        {
            txtSearch.Clear();
            cbFilterCategory.SelectedValue = 0;

            btnSearch_Click(sender, e);
        }

        // 6. SELECT BOOK
        private void dgBooks_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            if (dgBooks.SelectedItem is not BookListItem book)
                return;

            selectedBookId = book.BookId;

            txtTitle.Text = book.Title;
            txtPrice.Text = book.Price.ToString(
                CultureInfo.CurrentCulture);
            txtQuantity.Text = book.Quantity.ToString();

            cbCategory.SelectedValue = book.CategoryId;

            // Author and Publisher are now TextBoxes
            txtAuthor.Text = book.AuthorName;
            txtPublisher.Text = book.PublisherName;
        }

        // 7. VALIDATE FORM
        private bool TryReadForm(
            out string title,
            out decimal price,
            out int quantity,
            out int categoryId,
            out string authorName,
            out string publisherName)
        {
            title = txtTitle.Text.Trim();
            authorName = txtAuthor.Text.Trim();
            publisherName = txtPublisher.Text.Trim();

            price = 0;
            quantity = 0;
            categoryId = 0;

            if (title.Length == 0 || title.Length > 200)
            {
                MessageBox.Show(
                    "Book title must contain 1-200 characters.");
                return false;
            }

            if (!decimal.TryParse(
                    txtPrice.Text,
                    NumberStyles.Number,
                    CultureInfo.CurrentCulture,
                    out price)
                || price < 0
                || price >= 10000000000000000m
                || decimal.Round(price, 2) != price)
            {
                MessageBox.Show(
                    "Enter a valid non-negative price " +
                    "with at most 2 decimal places.");
                return false;
            }

            if (!int.TryParse(
                    txtQuantity.Text, out quantity)
                || quantity < 0)
            {
                MessageBox.Show(
                    "Quantity must be a non-negative integer.");
                return false;
            }

            if (cbCategory.SelectedValue is not int id
                || id <= 0)
            {
                MessageBox.Show("Please select a category.");
                return false;
            }

            categoryId = id;

            if (authorName.Length == 0 || authorName.Length > 150)
            {
                MessageBox.Show(
                    "Author name must contain 1-150 characters.");
                return false;
            }

            if (publisherName.Length == 0 ||
                publisherName.Length > 150)
            {
                MessageBox.Show(
                    "Publisher name must contain 1-150 characters.");
                return false;
            }

            return true;
        }

        // 8. FIND OR CREATE AUTHOR
        private static Author GetOrCreateAuthor(
            BookStoreDbContext db, string name)
        {
            var author = db.Authors
                .FirstOrDefault(a => a.AuthorName == name);

            if (author != null)
                return author;

            author = new Author
            {
                AuthorName = name
            };

            db.Authors.Add(author);
            db.SaveChanges();

            return author;
        }

        // 9. FIND OR CREATE PUBLISHER
        private static Publisher GetOrCreatePublisher(
            BookStoreDbContext db, string name)
        {
            var publisher = db.Publishers
                .FirstOrDefault(p => p.PublisherName == name);

            if (publisher != null)
                return publisher;

            publisher = new Publisher
            {
                PublisherName = name
            };

            db.Publishers.Add(publisher);
            db.SaveChanges();

            return publisher;
        }

        // 10. ADD BOOK
        private void btnAdd_Click(
            object sender, RoutedEventArgs e)
        {
            if (!TryReadForm(
                out var title,
                out var price,
                out var quantity,
                out var categoryId,
                out var authorName,
                out var publisherName))
            {
                return;
            }

            try
            {
                using var db = new BookStoreDbContext();
                using var transaction =
                    db.Database.BeginTransaction();

                var author = GetOrCreateAuthor(
                    db, authorName);

                var publisher = GetOrCreatePublisher(
                    db, publisherName);

                var book = new Book
                {
                    Title = title,
                    Price = price,
                    Quantity = quantity,
                    CategoryId = categoryId,
                    AuthorId = author.AuthorId,
                    PublisherId = publisher.PublisherId
                };

                db.Books.Add(book);
                db.SaveChanges();

                transaction.Commit();

                ClearForm();
                LoadBooks();

                MessageBox.Show(
                    "Book added successfully!");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Add Error");
            }
        }

        // 11. UPDATE BOOK
        private void btnUpdate_Click(
            object sender, RoutedEventArgs e)
        {
            if (selectedBookId == 0)
            {
                MessageBox.Show(
                    "Please select a book to update.");
                return;
            }

            if (!TryReadForm(
                out var title,
                out var price,
                out var quantity,
                out var categoryId,
                out var authorName,
                out var publisherName))
            {
                return;
            }

            try
            {
                using var db = new BookStoreDbContext();
                using var transaction =
                    db.Database.BeginTransaction();

                var book = db.Books.Find(selectedBookId);

                if (book == null)
                {
                    MessageBox.Show("Book not found.");
                    return;
                }

                var author = GetOrCreateAuthor(
                    db, authorName);

                var publisher = GetOrCreatePublisher(
                    db, publisherName);

                book.Title = title;
                book.Price = price;
                book.Quantity = quantity;
                book.CategoryId = categoryId;
                book.AuthorId = author.AuthorId;
                book.PublisherId = publisher.PublisherId;

                db.SaveChanges();
                transaction.Commit();

                ClearForm();
                LoadBooks();

                MessageBox.Show(
                    "Book updated successfully!");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Update Error");
            }
        }

        // 12. DELETE BOOK
        private void btnDelete_Click(
            object sender, RoutedEventArgs e)
        {
            if (selectedBookId == 0)
            {
                MessageBox.Show(
                    "Please select a book to delete.");
                return;
            }

            var confirm = MessageBox.Show(
                "Are you sure you want to delete this book?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                using var db = new BookStoreDbContext();

                var book = db.Books.Find(selectedBookId);

                if (book == null)
                {
                    MessageBox.Show("Book not found.");
                    return;
                }

                db.Books.Remove(book);
                db.SaveChanges();

                ClearForm();
                LoadBooks();

                MessageBox.Show(
                    "Book deleted successfully!");
            }
            catch (DbUpdateException)
            {
                MessageBox.Show(
                    "Cannot delete this book because " +
                    "it may be referenced by invoices.",
                    "Delete Error");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Delete Error");
            }
        }

        // 13. CLEAR FORM BUTTON
        private void btnClear_Click(
            object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        // 14. CLEAR FORM
        private void ClearForm()
        {
            selectedBookId = 0;
            dgBooks.SelectedItem = null;

            txtTitle.Clear();
            txtPrice.Clear();
            txtQuantity.Clear();

            txtAuthor.Clear();
            txtPublisher.Clear();

            cbCategory.SelectedIndex = -1;
        }

        private void btnCustomerManagement_Click(
            object sender, RoutedEventArgs e)
        {
            CustomerManagementWindow customerWindow =
                new CustomerManagementWindow();

            customerWindow.Owner = this;

            customerWindow.ShowDialog();
        }

        private void btnUserManagement_Click(
            object sender, RoutedEventArgs e)
        {
            UserManagementWindow userWindow =
                new UserManagementWindow();

            userWindow.Owner = this;

            userWindow.ShowDialog();
        }

        private void btnOrderManagement_Click(
            object sender, RoutedEventArgs e)
        {
            AdminOrdersWindow ordersWindow = new AdminOrdersWindow();

            ordersWindow.Owner = this;

            ordersWindow.ShowDialog();
        }

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

            this.Close();
        }

        // 15. DATAGRID DISPLAY MODEL
        public class BookListItem
        {
            public int BookId { get; set; }

            public string Title { get; set; } = "";

            public decimal Price { get; set; }

            public int Quantity { get; set; }

            public int CategoryId { get; set; }

            public int AuthorId { get; set; }

            public int PublisherId { get; set; }

            public string CategoryName { get; set; } = "";

            public string AuthorName { get; set; } = "";

            public string PublisherName { get; set; } = "";
        }
    }
}
