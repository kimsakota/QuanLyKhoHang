using UiDesktopApp1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace UiDesktopApp1.Services
{
    public interface IAuthenticationService
    {
        Task<UserModel?> AuthenticateAsync(string username, string password);
    }
}
