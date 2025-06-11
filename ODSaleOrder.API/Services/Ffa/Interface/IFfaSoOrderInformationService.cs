
using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Infrastructure.SOInfrastructure;
using ODSaleOrder.API.Models.SaleHistories;
using Sys.Common.Models;
using System;
using System.Collections.Generic;

namespace ODSaleOrder.API.Services.Ffa.Interface
{
    public interface IFfaSoOrderInformationService
    {
        BaseResultModel CreateFfaSoOrderInformation(FfasoOrderInformation model, string token, string username);
        BaseResultModel UpdateFfaSoOrderInformation(FfasoOrderInformation model, string token, string username);
        BaseResultModel DeleteFfaSoOrderInformation(Guid Id, string token, string username);
        BaseResultModel GetAll();
        BaseResultModel InsertMany(List<FfasoOrderInformation> model, string token, string username);
        BaseResultModel UpdateMany(List<FfasoOrderInformation> model, string token, string username);
        BaseResultModel InsertOrUpdate(List<FfasoOrderInformation> model);
        FfasoOrderInformation GetByVisitId(string visitId);
        FfasoOrderInformation GetByExternalOrdNBR(string externalOrdNBR);
        ResultCustomSale<List<FfasoOrderInformation>> GetHistoryTransactions(SyncTransactionRequest request);
        BaseResultModel GetOrderDetailByVisitId(string VisitId);
    }
}
