using System.ComponentModel.DataAnnotations;

namespace PROG7312_POE.Domain
{
    public class Attachment
    {
        public int Id { get; set; }

        public int IssueId { get; set; }
        public Issue Issue { get; set; } = null!;

        [MaxLength(255)]
        public string OriginalFileName { get; set; } = "";

        [MaxLength(255)]
        public string StoredFileName { get; set; } = "";

        [MaxLength(100)]
        public string ContentType { get; set; } = "";

        public long SizeBytes { get; set; }
    }
}
