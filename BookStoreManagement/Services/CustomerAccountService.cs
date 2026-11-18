
using System;
using System.Linq;
using System.Net.Mail;
using System.Text.RegularExpressions;
using BookStoreManagement.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookStoreManagement.Services
{
    public class CustomerAccountService
    {
        public void RegisterCustomer(
            string username,
            string password,
            string confirmPassword,
            string fullName,
            string phone,
            string email,
            string address)
        {
            // 1. NORMALIZE INPUT
            username = username?.Trim() ?? "";
            fullName = fullName?.Trim() ?? "";
            phone = phone?.Trim() ?? "";
            email = email?.Trim() ?? "";
            address = address?.Trim() ?? "";

            // 2. CHECK ALL REQUIRED FIELDS
            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(confirmPassword) ||
                string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(phone) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException(
                    "Please fill in all fields.");
            }

            // 3. VALIDATE USERNAME
            if (username.Length < 3 || username.Length > 50)
            {
                throw new ArgumentException(
                    "Username must contain 3-50 characters.");
            }

            // 4. VALIDATE PASSWORD
            if (password.Length < 8 || password.Length > 128)
            {
                throw new ArgumentException(
                    "Password must contain 8-128 characters.");
            }

            if (password != confirmPassword)
            {
                throw new ArgumentException(
                    "Passwords do not match.");
            }

            // 5. VALIDATE FULL NAME
            if (fullName.Length > 150)
            {
                throw new ArgumentException(
                    "Full name must not exceed 150 characters.");
            }

            // 6. VALIDATE PHONE: 10-20 DIGITS ONLY
            if (!Regex.IsMatch(phone, @"^[0-9]{10,20}$"))
            {
                throw new ArgumentException(
                    "Phone must contain 10-20 digits only.");
            }

            // 7. VALIDATE EMAIL
            if (email.Length > 150 ||
                !Regex.IsMatch(
                    email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                throw new ArgumentException(
                    "Please enter a valid email address.");
            }

            // 8. VALIDATE ADDRESS
            if (address.Length > 255)
            {
                throw new ArgumentException(
                    "Address must not exceed 255 characters.");
            }

            // 9. CONNECT TO DATABASE
            using var db = new BookStoreDbContext();

            // 10. CHECK DUPLICATE USERNAME
            bool exists = db.AppUsers.Any(u =>
                u.Username == username);

            if (exists)
            {
                throw new ArgumentException(
                    "Username already exists.");
            }

            // 11. BEGIN TRANSACTION
            using var transaction =
                db.Database.BeginTransaction();

            // 12. CREATE LOGIN ACCOUNT
            var user = new AppUser
            {
                Username = username,
                Role = "Customer"
            };

            var hasher = new PasswordHasher<AppUser>();

            user.PasswordHash =
                hasher.HashPassword(user, password);

            db.AppUsers.Add(user);
            db.SaveChanges();

            // 13. CREATE CUSTOMER PROFILE
            var customer = new Customer
            {
                FullName = fullName,
                Phone = phone,
                Email = email,
                Address = address,
                AppUserId = user.Id
            };

            db.Customers.Add(customer);
            db.SaveChanges();

            // 14. COMMIT ONLY AFTER BOTH INSERTS
            transaction.Commit();
        }
    }
}
