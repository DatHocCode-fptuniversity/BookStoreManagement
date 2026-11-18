using System;
using System.Collections.Generic;

namespace BookStoreManagement.Entities;

public partial class AppUser
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Role { get; set; } = null!;
}
