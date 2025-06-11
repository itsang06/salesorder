using AutoMapper;
using ODSaleOrder.API.Infrastructure;

namespace ODSaleOrder.API.Models
{
    public class AutoMapping : Profile
    {
        public AutoMapping()
        {
            CreateMap<ODOrderPendingTransModel, OrderPendingTransModel>();
        }
    }
}
