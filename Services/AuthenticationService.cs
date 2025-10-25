using Microsoft.EntityFrameworkCore;
using UiDesktopApp1.Models;

namespace UiDesktopApp1.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public AuthenticationService(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        // === THAY SecureString thành string ===
        public async Task<UserModel?> AuthenticateAsync(string username, string password)
        // =====================================
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();

            var user = await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username.Equals(username));

            if (user == null) return null;

            // === XÁC THỰC TRỰC TIẾP VỚI string ===
            // KHÔNG cần chuyển đổi từ SecureString nữa
            bool isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            // =======================================

            // Không cần xóa password khỏi bộ nhớ vì nó đã là string rồi

            return isValid ? user : null;
        }

        // === XÓA HÀM ConvertSecureStringToString() ===
        // private string ConvertSecureStringToString(SecureString securePassword) { ... }
        // ==============================================
    }
}