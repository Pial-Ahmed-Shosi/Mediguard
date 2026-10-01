using System;
using System.ComponentModel.DataAnnotations;

namespace MediGuard.Models.ViewModels
{
    public class CreateManufacturerViewModel
    {
        [Required(ErrorMessage = "Manufacturer name is required.")]
        [MaxLength(150, ErrorMessage = "Manufacturer name cannot exceed 150 characters.")]
        public string Name { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        [MaxLength(255)]
        public string? ContactEmail { get; set; }

        [MaxLength(30)]
        public string? Phone { get; set; }

        public string? Address { get; set; }
    }

    public class EditManufacturerViewModel
    {
        [Required]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Manufacturer name is required.")]
        [MaxLength(150, ErrorMessage = "Manufacturer name cannot exceed 150 characters.")]
        public string Name { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        [MaxLength(255)]
        public string? ContactEmail { get; set; }

        [MaxLength(30)]
        public string? Phone { get; set; }

        public string? Address { get; set; }
    }

    public class QuickCreateManufacturerDto
    {
        [Required(ErrorMessage = "Manufacturer name is required.")]
        [MaxLength(150, ErrorMessage = "Manufacturer name cannot exceed 150 characters.")]
        public string Name { get; set; } = string.Empty;

        [EmailAddress]
        [MaxLength(255)]
        public string? ContactEmail { get; set; }

        [MaxLength(30)]
        public string? Phone { get; set; }

        public string? Address { get; set; }
    }
}
