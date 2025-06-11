namespace ODSaleOrder.API.Models
{
    public class DeliveryItem
    {
        public string SanPham { get; set; }
        public string SoLuong { get; set; }
        public decimal DonGiaTruocThue { get; set; }
        public decimal ThanhTienTruocThue { get; set; }
        public decimal DonGiaSauThue { get; set; }
        public decimal ThanhTienSauThue { get; set; }
        public string UOMDesc { get; set; }
    }

    public class DeliveryStatus
    {
        public bool HasTangHang { get; set; } = false;
        public bool HasTangTienChietKhau { get; set; } = false;
        public bool HasTangTienKhuyenMai { get; set; } = false;
    }

    public class OrderItemModel
    {
        public string Name { get; set; }
        public string SanPhamTang { get; set; }
        public string UOM { get; set; }
        public int OrderQuantities { get; set; }
        public string Quantity { get; set; } // dạng "5 | 0 | 0"
    }

    public class TangTien
    {
        public string Name { get; set; }
        public decimal SoTienTang { get; set; }
    }
}
