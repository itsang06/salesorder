using AutoMapper;
using ODSaleOrder.API.Infrastructure;

namespace ODSaleOrder.API.Models
{
    public class Temp_PromotionProfile : Profile
    {
        public Temp_PromotionProfile()
        {
            CreateMap<PromotionCustomerModel, ProgramCustomers>().ReverseMap();
            CreateMap<PromotionCustomerDetailsModel, ProgramCustomersDetail>().ReverseMap();

        }
    }
}
