using ODSaleOrder.API.Infrastructure;
using Sys.Common.Models;
using System.Collections.Generic;

namespace ODSaleOrder.API.Services.Ffa.Interface
{
    public interface IFFASoSuggestOrderService
    {
        BaseResultModel InsertOrUpdate(List<FFASoSuggestOrder> model);
    }
}