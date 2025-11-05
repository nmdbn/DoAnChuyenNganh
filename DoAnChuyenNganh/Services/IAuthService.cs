using DoAnChuyenNganh.Models;
using DoAnChuyenNganh.ViewModels.Auth;

namespace DoAnChuyenNganh.Services
{
    public interface IAuthService
    {
        /// <summary>
        /// Authenticates a user with username/email and password
        /// </summary>
        Task<(bool Success, User? User, string Message)> AuthenticateAsync(string usernameOrEmail, string password, string ipAddress, string userAgent);

        /// <summary>
        /// Registers a new user
        /// </summary>
        Task<(bool Success, User? User, string Message)> RegisterAsync(RegisterViewModel model, string ipAddress, string userAgent);

        /// <summary>
        /// Changes user password
        /// </summary>
        Task<(bool Success, string Message)> ChangePasswordAsync(int userId, string currentPassword, string newPassword, string ipAddress, string userAgent);

        /// <summary>
        /// Generates password reset token
        /// </summary>
        Task<(bool Success, string? Token, string Message)> GeneratePasswordResetTokenAsync(string email);

        /// <summary>
        /// Resets password using token
        /// </summary>
        Task<(bool Success, string Message)> ResetPasswordAsync(string email, string token, string newPassword, string ipAddress, string userAgent);

        /// <summary>
        /// Creates a new session for user
        /// </summary>
        Task<UserSession> CreateSessionAsync(int userId, string ipAddress, string userAgent, bool rememberMe);

        /// <summary>
        /// Validates and retrieves session
        /// </summary>
        Task<UserSession?> GetValidSessionAsync(string sessionToken);

        /// <summary>
        /// Invalidates user session
        /// </summary>
        Task InvalidateSessionAsync(string sessionToken);

        /// <summary>
        /// Logs user activity
        /// </summary>
        Task LogActivityAsync(int? userId, string activityType, string? itemType, int? itemId, string ipAddress, string userAgent);

        /// <summary>
        /// Gets user by ID
        /// </summary>
        Task<User?> GetUserByIdAsync(int userId);

        /// <summary>
        /// Updates user profile
        /// </summary>
        Task<(bool Success, string Message)> UpdateProfileAsync(int userId, UserProfileViewModel model);

        /// <summary>
        /// Checks if username exists
        /// </summary>
        Task<bool> UsernameExistsAsync(string username);

        /// <summary>
        /// Checks if email exists
        /// </summary>
        Task<bool> EmailExistsAsync(string email);

        /// <summary>
        /// Google OAuth authentication
        /// </summary>
        Task<AuthResult> AuthenticateGoogleAsync(string email, string fullName, string googleId, string ipAddress, string userAgent);
    }
}

