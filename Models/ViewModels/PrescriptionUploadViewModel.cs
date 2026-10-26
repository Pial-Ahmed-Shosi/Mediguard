using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace MediGuard.Models.ViewModels
{
    public class PrescriptionUploadViewModel
    {
        /// <summary>The order that this prescription is linked to.</summary>
        [Required]
        public Guid OrderId { get; set; }

        /// <summary>Optional notes for the inspecting pharmacist.</summary>
        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
        [Display(Name = "Notes for Pharmacist (optional)")]
        public string? PharmacistNotes { get; set; }

        /// <summary>The uploaded prescription file (JPG, PNG, or PDF - max 5 MB).</summary>
        [Required(ErrorMessage = "Please select a prescription file.")]
        [Display(Name = "Prescription File")]
        public IFormFile? PrescriptionFile { get; set; }

        // Display helpers
        public string OrderReference => OrderId == Guid.Empty ? "-" : $"#{OrderId.ToString().Substring(0, 8).ToUpper()}";
    }
}
