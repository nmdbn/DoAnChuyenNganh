using DoAnChuyenNganh.Models;
using DoAnChuyenNganh.ViewModels.Auth;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace DoAnChuyenNganh.Services
{
    public class AuthService : IAuthService
    {
        private readonly DoAnChuyenNganhContext _context;
        private readonly ILogger<AuthService> _logger;
        private const int MaxLoginAttempts = 5;
        private const int LockoutMinutes = 30;

        public AuthService(DoAnChuyenNganhContext context, ILogger<AuthService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<(bool Success, User? User, string Message)> AuthenticateAsync(
            string usernameOrEmail, string password, string ipAddress, string userAgent)
        {
            try
            {
                // Find user by username or email
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => 
                        u.Username == usernameOrEmail || u.Email == usernameOrEmail);

                if (user == null)
                {
                    await LogActivityAsync(null, "login_failed", "user", null, ipAddress, userAgent);
                    return (false, null, "Invalid username/email or password");
                }

                // Check if account is locked
                if (user.IsLocked == true && user.LockoutUntil.HasValue && user.LockoutUntil.Value > DateTime.Now)
                {
                    var remainingMinutes = (int)(user.LockoutUntil.Value - DateTime.Now).TotalMinutes;
                    return (false, null, $"Account is locked. Try again in {remainingMinutes} minutes");
                }

                // Check if account is active
                if (user.IsActive == false)
                {
                    return (false, null, "Account is inactive. Please contact support");
                }

                // Verify password
                if (!VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
                {
                    // Increment login attempts
                    user.LoginAttempts = (byte)((user.LoginAttempts ?? 0) + 1);

                    if (user.LoginAttempts >= MaxLoginAttempts)
                    {
                        user.IsLocked = true;
                        user.LockoutUntil = DateTime.Now.AddMinutes(LockoutMinutes);
                        await _context.SaveChangesAsync();
                        await LogActivityAsync(user.UserId, "account_locked", "user", user.UserId, ipAddress, userAgent);
                        return (false, null, $"Too many failed attempts. Account locked for {LockoutMinutes} minutes");
                    }

                    await _context.SaveChangesAsync();
                    await LogActivityAsync(user.UserId, "login_failed", "user", user.UserId, ipAddress, userAgent);
                    return (false, null, "Invalid username/email or password");
                }

                // Reset login attempts and unlock if locked
                user.LoginAttempts = 0;
                user.IsLocked = false;
                user.LockoutUntil = null;
                user.LastLoginAt = DateTime.Now;
                await _context.SaveChangesAsync();

                await LogActivityAsync(user.UserId, "login_success", "user", user.UserId, ipAddress, userAgent);
                return (true, user, "Login successful");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during authentication");
                return (false, null, "An error occurred during login");
            }
        }

        public async Task<(bool Success, User? User, string Message)> RegisterAsync(
            RegisterViewModel model, string ipAddress, string userAgent)
        {
            try
            {
                // Check if username exists
                if (await UsernameExistsAsync(model.Username))
                {
                    return (false, null, "Username already exists");
                }

                // Check if email exists
                if (await EmailExistsAsync(model.Email))
                {
                    return (false, null, "Email already exists");
                }

                // Generate password hash and salt
                var (hash, salt) = HashPassword(model.Password);

                // Create new user
                var user = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    PasswordHash = hash,
                    PasswordSalt = salt,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PhoneNumber = model.PhoneNumber,
                    DateOfBirth = model.DateOfBirth,
                    RoleId = 2, // Default role (Student/User)
                    IsEmailVerified = false,
                    IsActive = true,
                    IsLocked = false,
                    LoginAttempts = 0,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                await LogActivityAsync(user.UserId, "user_registered", "user", user.UserId, ipAddress, userAgent);
                return (true, user, "Registration successful");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                return (false, null, "An error occurred during registration");
            }
        }

        public async Task<(bool Success, string Message)> ChangePasswordAsync(
            int userId, string currentPassword, string newPassword, string ipAddress, string userAgent)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return (false, "User not found");
                }

                // Verify current password
                if (!VerifyPassword(currentPassword, user.PasswordHash, user.PasswordSalt))
                {
                    await LogActivityAsync(userId, "password_change_failed", "user", userId, ipAddress, userAgent);
                    return (false, "Current password is incorrect");
                }

                // Generate new password hash and salt
                var (hash, salt) = HashPassword(newPassword);
                user.PasswordHash = hash;
                user.PasswordSalt = salt;
                user.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                await LogActivityAsync(userId, "password_changed", "user", userId, ipAddress, userAgent);

                return (true, "Password changed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return (false, "An error occurred while changing password");
            }
        }

        public async Task<(bool Success, string? Token, string Message)> GeneratePasswordResetTokenAsync(string email)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    // Don't reveal if email exists
                    return (true, null, "If the email exists, a reset link will be sent");
                }

                // Generate reset token (in production, store this in database with expiration)
                var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                
                // TODO: Store token in database with expiration time
                // TODO: Send email with reset link

                return (true, token, "Password reset token generated");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating password reset token");
                return (false, null, "An error occurred");
            }
        }

        public async Task<(bool Success, string Message)> ResetPasswordAsync(
            string email, string token, string newPassword, string ipAddress, string userAgent)
        {
            try
            {
                // TODO: Validate token from database
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    return (false, "Invalid reset token");
                }

                // Generate new password hash and salt
                var (hash, salt) = HashPassword(newPassword);
                user.PasswordHash = hash;
                user.PasswordSalt = salt;
                user.UpdatedAt = DateTime.Now;
                user.LoginAttempts = 0;
                user.IsLocked = false;
                user.LockoutUntil = null;

                await _context.SaveChangesAsync();
                await LogActivityAsync(user.UserId, "password_reset", "user", user.UserId, ipAddress, userAgent);

                return (true, "Password reset successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password");
                return (false, "An error occurred while resetting password");
            }
        }

        public async Task<UserSession> CreateSessionAsync(int userId, string ipAddress, string userAgent, bool rememberMe)
        {
            var sessionToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var expiresAt = rememberMe ? DateTime.Now.AddDays(30) : DateTime.Now.AddHours(24);

            var session = new UserSession
            {
                UserId = userId,
                SessionToken = sessionToken,
                Ipaddress = ipAddress,
                UserAgent = userAgent,
                ExpiresAt = expiresAt,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.UserSessions.Add(session);
            await _context.SaveChangesAsync();

            return session;
        }

        public async Task<UserSession?> GetValidSessionAsync(string sessionToken)
        {
            return await _context.UserSessions
                .Include(s => s.User)
                    .ThenInclude(u => u.Role)
                .FirstOrDefaultAsync(s => 
                    s.SessionToken == sessionToken && 
                    s.IsActive == true && 
                    s.ExpiresAt > DateTime.Now);
        }

        public async Task InvalidateSessionAsync(string sessionToken)
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.SessionToken == sessionToken);

            if (session != null)
            {
                session.IsActive = false;
                await _context.SaveChangesAsync();
            }
        }

        public async Task LogActivityAsync(int? userId, string activityType, string? itemType, int? itemId, string ipAddress, string userAgent)
        {
            try
            {
                var log = new UserActivityLog
                {
                    UserId = userId,
                    ActivityType = activityType,
                    ItemType = itemType,
                    ItemId = itemId,
                    Ipaddress = ipAddress,
                    UserAgent = userAgent,
                    CreatedAt = DateTime.Now
                };

                _context.UserActivityLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging activity");
            }
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task<(bool Success, string Message)> UpdateProfileAsync(int userId, UserProfileViewModel model)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return (false, "User not found");
                }

                // Check if username changed and already exists
                if (user.Username != model.Username && await UsernameExistsAsync(model.Username))
                {
                    return (false, "Username already exists");
                }

                // Check if email changed and already exists
                if (user.Email != model.Email && await EmailExistsAsync(model.Email))
                {
                    return (false, "Email already exists");
                }

                // Update user properties
                user.Username = model.Username;
                user.Email = model.Email;
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.PhoneNumber = model.PhoneNumber;
                user.DateOfBirth = model.DateOfBirth;
                user.Bio = model.Bio;
                user.AvatarUrl = model.AvatarUrl;
                user.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                return (true, "Profile updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile");
                return (false, "An error occurred while updating profile");
            }
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            return await _context.Users.AnyAsync(u => u.Username == username);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        #region Password Hashing

        private (string Hash, string Salt) HashPassword(string password)
        {
            // Generate salt
            var saltBytes = RandomNumberGenerator.GetBytes(32);
            var salt = Convert.ToBase64String(saltBytes);

            // Generate hash
            var hash = HashPasswordWithSalt(password, saltBytes);

            return (hash, salt);
        }

        private bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            var saltBytes = Convert.FromBase64String(storedSalt);
            var hash = HashPasswordWithSalt(password, saltBytes);
            return hash == storedHash;
        }

        private string HashPasswordWithSalt(string password, byte[] salt)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(32);
            return Convert.ToBase64String(hash);
        }

        #endregion
    }
}

