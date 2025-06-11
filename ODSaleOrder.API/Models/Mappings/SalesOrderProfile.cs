using AutoMapper;
using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Infrastructure.SOInfrastructure;
using ODSaleOrder.API.Models.SORecallModels;
using ODSaleOrder.API.Models.SyncHistory;
using System;

namespace ODSaleOrder.API.Models
{
    public class SalesOrderProfile : Profile
    {
        public SalesOrderProfile()
        {
            CreateMap<SO_OrderInformations, SaleOrderModel>().ReverseMap();
            // CreateMap<SO_OrderInformations, SaleOrderModel>().ReverseMap();
            CreateMap<FfasoOrderInformation, SaleOrderModel>().ReverseMap();
                //.ForMember(d => d.External_OrdNBR, o => o.MapFrom(x => x.ReferenceRefNbr))
                //.ForMember(d => d.DeliveryTime, o => o.MapFrom(x => x.ExpectDeliveryNote));
            CreateMap<SO_OrderItems, FfasoOrderItem>().ReverseMap()
                .ForMember(d => d.KitId, o => o.MapFrom(x => Guid.Empty))
                .ForMember(d => d.OriginalOrderQuantities, o => o.MapFrom(x => x.OriginalOrderQty))
                .ForMember(d => d.OriginalOrderBaseQuantities, o => o.MapFrom(x => x.OriginalOrderBaseQty))
                .ForMember(d => d.OrderQuantities, o => o.MapFrom(x => x.OriginalOrderQty))
                .ForMember(d => d.OrderBaseQuantities, o => o.MapFrom(x => x.OriginalOrderBaseQty))
                .ForMember(d => d.Ord_Line_Amt, o => o.MapFrom(x => x.Orig_Ord_Line_Amt))
                .ForMember(d => d.Ord_line_Disc_Amt, o => o.MapFrom(x => x.Orig_Ord_line_Disc_Amt))
                .ForMember(d => d.Ord_Line_Extend_Amt, o => o.MapFrom(x => x.Orig_Ord_Line_Extend_Amt))
                .ForMember(d => d.BaseUomCode, o => o.MapFrom(x => x.BaseUnitCode))
                .ForMember(d => d.ProgramCustomersDetailDesc, o => o.MapFrom(x => x.PromotionLevelDescription))
                .ForMember(d => d.PromotionLevel, o => o.MapFrom(x => x.PromotionLevelCode)).ReverseMap();

            CreateMap<FfasoOrderItem, FfasoOrderItem>().ReverseMap();
            CreateMap<SoorderRecallReq, SORecallReqModel>().ReverseMap();
            CreateMap<StagingSyncDataHistory, StagingSyncDataHistoryModel>().ReverseMap();
            CreateMap<SoorderRecall, SORecallModel>().ReverseMap();
        }
    }
}
