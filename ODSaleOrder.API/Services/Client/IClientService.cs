using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Models;
using Sys.Common.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ODSaleOrder.API.Services.SaleOrder.Interface
{
    public interface IClientService
    {
        T CommonRequest<T>(string urlCode, string route, RestSharp.Method method, string token, object dataRequest, bool isInputHeader = false);
        Task<T> CommonRequestAsync<T>(string urlCode, string route, RestSharp.Method method, string token, object dataRequest);
    }
}
