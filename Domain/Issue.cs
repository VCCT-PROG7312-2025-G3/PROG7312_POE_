using System.ComponentModel.DataAnnotations;

namespace PROG7312_POE.Domain
{
    public class Issue
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required, StringLength(200)]
        public string Location { get; set; } = "";

        [Required]
        public IssueCategory Category { get; set; }

        [Required, StringLength(4000)]
        public string Description { get; set; } = "";

        public bool WillingForFollowUp { get; set; }
        public string? SubmissionExperience { get; set; } // "Easy" / "Not easy"

        public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    }
}
