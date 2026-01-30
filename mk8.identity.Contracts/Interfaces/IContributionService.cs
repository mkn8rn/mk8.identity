using mk8.identity.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace mk8.identity.Contracts.Interfaces
{
    public interface IContributionService
    {
        // member actions
        Task<ServiceResult<ContributionDTO>> SubmitContributionAsync(Guid userId, ContributionSubmitDTO contribution);
        Task<ServiceResult<List<ContributionDTO>>> GetUserContributionsAsync(Guid userId);

        // assessor actions
        Task<ServiceResult<ContributionDTO>> CreateAndValidateContributionAsync(Guid assessorId, ContributionCreateByAssessorDTO contribution);
        Task<ServiceResult<ContributionDTO>> ValidateContributionAsync(Guid assessorId, ContributionValidateDTO validation);
        Task<ServiceResult<List<ContributionDTO>>> GetPendingContributionsAsync();
        Task<ServiceResult<List<ContributionDTO>>> GetContributionsByMonthAsync(int month, long year);

        // auto-verification / auto-assignment
        Task<ServiceResult<List<ContributionDTO>>> ProcessGitHubSubscriptionsAsync();
        Task<ServiceResult<ContributionDTO>> AutoVerifyContributionAsync(Guid userId, ContributionTypeDTO type, int month, long year, string externalReference);
        Task<ServiceResult<int>> AssignRoleBasedContributionsAsync();

        // queries
        Task<ServiceResult<ContributionDTO>> GetByIdAsync(Guid contributionId);
        Task<ServiceResult<List<ContributionDTO>>> GetAllContributionsAsync(int? month = null, long? year = null, ContributionStatusDTO? status = null);
    }
}
