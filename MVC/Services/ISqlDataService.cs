// ISqlDataService.cs
using ABCRetailers.Models;

namespace ABCRetailers.Services
{
    public interface ISqlDataService
    {
        // User operations
        Task<List<User>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(Guid userId);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User> CreateUserAsync(User user);
        Task<User> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(Guid userId);

        // Authentication operations
        Task<bool> ValidateUserCredentialsAsync(string username, string password);
        Task UpdateLastLoginAsync(Guid userId);

        // Role-based operations
        Task<List<User>> GetUsersByRoleAsync(string role);
        Task<bool> ChangeUserRoleAsync(Guid userId, string newRole);
        Task<bool> ToggleUserActiveStatusAsync(Guid userId, bool isActive);
    }
}