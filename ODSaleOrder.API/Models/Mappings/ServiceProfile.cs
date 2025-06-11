using AutoMapper;
using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Infrastructure.SOInfrastructure;
using SysAdmin.Models.SystemUrl;
using System;

namespace ODSaleOrder.API.Models
{
    public class ServiceProfile : Profile
    {
        public ServiceProfile()
        {
            CreateMap<Service, SystemUrlModel>().ReverseMap();
        }
    }
}
