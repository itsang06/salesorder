using System.Collections.Generic;

namespace ODSaleOrder.API.Models
{
    public class Delivery
    {
        public string OrderRefNumber { get; set; }
        public List<DeliveryItem> Items { get; set; }
        public DeliveryNote Header { get; set; }
        public DeliveryStatus Status { get; set; }
        public List<OrderItemModel> TangHang { get; set; }
        public List<OrderItemModel> TangChietKhau { get; set; }
        public List<OrderItemModel> KhuyenMai { get; set; }
        public TienPhaiThanhToanModel TienPhaiThanhToan { get; set; }
        public List<TangTien> TangTien { get; set; }
    }

    public class TienPhaiThanhToanModel
    {
        public decimal TienPhaiThanhToan { get; set; } = 0;
        public decimal TongKhuyenMaiTien { get; set; } = 0;
        public decimal TraThuongTrungBai { get; set; } = 0;
    }
}
