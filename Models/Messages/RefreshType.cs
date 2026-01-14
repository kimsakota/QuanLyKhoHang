using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiDesktopApp1.Models.Messages
{
    public enum RefreshType
    {
        ProductList, // Thay cho ProductsNeedRefreshMessage
        Inventory,   // Thay cho UpdateTonKhoMessage
        CustomerList,
        CategoryList
    }
}
