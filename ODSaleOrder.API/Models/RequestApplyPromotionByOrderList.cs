using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ODSaleOrder.API.Models
{
    public class RequestApplyPromotionByOrderList
    {
        public string SaleOrgCode { get; set; }
        public string SicCode { get; set; }
        public string CustomerCode { get; set; }
        public string ShiptoCode { get; set; }
        public string RouteZoneCode { get; set; }
        public string DsaCode { get; set; }
        public string Branch { get; set; }
        public string Region { get; set; }
        public string SubRegion { get; set; }
        public string Area { get; set; }
        public string SubArea { get; set; }
        public string DistributorCode { get; set; }
        public List<ProductItem> ProductList { get; set; }
        public bool EnableRecursiveMode { get; set; } = false;
    }


    public class RequestEnhanceApplyPromotionByOrderList
    {
        public string SaleOrgCode { get; set; }
        public string SicCode { get; set; }
        public string CustomerCode { get; set; }
        public string ShiptoCode { get; set; }
        public string RouteZoneCode { get; set; }
        public string DsaCode { get; set; }
        public string Branch { get; set; }
        public string Region { get; set; }
        public string SubRegion { get; set; }
        public string Area { get; set; }
        public string SubArea { get; set; }
        public string DistributorCode { get; set; }
        public List<ProductItem> ProductList { get; set; }
        [Required]
        public List<string> PromotionCodes { get; set; }

    }

    public class RequestGetPromotionCode
    {
        public string SaleOrgCode { get; set; }
        public string SicCode { get; set; }
        public string CustomerCode { get; set; }
        public string ShiptoCode { get; set; }
        public string RouteZoneCode { get; set; }
        public string DsaCode { get; set; }
        public string Branch { get; set; }
        public string Region { get; set; }
        public string SubRegion { get; set; }
        public string Area { get; set; }
        public string SubArea { get; set; }
        public string DistributorCode { get; set; }
        public decimal TotalAmount { get; set; }

    }

    public class ProductItem
    {
        public string ItemGroupCode { get; set; }
        public string ItemCode { get; set; }
        public decimal? Quantity { get; set; }
        public string Uom { get; set; }
        public decimal? Price { get; set; }
        public decimal? TotalAmount { get; set; }
        public int? BaseQuantity { get; set; }
        public string BaseUom { get; set; }
        public int ConversionFactor { get; set; }
        public ProductItem CloneWithNewQuantity(int quantity)
        {
            return new ProductItem
            {
                ItemGroupCode = this.ItemGroupCode,
                ItemCode = this.ItemCode,
                Uom = this.Uom,
                Price = this.Price,
                TotalAmount = this.Price * quantity,
                BaseUom = this.BaseUom,
                ConversionFactor = this.ConversionFactor,
                Quantity = quantity / (decimal)this.ConversionFactor,
                BaseQuantity = quantity
            };
        }

    }

    public class ProductItemCustomize
    {      
        public string ItemCode { get; set; }
        public string BaseUom { get; set; }
        public int? BaseQuantity { get; set; }
    }

}
