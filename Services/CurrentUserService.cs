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

        public Roles CurrentRole { get; private set; } = Roles.Employee; 

        public bool IsLoggedIn => CurrentUser != null;
        public bool IsAdmin => CurrentRole == Roles.Admin;
        public bool IsManager => CurrentRole == Roles.Manager;
        public bool IsEmployee => CurrentRole == Roles.Employee;

        public void SetCurrentUser(UserModel user)
        {
            CurrentUser = user;
            if (Enum.TryParse(user.Role, true, out Roles parsedRole))
                CurrentRole = parsedRole;
            else
                CurrentRole = Roles.Employee;
        }

        public void ClearCurrentUser()
        {
            CurrentUser = null;
            CurrentRole = Roles.Employee;
        }
    }
}
