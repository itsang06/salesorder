using AutoMapper;
using DynamicSchema.Helper.Services;
using DynamicSchema.Helper.Services.Interface;
using Elasticsearch.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.Extensions.Logging;
using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Infrastructure.SOInfrastructure;
using ODSaleOrder.API.Models;
using ODSaleOrder.API.Models.SaleHistories;
using ODSaleOrder.API.Services.Base;
using Sys.Common.Models;
using SysAdmin.Models.Enum;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using static SysAdmin.API.Constants.Constant;
using static SysAdmin.Models.StaticValue.CommonData;

namespace ODSaleOrder.API.Services.SaleHistories
{
    public class SaleHistoriesService : ISaleHistoriesService
    {
        private readonly ILogger<SaleHistoriesService> _logger;
        //private readonly IBaseRepository<FfasoOrderInformation> _ffaOrderInfo;
        private readonly IBaseRepository<OrderResultModel> _orderResult;
        private readonly IBaseRepository<SaleVolumnReportModel> _saleVolumnReport;
        //private readonly IBaseRepository<SO_OrderInformations> _orderInformationRepository;
        private readonly IMapper _mapper;
        private readonly RDOSContext _dataContext;

        // Private
        private readonly IDynamicBaseRepository<SO_OrderInformations> _so_OrderInformationRepository;
        private readonly IDynamicBaseRepository<FfasoOrderInformation> _ffaSoOrderRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private string _schemaName = OD_Constant.DEFAULT_SCHEMA;
        public SaleHistoriesService(
            ILogger<SaleHistoriesService> logger, 
            IMapper mapper, 
            RDOSContext dataContext, 
            IBaseRepository<OrderResultModel> orderResult, 
            IBaseRepository<SaleVolumnReportModel> saleVolumnReport,
            IHttpContextAccessor httpContextAccessor
        )
        {
            _logger = logger;
            _mapper = mapper;
            //_ffaOrderInfo = ffaOrderInfo;
            //_orderInformationRepository = orderInformationRepository;
            _dataContext = dataContext;
            _orderResult = orderResult;
            _saleVolumnReport = saleVolumnReport;

            // Private
            _so_OrderInformationRepository = new DynamicBaseRepository<SO_OrderInformations>(dataContext);
            _ffaSoOrderRepository = new DynamicBaseRepository<FfasoOrderInformation>(dataContext);

            _httpContextAccessor = httpContextAccessor;
            _schemaName = _httpContextAccessor.HttpContext?.Items["SchemaName"] as string ?? _schemaName;
        }



        //public ResultCustomSale<SaleHistoriesModel> SaleHistories(SearchModelv2 _search)
        //{
        //    ResultCustomSale<SaleHistoriesModel> result = new ResultCustomSale<SaleHistoriesModel>();
        //    try
        //    {
        //        _search.PageSize = _search.PageSize == 0 || _search.PageSize == null ? 10 : _search.PageSize;
        //        _search.PageIndex = _search.PageIndex == 0 || _search.PageIndex == null ? 1 : _search.PageIndex;

        //        _search.FromDate = _search.FromDate == null ? DateTime.Now : _search.FromDate;
        //        _search.ToDate = _search.ToDate == null ? DateTime.Now : _search.ToDate;

        //        List<FfasoOrderInformation> items = new List<FfasoOrderInformation>();
        //        List<SO_OrderInformations> soItems = new List<SO_OrderInformations>();

        //        if (_search.CustomerId is not null && _search.ShipToId is not null)
        //        {
        //            items = _ffaOrderInfo.GetAllQueryable(e => e.RouteZoneID == _search.RouteZone && e.CustomerId == _search.CustomerId && e.CustomerShiptoID == _search.ShipToId && e.OrderDate >= _search.FromDate && e.OrderDate <= _search.ToDate).ToList();
        //            soItems = _orderInformationRepository.GetAllQueryable(e => e.RouteZoneID == _search.RouteZone && e.CustomerId == _search.CustomerId && e.CustomerShiptoID == _search.ShipToId && e.OrderDate >= _search.FromDate && e.OrderDate <= _search.ToDate).ToList();
        //        }
        //        else
        //        {
        //            items = _ffaOrderInfo.GetAllQueryable(e => e.RouteZoneID == _search.RouteZone && e.OrderDate >= _search.FromDate && e.OrderDate <= _search.ToDate).ToList();
        //            soItems = _orderInformationRepository.GetAllQueryable(e => e.RouteZoneID == _search.RouteZone && e.OrderDate >= _search.FromDate && e.OrderDate <= _search.ToDate).ToList();
        //        }

        //        if (_search.ExternalOrdNbr is not null)
        //        {
        //            items = items.Where(e => e.External_OrdNBR == _search.ExternalOrdNbr)?.ToList();
        //            soItems = soItems.Where(e => e.External_OrdNBR == _search.ExternalOrdNbr)?.ToList();

        //        }

        //        SaleHistoriesModel lst = new SaleHistoriesModel();
        //        lst.FAILED = soItems.Count(e => e.Status.Equals("SO_ST_FAILED"));
        //        lst.WAITINGSHIPPING = soItems.Count(e => e.Status.Equals("SO_ST_OPEN"));
        //        lst.CANCEL = soItems.Count(e => e.Status.Equals("SO_ST_CANCEL"));
        //        lst.DRAFT = soItems.Count(e => e.Status.Equals("SO_ST_DRAFT"));
        //        lst.SHIPPING = soItems.Count(e => e.Status.Equals("SO_ST_WAITINGSHIPPING") || e.Status.Equals("SO_ST_SHIPPING"));
        //        lst.DELIVERED = soItems.Count(e => e.Status.Equals("SO_ST_DELIVERED") || e.Status.Equals("SO_ST_PARTIALDELIVERED"));
        //        lst.NEEDCONFIRM = items.Count(e => e.Status.Equals("FFA_SO_02"));
        //        lst.WAITINGCONFIRM = items.Count(e => e.Status.Equals("FFA_SO_00"));
        //        lst.CANCELSHIPPING = items.Count(e => e.Status.Equals("FFA_SO_07"));

        //        if (string.IsNullOrEmpty(_search.Status) || _search.Status.ToLower().Equals("needconfirm") || _search.Status.ToLower().Equals("cancelshipping"))
        //        {
        //            items = items.Where(e => e.Status.Equals("FFA_SO_02") || e.Status.Equals("FFA_SO_07") || e.Status.Equals("FFA_SO_00"))?.ToList();

        //            if (!string.IsNullOrEmpty(_search.Status))
        //            {
        //                if (_search.Status.ToLower().Equals("needconfirm"))
        //                    items = items.Where(e => e.Status is not null && e.Status.Equals("FFA_SO_02"))?.ToList();
        //                else if (_search.Status.ToLower().Equals("cancelshipping"))
        //                    items = items.Where(e => e.Status is not null && e.Status.Equals("FFA_SO_07"))?.ToList();
        //            }

        //            result.TotalCountFFA = items.Count;
        //            lst.ffaOrderInfomation = items.Skip((_search.PageIndex - 1) * _search.PageSize ?? 0).Take(_search.PageSize ?? 10).ToList(); 

        //        }

        //        if (string.IsNullOrEmpty(_search.Status) || (!_search.Status.ToLower().Equals("needconfirm")) && !_search.Status.ToLower().Equals("cancelshipping"))
        //        {
        //            if(!string.IsNullOrEmpty(_search.Status))
        //            {
        //                var status = _search.Status.ToLower();
        //                if(status.Equals("failed"))
        //                    soItems = soItems.Where(e => e.Status.Equals("SO_ST_FAILED"))?.ToList();
        //                else if (status.Equals("waitingshipping"))
        //                    soItems = soItems.Where(e => e.Status.Equals("SO_ST_OPEN"))?.ToList();
        //                else if (status.Equals("cancel"))
        //                    soItems = soItems.Where(e => e.Status.Equals("SO_ST_CANCEL"))?.ToList();
        //                else if (status.Equals("draft"))
        //                    soItems = soItems.Where(e => e.Status.Equals("SO_ST_DRAFT"))?.ToList();
        //                else if (status.Equals("shipping"))
        //                    soItems = soItems.Where(e => e.Status.Equals("SO_ST_WAITINGSHIPPING") || e.Status.Equals("SO_ST_SHIPPING"))?.ToList();
        //                else if (status.Equals("delivered"))
        //                    soItems = soItems.Where(e => e.Status.Equals("SO_ST_DELIVERED") || e.Status.Equals("SO_ST_PARTIALDELIVERED"))?.ToList();
        //            }                  
        //            result.TotalCountSO = soItems.Count;
        //            lst.soOrderInfomation = soItems.Skip((_search.PageIndex - 1) * _search.PageSize ?? 0).Take(_search.PageSize ?? 10).ToList(); ;
        //        }


        //        result.Data = lst;
        //        result.Success = true;

        //    }
        //    catch (System.Exception ex)
        //    {
        //        result.Messages.Add(ex.Message);
        //    }
        //    return result;
        //}
        
        public ResultCustomSale<SaleHistoriesModel> SaleHistories(SearchModelv2 _search)
        {
            ResultCustomSale<SaleHistoriesModel> result = new ResultCustomSale<SaleHistoriesModel>();
            try
            {
                _search.PageSize = _search.PageSize == 0 || _search.PageSize == null ? 10 : _search.PageSize;
                _search.PageIndex = _search.PageIndex == 0 || _search.PageIndex == null ? 1 : _search.PageIndex;

                _search.FromDate = _search.FromDate == null ? DateTime.Now : _search.FromDate;
                _search.ToDate = _search.ToDate == null ? DateTime.Now : _search.ToDate;

                IEnumerable<FfasoOrderInformation> items = null;
                IEnumerable<SO_OrderInformations> soItems = null;


                if (_search.CustomerId is not null && _search.ShipToId is not null)
                {
                    items = _ffaSoOrderRepository.GetAllQueryable(e => e.RouteZoneID == _search.RouteZone && e.CustomerId == _search.CustomerId && e.CustomerShiptoID == _search.ShipToId && e.OrderDate >= _search.FromDate && e.OrderDate <= _search.ToDate, null, null, _schemaName);
                    soItems = _so_OrderInformationRepository.GetAllQueryable(e => e.RouteZoneID == _search.RouteZone && e.CustomerId == _search.CustomerId && e.CustomerShiptoID == _search.ShipToId 
                                                                          && e.OrderDate >= _search.FromDate && e.OrderDate <= _search.ToDate && e.Source.Equals("FFA"), null, null, _schemaName);
                }
                else
                {
                    items = _ffaSoOrderRepository.GetAllQueryable(e => e.RouteZoneID == _search.RouteZone && e.OrderDate >= _search.FromDate && e.OrderDate <= _search.ToDate, null, null, _schemaName);
                    soItems = _so_OrderInformationRepository.GetAllQueryable(e => e.RouteZoneID == _search.RouteZone 
                                                                          && e.OrderDate >= _search.FromDate && e.OrderDate <= _search.ToDate && e.Source.Equals("FFA"), null, null, _schemaName);
                }

                if (_search.ExternalOrdNbr is not null)
                {
                    items = items.Where(e => e.External_OrdNBR == _search.ExternalOrdNbr);
                }

                SaleHistoriesModel lst = new SaleHistoriesModel();
                lst.FAILED = soItems.Count(e => e.Status.Equals("SO_ST_FAILED"));
                lst.WAITINGSHIPPING = soItems.Count(e => e.Status.Equals("SO_ST_OPEN"));
                lst.CANCEL = soItems.Count(e => e.Status.Equals("SO_ST_CANCEL"));
                lst.DRAFT = soItems.Count(e => e.Status.Equals("SO_ST_DRAFT"));
                lst.SHIPPING = soItems.Count(e => e.Status.Equals("SO_ST_WAITINGSHIPPING") || e.Status.Equals("SO_ST_SHIPPING"));
                lst.DELIVERED = soItems.Count(e => e.Status.Equals("SO_ST_DELIVERED") || e.Status.Equals("SO_ST_PARTIALDELIVERED"));
                lst.NEEDCONFIRM = items.Count(e => e.Status.Equals("FFA_SO_02"));
                lst.WAITINGCONFIRM = items.Count(e => e.Status.Equals("FFA_SO_00"));
                lst.CANCELSHIPPING = items.Count(e => e.Status.Equals("FFA_SO_07"));

                if (string.IsNullOrEmpty(_search.Status))
                {
                    var ffaItems = items.Where(e => e.Status is not null && (e.Status.Equals("FFA_SO_02") || e.Status.Equals("FFA_SO_07") || e.Status.Equals("FFA_SO_00")));
                    var joinSOItems = this.JoinOrderListByStatus(items?.Where(e => e.Status is not null && e.Status.Equals("FFA_SO_01")).ToList(), soItems?.ToList(), new List<string> { "SO_ST_FAILED", "SO_ST_OPEN", "SO_ST_CANCEL", "SO_ST_DRAFT", "SO_ST_WAITINGSHIPPING", "SO_ST_SHIPPING", "SO_ST_DELIVERED", "SO_ST_PARTIALDELIVERED" });

                    IEnumerable<FfasoOrderInformation> combined = ffaItems.Union(joinSOItems);
                    items = combined.ToList(); 
                }
                else
                {
                    var status = _search.Status.ToLower();
                    switch (status)
                    {
                        case "needconfirm":
                            items = items.Where(e => e.Status is not null && e.Status.Equals("FFA_SO_02"));
                            break;
                        case "cancelshipping":
                            items = items.Where(e => e.Status is not null && e.Status.Equals("FFA_SO_07"));
                            break;
                        case "failed":
                            items = this.JoinOrderListByStatus(items?.Where(e => e.Status is not null && e.Status.Equals("FFA_SO_01")).ToList(), soItems?.ToList(), new List<string> { "SO_ST_FAILED" });
                            break;
                        case "waitingshipping":
                            items = this.JoinOrderListByStatus(items?.Where(e => e.Status is not null && e.Status.Equals("FFA_SO_01")).ToList(), soItems?.ToList(), new List<string> { "SO_ST_OPEN" });
                            break;
                        case "cancel":
                            items = this.JoinOrderListByStatus(items?.Where(e => e.Status is not null && e.Status.Equals("FFA_SO_01")).ToList(), soItems?.ToList(), new List<string> { "SO_ST_CANCEL" });
                            break;
                        case "draft":
                            items = this.JoinOrderListByStatus(items?.Where(e => e.Status is not null && e.Status.Equals("FFA_SO_01")).ToList(), soItems?.ToList(), new List<string> { "SO_ST_DRAFT" });
                            break;
                        case "shipping":
                            items = this.JoinOrderListByStatus(items?.Where(e => e.Status is not null && e.Status.Equals("FFA_SO_01")).ToList(), soItems?.ToList(), new List<string> { "SO_ST_WAITINGSHIPPING", "SO_ST_SHIPPING" });
                            break;
                        case "delivered":
                            items = this.JoinOrderListByStatus(items?.Where(e => e.Status is not null && e.Status.Equals("FFA_SO_01")).ToList(), soItems?.ToList(), new List<string> { "SO_ST_DELIVERED", "SO_ST_PARTIALDELIVERED" });
                            break;
                    }

                }

                result.TotalCount = items?.ToList()?.Count ?? 0;
                lst.OrderList = items.Skip((_search.PageIndex - 1) * _search.PageSize ?? 0).Take(_search.PageSize ?? 10).ToList();

                result.Data = lst;
                result.Success = true;

            }
            catch (System.Exception ex)
            {
                result.Messages.Add(ex.Message);
            }
            return result;
        }

        public BaseResultModel OrderResult(string EmployeeCode, string VisitDate)
        {
            var _query = $@"SELECT * from public.""ordersresultinday""('{EmployeeCode}', '{VisitDate}')";
            var res = _orderResult.GetByFunction(_query).ToList();
            return new BaseResultModel
            {
                IsSuccess = true,
                Code = 200,
                Message = "OK",
                Data = res
            };
        }

        public BaseResultModel SalesVolumnReport(SaleVolumnReportRequest _search)
        {
            var _query = $@"SELECT * from public.""Func_FFA_SalesVolumnReport""('{_search.EmployeeCode}', '{_search.FromDate}', '{_search.ToDate}', 'PurchaseUnit')";
            var res = _saleVolumnReport.GetByFunction(_query).ToList();
            return new BaseResultModel
            {
                IsSuccess = true,
                Code = 200,
                Message = "OK",
                Data = res
            };
        }

        private IEnumerable<FfasoOrderInformation> JoinOrderListByStatus(IEnumerable<FfasoOrderInformation> ffaList, IEnumerable<SO_OrderInformations> soList, List<string> statusList)
        {
            return from f in ffaList
                   join k in soList on new
                   {
                       f.External_OrdNBR,
                       f.OrderRefNumber,
                       f.OrderType
                   }
                   equals new
                   {
                       k.External_OrdNBR,
                       k.OrderRefNumber,
                       k.OrderType
                   }
                   where statusList.Contains(k.Status)
                   select new FfasoOrderInformation()
                   {
                       Id = f.Id,
                       OrderRefNumber = f.OrderRefNumber,
                       VisitID = f.VisitID,
                       External_OrdNBR = f.External_OrdNBR,
                       NotInSubRoute = f.NotInSubRoute,
                       IsDirect = f.IsDirect,
                       OrderType = f.OrderType,
                       PeriodID = f.PeriodID,
                       WareHouseID = f.WareHouseID,
                       WareHouseDescription = f.WareHouseDescription,
                       PrincipalID = f.PrincipalID,
                       DistributorCode = f.DistributorCode,
                       VisitDate = f.VisitDate,
                       OrderDate = f.OrderDate,
                       ExpectShippedDate = f.ExpectShippedDate,
                       Status = k.Status,
                       SalesOrgID = f.SalesOrgID,
                       TerritoryStrID = f.TerritoryStrID,
                       TerritoryValueKey = f.TerritoryValueKey,
                       AreaId = f.AreaId,
                       BranchId = f.BranchId,
                       RegionId = f.RegionId,
                       SubAreaId = f.SubAreaId,
                       SubRegionId = f.SubRegionId,
                       DSAID = f.DSAID,
                       NSD_ID = f.NSD_ID,
                       Branch_Manager_ID = f.Branch_Manager_ID,
                       Region_Manager_ID = f.Region_Manager_ID,
                       Sub_Region_Manager_ID = f.Sub_Region_Manager_ID,
                       Area_Manager_ID = f.Area_Manager_ID,
                       Sub_Area_Manager_ID = f.Sub_Area_Manager_ID,
                       DSA_Manager_ID = f.DSA_Manager_ID,
                       RZ_Suppervisor_ID = f.RZ_Suppervisor_ID,
                       SIC_ID = f.SIC_ID,
                       SalesRepID = f.SalesRepID,
                       SalesRepName = f.SalesRepName,
                       SalesRepPhone = f.SalesRepPhone,
                       RouteZoneID = f.RouteZoneID,
                       RouteZOneType = f.RouteZOneType,
                       RouteZonelocation = f.RouteZonelocation,
                       Created_By = f.Created_By,
                       Owner_ID = f.Owner_ID,
                       CustomerId = f.CustomerId,
                       CustomerShiptoID = f.CustomerShiptoID,
                       CustomerShiptoName = f.CustomerShiptoName,
                       CustomerName = f.CustomerName,
                       CustomerAddress = f.CustomerAddress,
                       CustomerPhone = f.CustomerPhone,
                       Shipto_Attribute1 = f.Shipto_Attribute1,
                       Shipto_Attribute2 = f.Shipto_Attribute2,
                       Shipto_Attribute3 = f.Shipto_Attribute3,
                       Shipto_Attribute4 = f.Shipto_Attribute4,
                       Shipto_Attribute5 = f.Shipto_Attribute5,
                       Shipto_Attribute6 = f.Shipto_Attribute6,
                       Shipto_Attribute7 = f.Shipto_Attribute7,
                       Shipto_Attribute8 = f.Shipto_Attribute8,
                       Shipto_Attribute9 = f.Shipto_Attribute9,
                       Shipto_Attribute10 = f.Shipto_Attribute10,
                       Source = f.Source,
                       Orig_Ord_SKUs = f.Orig_Ord_SKUs,
                       Orig_Ord_Qty = f.Orig_Ord_Qty,
                       Orig_Promotion_Qty = f.Orig_Promotion_Qty,
                       Orig_Ord_Amt = f.Orig_Ord_Amt,
                       Promotion_Amt = f.Promotion_Amt,
                       Orig_Ord_Disc_Amt = f.Orig_Ord_Disc_Amt,
                       Orig_Ordline_Disc_Amt = f.Orig_Ordline_Disc_Amt,
                       Orig_Ord_Extend_Amt = f.Orig_Ord_Extend_Amt,
                       DiscountID = f.DiscountID,
                       DiscountDescription = f.DiscountDescription,
                       DiscountType = f.DiscountType,
                       ExpectDeliveryNote = f.ExpectDeliveryNote,
                       TotalLine = f.TotalLine,
                       OrderDescription = f.OrderDescription,
                       Note = f.Note,
                       DeliveryTimeType = f.DeliveryTimeType,
                       DeliveryTimeTypeDesc = f.DeliveryTimeTypeDesc,
                       DeliveryTime = f.DeliveryTime,
                       DeliveryMethod = f.DeliveryMethod,
                       DeliveryMethodDesc = f.DeliveryMethodDesc,
                       PaymentType = f.PaymentType,
                       PaymentTypeDesc = f.PaymentTypeDesc,
                       WaittingBudget = f.WaittingBudget,
                       WaittingStock = f.WaittingStock,
                       AllowRemoveFreeItem = f.AllowRemoveFreeItem,
                       IsDeleted = f.IsDeleted,
                       CreatedBy = f.CreatedBy,
                       CreatedDate = f.CreatedDate,
                       UpdatedBy = f.UpdatedBy,
                       UpdatedDate = f.UpdatedDate,
                       ImportStatus = f.ImportStatus,
                       ReasonCode = f.ReasonCode,
                       IsSplitOrder = f.IsSplitOrder
                   };
        }
    }
}
