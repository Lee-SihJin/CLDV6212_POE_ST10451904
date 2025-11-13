// Services/IAuthService.cs
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using Dapper;

namespace ABCRetailers.Services
{
    public interface IAuthService
    {
        Task<User?> AuthenticateAsync(string username, string password);
        Task<User?> RegisterAsync(RegisterModel model);
        Task<User?> GetUserByIdAsync(Guid userId);
        Task<bool> UserExistsAsync(string username, string email);
        Task<UserProfile?> GetUserProfileAsync(Guid userId);
        Task<bool> UpdateUserProfileAsync(Guid userId, UpdateCustomerProfileModel model);
        Task<bool> UpdateUserAsync(Guid userId, string firstName, string lastName, string email);
    }

    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public AuthService(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            using var connection = new SqlConnection(_connectionString);

            var user = await connection.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM Users WHERE Username = @Username AND IsActive = 1",
                new { Username = username });

            if (user != null && VerifyPassword(password, user.PasswordHash))
            {
                // Update last login date
                await connection.ExecuteAsync(
                    "UPDATE Users SET LastLoginDate = @LastLoginDate WHERE UserId = @UserId",
                    new { LastLoginDate = DateTime.UtcNow, user.UserId });

                return user;
            }

            return null;
        }

        public async Task<User?> RegisterAsync(RegisterModel model)
        {
            if (await UserExistsAsync(model.Username, model.Email))
                return null;

            using var connection = new SqlConnection(_connectionString);

            var user = new User
            {
                UserId = Guid.NewGuid(),
                Username = model.Username,
                Email = model.Email,
                PasswordHash = HashPassword(model.Password),
                FirstName = model.FirstName,
                LastName = model.LastName,
                Role = "Customer"
            };

            var sql = @"INSERT INTO Users (UserId, Username, Email, PasswordHash, FirstName, LastName, Role) 
                       VALUES (@UserId, @Username, @Email, @PasswordHash, @FirstName, @LastName, @Role)";

            await connection.ExecuteAsync(sql, user);

            // Create shopping cart for the user
            await connection.ExecuteAsync(
                "INSERT INTO ShoppingCart (CartId, UserId) VALUES (@CartId, @UserId)",
                new { CartId = Guid.NewGuid(), UserId = user.UserId });

            return user;
        }

        public async Task<User?> GetUserByIdAsync(Guid userId)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM Users WHERE UserId = @UserId",
                new { UserId = userId });
        }

        public async Task<bool> UserExistsAsync(string username, string email)
        {
            using var connection = new SqlConnection(_connectionString);
            var exists = await connection.QueryFirstOrDefaultAsync<int?>(
                "SELECT 1 FROM Users WHERE Username = @Username OR Email = @Email",
                new { Username = username, Email = email });

            return exists.HasValue;
        }

        private string HashPassword(string password)
        {
            // In production, use proper password hashing like BCrypt
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password + _configuration["PasswordSalt"]);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            var hash = HashPassword(password);
            return hash == storedHash;
        }

        public async Task<UserProfile?> GetUserProfileAsync(Guid userId)
        {
            using var connection = new SqlConnection(_connectionString);

            var user = await connection.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM Users WHERE UserId = @UserId",
                new { UserId = userId });

            if (user == null)
                return null;

            var profile = await connection.QueryFirstOrDefaultAsync<UserProfile>(
                "SELECT * FROM UserProfiles WHERE UserId = @UserId",
                new { UserId = userId });

            return profile;
        }

        public async Task<bool> UpdateUserProfileAsync(Guid userId, UpdateCustomerProfileModel model)
        {
            using var connection = new SqlConnection(_connectionString);

            // Check if profile exists
            var existingProfile = await connection.QueryFirstOrDefaultAsync<UserProfile>(
                "SELECT * FROM UserProfiles WHERE UserId = @UserId",
                new { UserId = userId });

            if (existingProfile == null)
            {
                // Create new profile
                var newProfile = new UserProfile
                {
                    ProfileId = Guid.NewGuid(),
                    UserId = userId,
                    PhoneNumber = model.PhoneNumber,
                    ShippingAddress = model.ShippingAddress,
                    DateOfBirth = model.DateOfBirth
                };

                await connection.ExecuteAsync(
                    @"INSERT INTO UserProfiles (ProfileId, UserId, PhoneNumber, ShippingAddress, DateOfBirth) 
                  VALUES (@ProfileId, @UserId, @PhoneNumber, @ShippingAddress, @DateOfBirth)",
                    newProfile);
            }
            else
            {
                // Update existing profile
                await connection.ExecuteAsync(
                    @"UPDATE UserProfiles 
                  SET PhoneNumber = @PhoneNumber, 
                      ShippingAddress = @ShippingAddress, 
                      DateOfBirth = @DateOfBirth 
                  WHERE UserId = @UserId",
                    new
                    {
                        model.PhoneNumber,
                        model.ShippingAddress,
                        model.DateOfBirth,
                        UserId = userId
                    });
            }

            // Update user basic info
            await connection.ExecuteAsync(
                "UPDATE Users SET FirstName = @FirstName, LastName = @LastName WHERE UserId = @UserId",
                new { model.FirstName, model.LastName, UserId = userId });

            return true;
        }

        public async Task<bool> UpdateUserAsync(Guid userId, string firstName, string lastName, string email)
        {
            using var connection = new SqlConnection(_connectionString);

            var rowsAffected = await connection.ExecuteAsync(
                "UPDATE Users SET FirstName = @FirstName, LastName = @LastName, Email = @Email WHERE UserId = @UserId",
                new { FirstName = firstName, LastName = lastName, Email = email, UserId = userId });

            return rowsAffected > 0;
        }
    }
}