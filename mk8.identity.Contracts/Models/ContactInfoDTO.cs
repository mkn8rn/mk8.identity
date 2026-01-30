namespace mk8.identity.Contracts.Models
{
    public class ContactInfoDTO
    {
        public string? Email { get; set; }
        public string? Matrix { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }

        public bool HasContactInfo => !string.IsNullOrWhiteSpace(Email) || !string.IsNullOrWhiteSpace(Matrix);
    }

    public class ContactInfoUpdateDTO
    {
        public string? Email { get; set; }
        public string? Matrix { get; set; }
    }
}
