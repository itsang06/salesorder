using Sys.Common.Models;
using System.Threading.Tasks;

namespace ODSaleOrder.API.Services.BaseLine
{
    public interface IBaseLineService
    {
        Task<BaseResultModel> HandleCalculateBaselineDate();
    }
}
