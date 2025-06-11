using System;

namespace ODSaleOrder.API.Models
{
    public class DeliveryNote
    {
        public int PrintedDeliveryNoteCount { get; set; }
        public string DistributorCode { get; set; }
        public string DisName { get; set; }
        public string DisAddress { get; set; }
        public string DisPhone { get; set; }
        public string DisLogo { get; set; }
        public DateTime? NgayIn { get; set; }

        public string MaSM { get; set; }
        public string TenSM { get; set; }
        public string SDTSM { get; set; }

        public string DriverCode { get; set; }
        public string DriverName { get; set; }
        public string DriverPhone { get; set; }

        public string MaKhachHang { get; set; }
        public string TenKhachHang { get; set; }
        public string SDTKhachHang { get; set; }

        public DateTime? NgayDonHang { get; set; }
        public DateTime? NgayGiaoHang { get; set; }

        public string DiaChiKhachHang { get; set; }
        public string GhiChu { get; set; }

        public string TenNganHang { get; set; }
        public string TenTaiKhoan { get; set; }
        public string STK { get; set; }

    }
}
