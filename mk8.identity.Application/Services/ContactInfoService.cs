using Microsoft.EntityFrameworkCore;
using mk8.identity.Contracts.Interfaces;
using mk8.identity.Contracts.Models;
using mk8.identity.Infrastructure.Contexts;
using mk8.identity.Infrastructure.Models.Application;

namespace mk8.identity.Application.Services
{
    public class ContactInfoService : IContactInfoService
    {
        private readonly ApplicationContext _applicationContext;

        public ContactInfoService(ApplicationContext applicationContext)
        {
            _applicationContext = applicationContext;
        }

        public async Task<ServiceResult<ContactInfoDTO>> GetContactInfoAsync(Guid userId)
        {
            var membership = await _applicationContext.Memberships
                .Include(m => m.ContactInfo)
                .FirstOrDefaultAsync(m => m.UserId == userId);

            if (membership == null)
                return ServiceResult<ContactInfoDTO>.Fail("User membership not found");

            if (membership.ContactInfo == null)
            {
                return ServiceResult<ContactInfoDTO>.Ok(new ContactInfoDTO
                {
                    Email = null,
                    Matrix = null,
                    UpdatedAt = null
                });
            }

            return ServiceResult<ContactInfoDTO>.Ok(new ContactInfoDTO
            {
                Email = membership.ContactInfo.Email,
                Matrix = membership.ContactInfo.Matrix,
                UpdatedAt = membership.ContactInfo.UpdatedAt
            });
        }

        public async Task<ServiceResult<ContactInfoDTO>> UpdateContactInfoAsync(Guid userId, ContactInfoUpdateDTO update)
        {
            var membership = await _applicationContext.Memberships
                .Include(m => m.ContactInfo)
                .FirstOrDefaultAsync(m => m.UserId == userId);

            if (membership == null)
                return ServiceResult<ContactInfoDTO>.Fail("User membership not found");

            if (membership.ContactInfo == null)
            {
                membership.ContactInfo = new ContactInfoDB
                {
                    Id = Guid.NewGuid(),
                    MembershipId = membership.Id,
                    Email = update.Email?.Trim(),
                    Matrix = update.Matrix?.Trim(),
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                _applicationContext.ContactInfos.Add(membership.ContactInfo);
            }
            else
            {
                membership.ContactInfo.Email = update.Email?.Trim();
                membership.ContactInfo.Matrix = update.Matrix?.Trim();
                membership.ContactInfo.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await _applicationContext.SaveChangesAsync();

            return ServiceResult<ContactInfoDTO>.Ok(new ContactInfoDTO
            {
                Email = membership.ContactInfo.Email,
                Matrix = membership.ContactInfo.Matrix,
                UpdatedAt = membership.ContactInfo.UpdatedAt
            });
        }

        public async Task<ServiceResult<bool>> HasContactInfoAsync(Guid userId)
        {
            var membership = await _applicationContext.Memberships
                .Include(m => m.ContactInfo)
                .FirstOrDefaultAsync(m => m.UserId == userId);

            if (membership == null)
                return ServiceResult<bool>.Fail("User membership not found");

            var hasInfo = membership.ContactInfo != null &&
                (!string.IsNullOrWhiteSpace(membership.ContactInfo.Email) ||
                 !string.IsNullOrWhiteSpace(membership.ContactInfo.Matrix));

            return ServiceResult<bool>.Ok(hasInfo);
        }
    }
}
