using Microsoft.AspNetCore.Identity;
using Models;

namespace Services.Interface
{
    public interface IAuthService
    {
        Task<(bool Success, string[] Errors)> RegisterAsync(string username, string password);
        Task<string?> LoginAsync(string username, string password);
        Task<bool> IsEmailVerifiedAsync(string username);
        Task<bool> SetEmailVerifiedAsync(string username, string email);
        Task<ApplicationUser?> FindByEmailAsync(string email);
    }
}
