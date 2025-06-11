using Microsoft.Extensions.Logging;
using nProx.Helpers.Dapper;
using ODSaleOrder.API.Models;
using ODSaleOrder.API.Models.SalesOrder;
using ODSaleOrder.API.Services.SaleOrder;
using RestSharp.Authenticators;
using RestSharp;
using SysAdmin.Models.StaticValue;
using System;
using System.Collections.Generic;
using System.Linq;
using static SysAdmin.API.Constants.Constant;
using ODSaleOrder.API.Infrastructure;
using Elastic.Apm.Api;
using Newtonsoft.Json;
using Sys.Common.Models;
using Microsoft.AspNetCore.Http;

namespace ODSaleOrder.API.Services.CaculateTax
{
    public class CalculateTaxService : ICalculateTaxService
    {
        private readonly IDapperRepositories _dapperRepositorie;
        public IRestClient _client;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private string _distributorCode = null;
        public CalculateTaxService(IDapperRepositories dapperRepositories, IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            _dapperRepositorie = dapperRepositories;
            _distributorCode = _httpContextAccessor.HttpContext?.Items["DistributorCode"] as string ?? _distributorCode;
        }


        public bool GetSalesPriceIncludeVaT()
        {
            var queryStr = $@"SELECT ""Key"" as ""SettingKey"", ""Value"" as ""SettingValue"" FROM ""public"".""PrincipalSettings"" WHERE ""Key"" = 'SalesPriceIncludeVat';";
            var result = (List<SettingResponse>)_dapperRepositorie.Query<SettingResponse>(queryStr);
            if (result == null || result.Count == 0) throw new Exception("Cann't get SalesPriceIncludeVat!!!");
            return result.FirstOrDefault().SettingValue;
        }

        public void CaculateShippingTax(ShippedLineTax shippedLineTax, out double shipped_Line_TaxAfter_Amt, out double shipped_Line_TaxBefore_Amt, out decimal shipped_Line_Extend_Amt)
        {

            if (shippedLineTax.salespriceincludeVAT)
            {
                shipped_Line_TaxAfter_Amt = (double)shippedLineTax.shipped_Line_Amt - (((double)shippedLineTax.shipped_line_Disc_Amt + (double)shippedLineTax.disCountAmount)) * ((double)shippedLineTax.vatValue / 100);
                shipped_Line_TaxBefore_Amt = (shipped_Line_TaxAfter_Amt + (((double)shippedLineTax.shipped_line_Disc_Amt + (double)shippedLineTax.disCountAmount) * ((double)shippedLineTax.vatValue / 100))) / (1 + ((double)shippedLineTax.vatValue / 100));
                shipped_Line_Extend_Amt = (decimal)shipped_Line_TaxAfter_Amt - shippedLineTax.shipped_line_Disc_Amt - shippedLineTax.disCountAmount;
            }
            else
            {
                shipped_Line_TaxBefore_Amt = (double)shippedLineTax.shipped_Line_Amt;
                shipped_Line_TaxAfter_Amt = shipped_Line_TaxBefore_Amt + (shipped_Line_TaxBefore_Amt - ((double)shippedLineTax.shipped_line_Disc_Amt + (double)shippedLineTax.disCountAmount)) * ((double)shippedLineTax.vatValue / 100);
                shipped_Line_Extend_Amt = (decimal)shipped_Line_TaxAfter_Amt - (shippedLineTax.shipped_line_Disc_Amt + shippedLineTax.disCountAmount);
            }
        }

        public void CaculateOrderLineTax(OrderLineTaxModel orderLineTaxModel, out double ord_Line_TaxAfter_Amt, out double ord_Line_TaxBefore_Amt)
        {
            if (orderLineTaxModel.salespriceincludeVAT)
            {
                ord_Line_TaxAfter_Amt = orderLineTaxModel.ord_Line_TotalBeforeTax_Amt - (((double)orderLineTaxModel.orig_Ord_line_Disc_Amt + (double)orderLineTaxModel.disCountAmount) * ((double)orderLineTaxModel.vatValue / 100));
                ord_Line_TaxBefore_Amt = (ord_Line_TaxAfter_Amt + (((double)orderLineTaxModel.orig_Ord_line_Disc_Amt + (double)orderLineTaxModel.disCountAmount) * ((double)orderLineTaxModel.vatValue / 100))) / (1 + ((double)orderLineTaxModel.vatValue / 100));
            }
            else
            {
                ord_Line_TaxBefore_Amt = (double)orderLineTaxModel.ord_Line_Amt;
                ord_Line_TaxAfter_Amt = ord_Line_TaxBefore_Amt + (ord_Line_TaxBefore_Amt - ((double)orderLineTaxModel.orig_Ord_line_Disc_Amt + (double)orderLineTaxModel.disCountAmount)) * ((double)orderLineTaxModel.vatValue / 100);
            }
        }

        public SO_OrderItems CalculateTaxInCludeVAT(ref SO_OrderItems order)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));

            order.DisCountAmount = (order.Ord_Line_Amt / (1 + order.VatValue / 100)) * ((decimal)order.DiscountPercented);

            // Tính giá trị dòng lệnh trước và sau thuế salespriceincludeVAT = true
            order.Ord_Line_Amt = order.UnitPrice * order.OrderQuantities;
            order.Ord_Line_TotalAfterTax_Amt = (double)order.Ord_Line_Amt;// - ((order.DisCountAmount + 0) * (order.VatValue / 100.0));
            order.Ord_Line_TotalBeforeTax_Amt = (double)(order.Ord_Line_TotalAfterTax_Amt ?? 0) / (double)(1 + (order.VatValue / 100));  //+ ((order.DisCountAmount + 0) * (order.VatValue / 100))

            order.VAT = (decimal)(order.Ord_Line_TotalAfterTax_Amt ?? 0) - (decimal)(order.Ord_Line_TotalBeforeTax_Amt ?? 0);//(order.Ord_Line_TotalBeforeTax_Amt ?? 0 - (0 + order.DisCountAmount) - 0) * (order.VatValue / 100.0);
            order.Ord_Line_Extend_Amt = (decimal)(order.Ord_Line_TotalAfterTax_Amt ?? 0) - order.Ord_line_Disc_Amt - order.DisCountAmount;
            // PriceVAT
            //order.UnitPriceAfterTax = order.UnitPrice;
            //order.UnitPriceBeforeTax = order.UnitPriceAfterTax / (1 + (order.VatValue / 100));

            return order;
        }

        public SO_OrderItems CalculateTaxNotInCludeVAT(ref SO_OrderItems order)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));

            // Tính giá trị dòng lệnh trước và sau thuế salespriceincludeVAT = fasle
            order.Ord_Line_Amt = order.UnitPrice * order.OrderQuantities;
            order.Ord_Line_TotalBeforeTax_Amt = (double)order.Ord_Line_Amt;
            order.Ord_Line_TotalAfterTax_Amt = (double)(order.Ord_Line_TotalBeforeTax_Amt ?? 0) * (double)(1 + (order.VatValue / 100));  //+ (order.Ord_Line_TotalBeforeTax_Amt - (0 + order.DisCountAmount)) 

            order.VAT = (decimal)(order.Ord_Line_TotalBeforeTax_Amt ?? 0) * (decimal)(order.VatValue / 100);
            order.Ord_Line_Extend_Amt = (decimal)(order.Ord_Line_TotalAfterTax_Amt ?? 0) - (order.Ord_line_Disc_Amt + order.DisCountAmount);

            //order.UnitPriceBeforeTax = order.UnitPrice;
            //order.UnitPriceAfterTax = order.UnitPriceBeforeTax * (1 + (order.VatValue / 100));

            return order;
        }

        public SO_OrderInformations CalculateTotalVAT(ref SaleOrderModel order)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));
            if (order.OrderItems == null || !order.OrderItems.Any()) return order;

            var orderItems = order.OrderItems.Where(x =>
                  !x.IsDeleted &&
                  !(x.IsKit && x.ItemCode == null)
              ).ToList();

            // Calculations to reduce redundant computations
            int totalItems = orderItems.Count(x => !x.IsFree);
            int totalQty = orderItems.Sum(x => x.OrderQuantities);
            int promotionQty = orderItems.Where(o => o.DiscountID != null).Sum(x => x.OrderQuantities);

            decimal totalAmount = orderItems.Sum(x => x.Ord_Line_Amt);
            double? totalBeforeTax = orderItems.Sum(x => x.Ord_Line_TotalBeforeTax_Amt);
            double? totalAfterTax = orderItems.Sum(x => x.Ord_Line_TotalAfterTax_Amt);
            decimal totalPromotion = orderItems.Sum(x => x.Ord_line_Disc_Amt);
            decimal totalDiscount = orderItems.Sum(x => x.Ord_line_Disc_Amt) + orderItems.Sum(x => x.DisCountAmount);
            decimal totalVAT = orderItems.Sum(x => x.VAT);

            // Assign calculated values to the order obj

            order.Ord_SKUs = totalItems;
            order.Ord_Qty = totalQty;
            order.Promotion_Qty = promotionQty;
            order.TotalLine = totalItems;

            order.Ordline_Disc_Amt = totalDiscount;  // tiền chiết khấu 
            order.Promotion_Amt = totalPromotion; // tiền khuyến mãi
            order.Ord_Amt = totalAmount; // tổng tiền chưa tính thuế 
            order.Ord_TotalBeforeTax_Amt = totalBeforeTax; // tổng tiền trước thuế
            order.Ord_TotalAfterTax_Amt = totalAfterTax;

            // Tổng tiền trước thuế = Ord_TotalBeforeTax_Amt
            //order.Promotion_BeforeTax_Amt = order.Ordline_Disc_Amt;
            //Tiền chiết khấu đơn hàng = Ord_Disc_Amt
            decimal? Ord_TotalBeforTax_AfterPromotion_Amt = (decimal)(order.Ord_TotalBeforeTax_Amt) - (order.Ordline_Disc_Amt) - order.Ord_Disc_Amt;
            //Tiền thuế = TotalVAT
            //Số tiền phải thanh toán = Ord_TotalBeforeTax_Amt + TotalVAT
            //order.Ord_Disc_Amt = (totalBeforeTax - order.Ordline_Disc_Amt) - order.Promotion_Amt) * order.DisCountAmount;

            order.TotalVAT = totalVAT;
            order.Ord_Extend_Amt = (decimal)Ord_TotalBeforTax_AfterPromotion_Amt + order.TotalVAT;

            return order;
        }
        public double? CalculateCommissionDiscount(SO_OrderInformations orderInformation, SO_OrderItems orderItem)
        {
            return (orderItem.Ord_Line_TotalBeforeTax_Amt / orderInformation.Ord_TotalBeforeTax_Amt) * (double)orderInformation.Ord_Disc_Amt;
        }

        public BaseResultModel InventoryBulkCreate(SO_OrderInformations orderInfo, List<SO_OrderItems> orderItems, string oldStatus, string requestStatus, string token)
        {
            var inventoryTransactionModels = new List<INV_TransactionModel>();
            foreach (var item in orderItems)
            {
                var inventoryTransactionModel = new INV_TransactionModel()
                {
                    OrderCode = orderInfo.OrderRefNumber,
                    Description = orderInfo.OrderDescription,
                    ItemId = item.ItemId,
                    ItemCode = item.ItemCode,
                    ItemDescription = item.ItemDescription,
                    Uom = item.UOM,
                    Quantity = item.ShippedQuantities,
                    BaseQuantity = item.ShippedBaseQuantities,
                    OrderBaseQuantity = item.OrderBaseQuantities,
                    TransactionDate = DateTime.Now,
                    WareHouseCode = orderInfo.WareHouseID,
                    DistributorCode = orderInfo.DistributorCode,
                    DSACode = orderInfo.DSAID,
                    ReasonCode = null,
                    ReasonDescription = null,
                    LocationCode = item.LocationID ?? "1"
                };
                if (oldStatus.Equals("SO_ST_OPEN") && requestStatus.Equals("SO_ST_DELIVERED")) inventoryTransactionModel.TransactionType = INV_TransactionType.SO_SHIPPED_DIRECT;
                if (oldStatus.Equals("SO_ST_WAITINGSHIPPING") && requestStatus.Equals("SO_ST_DELIVERED")) inventoryTransactionModel.TransactionType = INV_TransactionType.SO_SHIPPED_NOPICKING;
                inventoryTransactionModels.Add(inventoryTransactionModel);
            }
            _client = new RestClient(CommonData.SystemUrl.Where(x => x.Code == CommonData.SystemUrlCode.ODInventoryAPI).Select(x => x.Url).FirstOrDefault());
            _client.Authenticator = new JwtAuthenticator($"{token}");
            var json = JsonConvert.SerializeObject(inventoryTransactionModels);
            var req = new RestRequest($"InventoryTransaction/BulkCreate", Method.POST);
            req.AddHeader(OD_Constant.KeyHeader, _distributorCode);
            req.AddJsonBody(json);

            var response = _client.Execute(req);

            var resultData = JsonConvert.DeserializeObject<BaseResultModel>(JsonConvert.DeserializeObject(response.Content).ToString());
            if (!resultData.IsSuccess)
            {
                resultData.Message = "Inventory transaction: " + resultData.Message;
                resultData.Data = null;
            }
            return resultData;
        }
    }
}
