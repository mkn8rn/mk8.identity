using System;
using System.Collections.Generic;
using System.Text;

namespace mk8.identity.Contracts.Models
{
    public enum RoleTypeDTO
    {
        None = 0,
        Administrator = 1,
        Assessor = 2,
        Moderator = 101,
        Support = 102,
    }

    public class RoleDTO
    {
        public Guid Id { get; set; }
        public required RoleTypeDTO RoleName { get; set; }
    }
}
