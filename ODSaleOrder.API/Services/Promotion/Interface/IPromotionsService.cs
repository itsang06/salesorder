using ODSaleOrder.API.Controllers;
using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Models;
using Sys.Common.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ODSaleOrder.API.Services.SaleOrder.Interface
{
    public interface IPromotionsService
    {
        Task<BaseResultModel> GetListProgramsByCustomerID(CustomerPromotionRequestModel cusInfo, string PromotionRefNumber, string ProgramType, string token);
        // Task<List<Temp_ProgramCustomerDetailsItems>> GetInventoryItemStdByItemGroupByQuantity(Temp_PromotionCustomerDetailsModel programCustomerDetail, string programCustomersDetailCode, bool isDiscount, string username, string token, DistributorInfoModel distributorInfo, string promotionType);
        // Task<BaseResultModel> GenRefPromotionNumber();
        Task<BaseResultModel> GenRefPromotionNumber(PromoRefRequestModel model);
        Task<BaseResultModel> UpsertMutipleCustomerProgram(List<PromotionCustomerModel> models, bool includeSaved, string username, string token);
        Task<List<ItemMng_InventoryItem>> GetRewardItemChange(string promotionCode, string detailCode, List<string> excludedProduct, string token);
        Task<BaseResultModel> ImportDataFromFFA(SaleOrderModel model, string token);
        Task<int> CommonGetQtyFromUnitToUnit(List<UomConversionModel> UomConversion, string fromUnit, string toUnit, int qty);
        Task<int> CommonGetQtyFromUnitIdToUnitId(List<UomConversionModel> UomConversion, Guid fromUnit, Guid toUnit, int qty);
    }
}
