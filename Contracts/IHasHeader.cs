using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiDesktopApp1.Contracts
{
    public interface IHasHeader
    {
        /// <summary>
        /// Trả về UserControl header tương ứng với page.
        /// </summary>
        object? GetHeader();
    }
}
