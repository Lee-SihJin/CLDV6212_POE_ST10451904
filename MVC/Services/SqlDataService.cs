// SqlDataService.cs
using System.Data.SqlClient;
using Dapper;
using ABCRetailers.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ABCRetailers.Services
{
    public class SqlDataService : ISqlDataService
    {
        private readonly string _connectionString;
        private readonly ILogger<SqlDataService> _logger;

        public SqlDataService(IConfiguration configuration, ILogger<SqlDataService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        // User operations
        public async Task<List<User>> GetAllUsersAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                const string sql = @"
                    SELECT 
                        UserId, Username, Email, PasswordHash, 
                        FirstName, LastName, Role, IsActive, 
                        CreatedDate, LastLoginDate
                    FROM Users 
                    ORDER BY CreatedDate DESC";

                var users = await connection.QueryAsync<User>(sql);
                return users.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all users from SQL database");
                throw;
            }
        }

        public async Task<User?> GetUserByIdAsync(Guid userId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                const string sql = @"
                    SELECT 
                        UserId, Username, Email, PasswordHash, 
                        FirstName, LastName, Role, IsActive, 
                        CreatedDate, LastLoginDate
                    FROM Users 
                    WHERE UserId = @UserId";

                return await connection.QueryFirstOrDefaultAsync<User>(sql, new { UserId = userId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                const string sql = @"
                    SELECT 
                        UserId, Username, Email, PasswordHash, 
                        FirstName, LastName, Role, IsActive, 
                        CreatedDate, LastLoginDate
                    FROM Users 
                    WHERE Username = @Username";

                return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Username = username });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by username: {Username}", username);
                throw;
            }
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                const string sql = @"
                    SELECT 
                        UserId, Username, Email, PasswordHash, 
                        FirstName, LastName, Role, IsActive, 
                        CreatedDate, LastLoginDate
                    FROM Users 
                    WHERE Email = @Email";

                return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Email = email });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by email: {Email}", email);
                throw;
            }
        }

        public async Task<User> CreateUserAsync(User user)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                const string sql = @"
                    INSERT INTO Users (
                        Username, Email, PasswordHash, FirstName, LastName, 
                        Role, IsActive, CreatedDate, LastLoginDate
                    )
                    OUTPUT INSERTED.*
                    VALUES (
                        @Username, @Email, @PasswordHash, @FirstName, @LastName,
                        @Role, @IsActive, @CreatedDate, @LastLoginDate
                    )";

                var createdUser = await connection.QuerySingleAsync<User>(sql, new
                {
                    user.Username,
                    user.Email,
                    user.PasswordHash,
                    user.FirstName,
                    user.LastName,
                    user.Role,
                    user.IsActive,
                    CreatedDate = DateTime.UtcNow,
                    LastLoginDate = (DateTime?)null
                });

                return createdUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user with username: {Username}", user.Username);
                throw;
            }
        }

        public async Task<User> UpdateUserAsync(User user)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                const string sql = @"
                    UPDATE Users 
                    SET 
                        Username = @Username,
                        Email = @Email,
                        PasswordHash = @PasswordHash,
                        FirstName = @FirstName,
                        LastName = @LastName,
                        Role = @Role,
                        IsActive = @IsActive,
                        LastLoginDate = @LastLoginDate
                    OUTPUT INSERTED.*
                    WHERE UserId = @UserId";

                var updatedUser = await connection.QuerySingleAsync<User>(sql, new
                {
                    user.UserId,
                    user.Username,
                    user.Email,
                    user.PasswordHash,
                    user.FirstName,
                    user.LastName,
                    user.Role,
                    user.IsActive,
                    user.LastLoginDate
                });

                return updatedUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user ID: {UserId}", user.UserId);
                throw;
            }
        }

        public async Task<bool> DeleteUserAsync(Guid userId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                const string sql = "DELETE FROM Users WHERE UserId = @UserId";

                var affectedRows = await connection.ExecuteAsync(sql, new { UserId = userId });
                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user ID: {UserId}", userId);
                throw;
            }
        }

        // Authentication operations
        public async Task<bool> ValidateUserCredentialsAsync(string username, string password)
        {
            try
            {
                var user = await GetUserByUsernameAsync(username);
                if (user == null || !user.IsActive)
                    return false;

                // In a real application, you would hash the input password and compare with stored hash
                // This is a simplified version - you should use proper password hashing
                return VerifyPassword(password, user.PasswordHash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating credentials for user: {Username}", username);
                throw;
            }
        }

        public async Task UpdateLastLoginAsync(Guid userId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                const string sql = @"
                    UPDATE Users 
                    SET LastLoginDate = @LastLoginDate 
                    WHERE UserId = @UserId";

                await connection.ExecuteAsync(sql, new
                {
                    UserId = userId,
                    LastLoginDate = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating last login for user ID: {UserId}", userId);
                throw;
            }
        }

        // Role-based operations
        public async Task<List<User>> GetUsersByRoleAsync(string role)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                const string sql = @"
                    SELECT 
                        UserId, Username, Email, PasswordHash, 
                        FirstName, LastName, Role, IsActive, 
                        CreatedDate, LastLoginDate
                    FROM Users 
                    WHERE Role = @Role 
                    ORDER BY CreatedDate DESC";

                var users = await connection.QueryAsync<User>(sql, new { Role = role });
                return users.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users by role: {Role}", role);
                throw;
            }
        }

        public async Task<bool> ChangeUserRoleAsync(Guid userId, string newRole)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                const string sql = @"
                    UPDATE Users 
                    SET Role = @Role 
                    WHERE UserId = @UserId";

                var affectedRows = await connection.ExecuteAsync(sql, new
                {
                    UserId = userId,
                    Role = newRole
                });

                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing role for user ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> ToggleUserActiveStatusAsync(Guid userId, bool isActive)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                const string sql = @"
                    UPDATE Users 
                    SET IsActive = @IsActive 
                    WHERE UserId = @UserId";

                var affectedRows = await connection.ExecuteAsync(sql, new
                {
                    UserId = userId,
                    IsActive = isActive
                });

                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling active status for user ID: {UserId}", userId);
                throw;
            }
        }

        // Helper method for password verification (simplified - use proper hashing in production)
        private bool VerifyPassword(string inputPassword, string storedHash)
        {
            // This is a simplified version. In production, use:
            // return BCrypt.Net.BCrypt.Verify(inputPassword, storedHash);
            return inputPassword == storedHash; // Remove this in production!
        }
    }
}