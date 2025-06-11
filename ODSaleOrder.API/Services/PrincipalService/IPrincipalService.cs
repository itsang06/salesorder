using Sys.Common.Models;
using System.Threading.Tasks;

namespace ODSaleOrder.API.Services.PrincipalService
{
    public interface IPrincipalService
    {
        Task<bool> IsODValidation();
        Task<BaseResultModel> NavigatePrivateSchema(string distributorCode);
    }
}
