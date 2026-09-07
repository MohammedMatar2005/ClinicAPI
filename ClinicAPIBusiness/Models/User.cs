using System;
using System.Collections.Generic;

namespace ClinicAPIBusiness.Models;

public partial class User
{
    public int UserId { get; set; }

    public int PersonId { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public int RoleId { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? LastLoginDate { get; set; }

    public string? RefreshTokenHash { get; set; }

    public DateTime? RefreshTokenExpiresAt { get; set; }

    public DateTime? RefreshTokenRevokedAt { get; set; }

    public virtual Doctor? Doctor { get; set; }

    public virtual Person Person { get; set; } = null!;

    public virtual UserRole Role { get; set; } = null!;
}
