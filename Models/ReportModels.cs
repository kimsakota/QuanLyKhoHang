using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiDesktopApp1.Models
{
    class ReportModels
    {
    }

    public class FinancialReportResponse
    {
        public decimal TotalRevenue { get; set; } // Tổng thu
        public decimal TotalCost { get; set; }    // Tổng chi
        public decimal TotalProfit { get; set; }  // Lợi nhuận
        public List<DailyFinancialStats> DailyStats { get; set; } = new();
    }

    public class DailyFinancialStats
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public decimal Cost { get; set; }
        public decimal Profit => Revenue - Cost;
    }

    public class CustomerReportReponse
    {
        public int TotalCustomers { get; set; }       // Tổng khách hàng trong DB
        public int ActiveCustomers { get; set; }      // Khách có mua hàng trong thời gian
        public int TotalOrders { get; set; }          // Tổng đơn hàng trong thời gian
        public decimal TotalRevenue { get; set; }     // Doanh thu trong thời gian
        public List<TopCustomerDto> TopCustomers { get; set; } = new(); // Danh sách khách hàng chi tiêu nhiều nhất
    }

    public class TopCustomerDto
    {
        public string Name { get; set; } = string.Empty; 
        public string Phone { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; }
    }

    public class SupplierReportResponse
    {
        public int TotalSuppliers { get; set; }       // Tổng số NCC
        public int ActiveSuppliers { get; set; }      // Số NCC có giao dịch trong kỳ
        public int TotalImportOrders { get; set; }    // Tổng số phiếu nhập
        public decimal TotalImportCost { get; set; }  // Tổng chi phí nhập hàng
        public List<TopSupplierDto> TopSuppliers { get; set; } = new(); // Top NCC
    }

    public class TopSupplierDto
    {
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal TotalImportValue { get; set; }
    }
}
