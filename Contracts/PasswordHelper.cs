using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace UiDesktopApp1.Contracts
{
    public static class PasswordHelper
    {
        private const int SaltSize = 16;
        private const int HashSize = 32; 
        private const int Iterations = 10000;

        /// <summary>
        /// Tạo chuỗi Hash và Salt từ mật khẩu
        /// </summary>
        /// <param name="password">Mật khẩu người dùng nhập</param>
        /// <returns>Một Tuple chứa (Hash, Salt) dưới dạng chuỗi Base64</returns>
        public static (string Hash, string Salt) HashPassword(string password)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] saltBytes = new byte[SaltSize];
                rng.GetBytes(saltBytes);

                using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations, HashAlgorithmName.SHA256))
                {
                    byte[] hashBytes = pbkdf2.GetBytes(HashSize);

                    // Trả về chuỗi Base64 để lưu vào CSDL
                    return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
                }
            }
        }

        /// <summary>
        /// Xác thực mật khẩu với Hash và Salt đã lưu
        /// </summary>
        /// <param name="password">Mật khẩu người dùng nhập (văn bản gốc)</param>
        /// <param name="storedHash">Hash đã lưu trong CSDL (Base64)</param>
        /// <param name="storedSalt">Salt đã lưu trong CSDL (Base64)</param>
        /// <returns>True nếu mật khẩu khớp, ngược lại là False</returns>
        public static bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            try
            {
                byte[] saltBytes = Convert.FromBase64String(storedSalt);
                byte[] hashBytes = Convert.FromBase64String(storedHash);

                using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations, HashAlgorithmName.SHA256))
                {
                    byte[] testHash = pbkdf2.GetBytes(HashSize);
                    return testHash.SequenceEqual(hashBytes);
                }
            }
            catch
            {
                // Lỗi (ví dụ: chuỗi base64 không hợp lệ) thì chắc chắn là không khớp
                return false;
            }
        }
    }
}
