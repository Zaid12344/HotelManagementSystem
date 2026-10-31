using HotelManagement.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Models.ViewModels;

public class ServiceListViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ServiceCategory Category { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
}

public class ServiceFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(100), Display(Name = "Service Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500), Display(Name = "Description")]
    public string? Description { get; set; }

    [Required, Display(Name = "Category")]
    public ServiceCategory Category { get; set; }

    [Required, Range(0, 1000000), Display(Name = "Price")]
    public decimal Price { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}