using Common.DTOs;
using Models;

namespace Application.Interfaces;

public interface IAccountService
{
    Task<PagedResponse<Account>> GetAccountsAsync(PagedRequest request, CancellationToken ct = default);
    Task<Account?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Account> CreateAsync(CreateAccountDto dto, CancellationToken ct = default);
    Task<Account> UpdateAsync(int id, UpdateAccountDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task AddRightsAsync(int accountId, List<int> rightIds, CancellationToken ct = default);
    Task RemoveRightsAsync(int accountId, List<int> rightIds, CancellationToken ct = default);
    Task<List<Rights>> GetAccountRightsAsync(int accountId, CancellationToken ct = default);
}

public interface IUserService
{
    Task<PagedResponse<User>> GetUsersAsync(PagedRequest request, CancellationToken ct = default);
    Task<User?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<User> CreateAsync(CreateUserDto dto, CancellationToken ct = default);
    Task<User> UpdateAsync(int id, UpdateUserDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public interface IAuthService
{
    Task<TokenResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default);
    Task<TokenResponseDto> RefreshAsync(string refreshToken, CancellationToken ct = default);
    Task RevokeAsync(string refreshToken, CancellationToken ct = default);
}
