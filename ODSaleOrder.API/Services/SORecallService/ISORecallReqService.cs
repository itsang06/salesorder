using ODSaleOrder.API.Models;
using ODSaleOrder.API.Models.SORecallModels;
using Sys.Common.Models;
using System.Threading.Tasks;

namespace ODSaleOrder.API.Services.SORecallService
{
    public interface ISORecallReqService
    {
        Task<BaseResultModel> InsertOrderRequest(SORecallReqModel model, string username, string token, bool isSync=false);
        Task<BaseResultModel> UpdateOrderRequest(SORecallReqModel model, string username, string token);
        Task<ResultModelWithObject<SORecallReqListModel>> SearchReq(SORecallReqSearch parameters);
        Task<ResultModelWithObject<SORecallReqModel>> GetDetailReq(string code);
        Task<BaseResultModel> DeleteReq(string code, string username);
        Task<BaseResultModel> ConfirmOrderRequest(string code, string username, string token);
        Task<BaseResultModel> GetDetailRecallReqForRecall(GetDetailRecallReqForRecallModel parameters);
    }
}
