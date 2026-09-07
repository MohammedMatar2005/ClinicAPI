using ClinicAPIBusiness.Models;
using System;
using System.Collections.Generic;

namespace ClinicAPIBusiness.Models;

public partial class Person
{
    public int PersonId { get; set; }

    public string FirstName { get; set; } = null!;

    public string? SecondName { get; set; }

    public string? ThirdName { get; set; }

    public string LastName { get; set; } = null!;


    public string FullName => $"{FirstName} {SecondName} {ThirdName} {LastName}".Replace("  ", " ").Trim();
    public DateOnly DateOfBirth { get; set; }

    public bool Gender { get; set; }

    public string Phone { get; set; } = null!;

    public string? Email { get; set; }

    public string? Address { get; set; }

    public string NationalNumber { get; set; } = null!;

    public virtual Patient? Patient { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
