using Microsoft.AspNetCore.Identity;

namespace Services
{
    public interface IAuthService
    {
        Task<(bool Success, string[] Errors)> RegisterAsync(string username, string password);
        Task<string?> LoginAsync(string username, string password);
        Task<bool> IsEmailVerifiedAsync(string username);
        Task<bool> SetEmailVerifiedAsync(string username, string email);
    }
}
