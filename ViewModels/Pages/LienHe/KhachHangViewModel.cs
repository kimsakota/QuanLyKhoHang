using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiDesktopApp1.Models;

namespace UiDesktopApp1.ViewModels.Pages.LienHe
{
    public partial class KhachHangViewModel : ObservableObject
    {

        public ObservableCollection<CustomerModel> Customers { get; } = new();

    }
}
