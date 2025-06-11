using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Infrastructure.SOInfrastructure;
using ODSaleOrder.API.Models.SaleHistories;
using Sys.Common.Models;
using System;
using System.Collections.Generic;

namespace ODSaleOrder.API.Services.Ffa.Interface
{
    public interface IFfaSoOrderItemService
    {
        BaseResultModel CreateFfaSoOrderItem(FfasoOrderItem model, string token, string username);
        BaseResultModel UpdateFfaSoOrderItem(FfasoOrderItem model, string token, string username);
        BaseResultModel DeleteFfaSoOrderItem(Guid Id, string token, string username);
        BaseResultModel GetAll();
        BaseResultModel InsertMany(List<FfasoOrderItem> model, string token, string username);
        BaseResultModel UpdateMany(List<FfasoOrderItem> model, string token, string username);
        BaseResultModel DeleteMany(List<string> model, string token, string username);
        BaseResultModel DeleteByExternal_OrdNBR(string External_OrdNBR, string token, string username);
        BaseResultModel InsertOrUpdate(List<FfasoOrderItem> model);
        ResultCustomSale<List<FfasoOrderItem>> GetHistoryTransactions(SyncTransactionRequest request);
    }
}
