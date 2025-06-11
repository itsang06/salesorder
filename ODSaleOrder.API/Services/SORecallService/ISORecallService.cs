using ODSaleOrder.API.Models;
using ODSaleOrder.API.Models.SORecallModels;
using Sys.Common.Models;
using System.Threading.Tasks;

namespace ODSaleOrder.API.Services.SORecallService
{
    public interface ISORecallService
    {
        Task<BaseResultModel> InsertOrderRecall(SORecallModel model, string username, string token);
        Task<ResultModelWithObject<SORecallModel>> GetDetail(string code);
        Task<BaseResultModel> UpdateOrderRecall(SORecallModel model, string username, string token);
        Task<ResultModelWithObject<SORecallListModel>> Search(SORecallSearch parameters);
        Task<BaseResultModel> Delete(string code, string username);
        Task<BaseResultModel> ConfirmOrder(string code, string username, string token);
    }
}
