using mk8.identity.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace mk8.identity.Contracts.Interfaces
{
    public interface IMatrixAccountService
    {
        // admin actions
        Task<ServiceResult<MatrixAccountDTO>> CreateMatrixAccountAsync(Guid adminId, MatrixAccountCreateDTO account);
        Task<ServiceResult> DisableMatrixAccountAsync(Guid adminId, MatrixAccountDisableDTO disable);
        Task<ServiceResult> EnableMatrixAccountAsync(Guid adminId, Guid matrixAccountId);

        // queries
        Task<ServiceResult<MatrixAccountDTO>> GetByIdAsync(Guid matrixAccountId);
        Task<ServiceResult<List<MatrixAccountDTO>>> GetMatrixAccountsForUserAsync(Guid userId);
        Task<ServiceResult<List<MatrixAccountDTO>>> GetAllMatrixAccountsAsync(bool? isDisabled = null);
        Task<ServiceResult<List<MatrixAccountDTO>>> GetAccountsRequiringDisableAsync();

        // check if user has active matrix account
        Task<ServiceResult<bool>> UserHasActiveMatrixAccountAsync(Guid userId);
    }
}
