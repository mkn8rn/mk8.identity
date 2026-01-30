using System;
using System.Collections.Generic;
using System.Text;

namespace mk8.identity.Infrastructure.Models.Application
{
    public class PrivilegesDB
    {
        public Guid Id { get; set; }

        // link to user via membership (set automatically by EF when using navigation property)
        public Guid MembershipId { get; set; }
        public UserMembershipDB Membership { get; set; } = null!;

        // matrix accounts owned by this user
        public List<MatrixAccountDB> MatrixAccounts { get; set; } = [];

        // other privileges
        public required bool VotingRights { get; set; }
    }
}
