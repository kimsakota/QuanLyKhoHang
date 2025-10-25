using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using UiDesktopApp1.Models;
using UiDesktopApp1.Models.Messages;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wpf.Ui;

namespace UiDesktopApp1.ViewModels.Pages.SanPham
{
    public partial class ThemDanhMucViewModel : ObservableObject
    {
        private readonly INavigationService _nav;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        [ObservableProperty]
        private string? name;

        public ThemDanhMucViewModel(INavigationService nav, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _nav = nav;
            _dbContextFactory = dbContextFactory;
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            var n = (Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(n))
            {
                MessageBox.Show("Không được để trống tên danh mục", "Dữ liệu chưa hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            try
            {
                await using var db = await _dbContextFactory.CreateDbContextAsync();

                // (tuỳ chọn) kiểm tra trùng tên
                var existed = await db.Categories.AnyAsync(c => c.Name == n);
                if (existed)
                {
                    // TODO: bắn snackbar nếu bạn có service thông báo
                    MessageBox.Show("Đã tồn tại danh mục này", "Dữ liệu chưa hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var cat = new CategoryModel
                {
                    Name = n,
                };

                db.Categories.Add(cat);
                await db.SaveChangesAsync();

                // Gửi “tin nhắn” về trang trước để tự chọn danh mục vừa tạo
                WeakReferenceMessenger.Default.Send(new CategoryCreatedMessage(cat));

                _nav.GoBack();
            }
            finally
            {
                
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            _nav.GoBack();
        }
    }
}
