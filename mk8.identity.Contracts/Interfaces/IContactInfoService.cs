using mk8.identity.Contracts.Models;

namespace mk8.identity.Contracts.Interfaces
{
    public interface IContactInfoService
    {
        Task<ServiceResult<ContactInfoDTO>> GetContactInfoAsync(Guid userId);
        Task<ServiceResult<ContactInfoDTO>> UpdateContactInfoAsync(Guid userId, ContactInfoUpdateDTO update);
        Task<ServiceResult<bool>> HasContactInfoAsync(Guid userId);
    }
}
