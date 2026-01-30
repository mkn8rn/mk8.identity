using System;
using System.Collections.Generic;
using System.Text;

namespace mk8.identity.Contracts.Models
{
    public class PrivilegesDTO
    {
        public Guid Id { get; set; }
        public required Guid UserId { get; set; }
        public List<MatrixAccountDTO> MatrixAccounts { get; set; } = [];
        public required bool VotingRights { get; set; }
    }
}
