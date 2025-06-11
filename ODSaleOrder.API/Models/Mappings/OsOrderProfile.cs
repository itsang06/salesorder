using AutoMapper;
using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Models.OS;

namespace ODSaleOrder.API.Models
{
    public class OsOrderProfile : Profile
    {
        public OsOrderProfile()
        {
            CreateMap<OsOrderModel, OsOrderInformation>().ReverseMap();
            CreateMap<ExCreateCustomer, OsOrderModel>().ReverseMap();
        }
    }
}
