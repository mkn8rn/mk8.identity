namespace mk8.identity.Infrastructure.Models.Application
{
    public class ContactInfoDB
    {
        public Guid Id { get; set; }
        public required Guid MembershipId { get; set; }
        public string? Email { get; set; }
        public string? Matrix { get; set; }
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation
        public UserMembershipDB? Membership { get; set; }
    }
}
