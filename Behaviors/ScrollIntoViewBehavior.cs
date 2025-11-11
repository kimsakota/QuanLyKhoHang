using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace UiDesktopApp1.Behaviors
{
    // Đây là một "Hành vi" có thể đính kèm vào ListView (hoặc DataGrid)
    public class ScrollIntoViewBehavior : Behavior<ItemsControl> // Dùng ItemsControl chung
    {
        // 1. Tạo một thuộc tính (DependencyProperty) 
        //    để chúng ta có thể BINDING từ XAML
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(
                nameof(SelectedItem),
                typeof(object),
                typeof(ScrollIntoViewBehavior),
                new PropertyMetadata(null, OnSelectedItemChanged)); // Hàm callback khi giá trị thay đổi

        // Thuộc tính .NET bình thường để XAML binding vào
        public object SelectedItem
        {
            get { return GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        // 2. Hàm này được GỌI TỰ ĐỘNG
        //    mỗi khi thuộc tính "SelectedItem" (ở trên) thay đổi
        //    (tức là khi ViewModel cập nhật SelectedSupplier)
        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (ScrollIntoViewBehavior)d;
            var itemsControl = behavior.AssociatedObject; // Lấy control (ListView/DataGrid)
            var newItem = e.NewValue; // Lấy item mới được chọn

            if (itemsControl == null || newItem == null)
                return;

            // 3. Thực thi hành động của View
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Kiểm tra xem control là ListView hay DataGrid
                // và gọi đúng hàm ScrollIntoView

                if (itemsControl is Wpf.Ui.Controls.ListView uiListView)
                {
                    uiListView.ScrollIntoView(newItem);
                }
                else if (itemsControl is Wpf.Ui.Controls.DataGrid uiDataGrid)
                {
                    uiDataGrid.ScrollIntoView(newItem);
                }
                // (Bạn có thể thêm các control khác ở đây nếu cần)
            });
        }
    }
}
