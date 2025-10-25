using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiDesktopApp1.Models;

namespace UiDesktopApp1.Services
{
    public class CurrentUserService
    {
        /// Dịch vụ Singleton để giữ trạng thái người dùng đang đăng nhập.
        public UserModel? CurrentUser { get; private set; }

        public bool IsLoggedIn => CurrentUser != null;
        public bool IsAdmin => CurrentUser?.Role == "Admin";
        public bool IsUser => CurrentUser?.Role == "User";

        public void SetCurrentUser(UserModel user)
        {
            CurrentUser = user;
        }

        public void ClearCurrentUser()
        {
            CurrentUser = null;
        }
    }
}
