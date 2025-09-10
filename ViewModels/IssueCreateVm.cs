using System.ComponentModel.DataAnnotations;
using PROG7312_POE.Domain;

namespace PROG7312_POE.ViewModels
{
    public class IssueCreateVm
    {
        [Required, StringLength(200)]
        public string Location { get; set; } = "";

        [Required]
        public IssueCategory Category { get; set; }

        [Required, StringLength(4000)]
        public string Description { get; set; } = "";

        public bool WillingForFollowUp { get; set; }
        public string? SubmissionExperience { get; set; } // "Easy" / "Not easy"
    }
}
