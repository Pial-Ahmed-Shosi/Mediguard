using System;
using System.ComponentModel.DataAnnotations;

namespace MediGuard.Models.ViewModels
{
    public class CategoryViewModels
    {
        [Required(ErrorMessage = "Category name is required.")]
        [MaxLength(100, ErrorMessage = "Category name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}
