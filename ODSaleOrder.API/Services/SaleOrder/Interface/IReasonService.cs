using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Models;
using Sys.Common.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ODSaleOrder.API.Services.SaleOrder.Interface
{
    public interface IReasonService
    {
        Task<ResultModelWithObject<ListReasonModel>> SearchReason(EcoparamsWithGenericFilter parameters);
        Task<BaseResultModel> BulkUpsertReason(List<SO_Reason> models, string username);
        Task<BaseResultModel> CheckInUsed(string ReasonCode);
    }
}
