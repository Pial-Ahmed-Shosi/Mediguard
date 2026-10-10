using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace MediGuard.Models.ViewModels
{
    public class PrescriptionUploadViewModel
    {
        [Required]
        public Guid OrderId { get; set; }

        public string? OrderNumber { get; set; }

        [Required(ErrorMessage = "Please upload or drop a prescription document.")]
        [Display(Name = "Prescription Document")]
        public IFormFile? PrescriptionFile { get; set; }

        [Display(Name = "Notes for Inspecting Pharmacist (Optional)")]
        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
        public string? Notes { get; set; }
    }
}