using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wpf.Ui.Controls;

namespace UiDesktopApp1.Behaviors
{
    public static class TextBoxBehaviors
    {
        public static readonly DependencyProperty SelectAllOnFocusProperty =
            DependencyProperty.RegisterAttached(
                "SelectAllOnFocus",
                typeof(bool),
                typeof(TextBoxBehaviors),
                new PropertyMetadata(false, OnSelectAllOnFocusChanged));

        public static void SetSelectAllOnFocus(DependencyObject element, bool value)
            => element.SetValue(SelectAllOnFocusProperty, value);

        public static bool GetSelectAllOnFocus(DependencyObject element)
            => (bool)element.GetValue(SelectAllOnFocusProperty);

        private static void OnSelectAllOnFocusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBox tb) return;

            if ((bool)e.NewValue)
            {
                tb.GotKeyboardFocus += (s, _) => ((TextBox)s).SelectAll();
                tb.PreviewMouseLeftButtonDown += (s, ev) =>
                {
                    var box = (TextBox)s;
                    if (!box.IsKeyboardFocusWithin)
                    {
                        ev.Handled = true;
                        box.Focus();
                        box.SelectAll();
                    }
                };
            }
        }
    }
}
