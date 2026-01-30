using System;
using System.Collections.Generic;
using System.Text;

namespace mk8.identity.Infrastructure.Models.Application
{
    public class MatrixAccountDB
    {
        public Guid Id { get; set; }
        public required string AccountId { get; set; }
        public required string Username { get; set; }
        public required DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public required bool IsDisabled { get; set; }
        public DateTimeOffset? DisabledAt { get; set; }

        // link to owner's privileges
        public required Guid PrivilegesId { get; set; }
        public PrivilegesDB Privileges { get; set; } = null!;

        // admin who created
        public Guid? CreatedByMembershipId { get; set; }
        public UserMembershipDB? CreatedBy { get; set; }

        // admin who disabled
        public Guid? DisabledByMembershipId { get; set; }
        public UserMembershipDB? DisabledBy { get; set; }
    }
}
