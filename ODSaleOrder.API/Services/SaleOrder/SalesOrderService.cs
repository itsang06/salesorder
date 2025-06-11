using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sys.Common.Models;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Sys.Common.Helper;
using ODSaleOrder.API.Services.SaleOrder.Interface;
using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Models;
using static SysAdmin.API.Constants.Constant;
using RestSharp;
using RestSharp.Authenticators;
using Newtonsoft.Json;
using SysAdmin.Models.StaticValue;
using static SysAdmin.Models.StaticValue.CommonData;
using ODSaleOrder.API.Infrastructure.SOInfrastructure;
using RDOS.INVAPI.Infratructure;
using ODSaleOrder.API.Models.ReportModel;
using ODSaleOrder.API.Constants;
using DynamicSchema.Helper.Services.Interface;
using DynamicSchema.Helper.Services;
using System.Linq.Expressions;
using System.Linq.Dynamic.Core;
using ODSaleOrder.API.Services.OrderStatusHistoryService;
using ODSaleOrder.API.Models.OS;
using ODSaleOrder.API.Services.OneShop.Interface;
using ODSaleOrder.API.Services.Base;
using ODSaleOrder.API.Services.Inventory;
using System.Linq.Dynamic.Core.Tokenizer;
using static SyncToStaging.Helper.Constants.SyncToStagingHelperConsts;
using Newtonsoft.Json.Linq;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using ODSaleOrder.API.Services.CaculateTax;
using ODSaleOrder.API.Models.SalesOrder;
using nProx.Helpers.Dapper;
using DocumentFormat.OpenXml.Drawing.Charts;

namespace ODSaleOrder.API.Services.SaleOrder
{
    public class SalesOrderService : ISalesOrderService
    {
        // Private Repository
        private readonly IDynamicBaseRepository<SO_FirstTimeCustomer> _firstTimeCustomerRepository;
        private readonly IDynamicBaseRepository<SO_OrderInformations> _orderInformationsRepository;
        private readonly IDynamicBaseRepository<SO_SumPickingListHeader> _sumPickingListHeaderRepository;
        private readonly IDynamicBaseRepository<SO_SumPickingListDetail> _sumPickingListDetailRepository;
        private readonly IDynamicBaseRepository<SO_OrderItems> _orderItemsRepository;
        private readonly IDynamicBaseRepository<FfasoOrderItem> _ffasoOrderItemRepository;
        private readonly IDynamicBaseRepository<FfasoOrderInformation> _ffasoOrderInformationRepository;
        private readonly IDynamicBaseRepository<ProgramCustomersDetail> _customerProgramDetailRepo;
        private readonly IDynamicBaseRepository<ProgramCustomers> _customerProgramRepo;
        private readonly IDynamicBaseRepository<SO_SalesOrderSetting> _settingRepository;
        private readonly IDynamicBaseRepository<INV_AllocationDetail> _allocationDetailRepo;
        private readonly IDynamicBaseRepository<InvAllocationTracking> _alocationtrackinglogRepo;
        private readonly IDynamicBaseRepository<INV_InventoryTransaction> _invTransactionRepo;
        private readonly IDynamicBaseRepository<SaleCalendar> _salesCalendarRepo;
        private readonly IDynamicBaseRepository<SaleCalendarGenerate> _saleCalendarGenerateRepo;
        private readonly ICalculateTaxService _caculateTaxService;

        // Public Repository
        private readonly IDynamicBaseRepository<SO_Reason> _reasonRepository;
        private readonly IDynamicBaseRepository<Principal> _principalRepo;

        // Client
        public IRestClient _client;
        public IRestClient _clientINV;
        public IRestClient _clientSalesConfig;
        protected readonly RDOSContext _dataContext;

        // Service
        private readonly ILogger<SalesOrderService> _logger;
        private readonly IMapper _mapper;
        protected readonly IPromotionsService _promoService;
        public readonly IClientService _clientService;
        private readonly IOrderStatusHistoryService _orderStatusHisService;
        private readonly IOSNotificationService _osNotifiService;
        private readonly IInventoryService _inventoryService;

        //Khoa enhance
        private readonly ICalculateTaxService _calculateTaxService;

        // Other
        public string _token;
        public string _saleOrderNumber;
        private List<BookingStockReqModel> ReqModel = new List<BookingStockReqModel>();

        //Khoa enhance 
        private readonly IDynamicBaseRepository<FfadsSoLot> _ffadsSoLotRepo;
        private readonly IDynamicBaseRepository<FfadsSoPayment> _ffadsSoPaymentRepo;

        private readonly IHttpContextAccessor _httpContextAccessor;
        private string _schemaName = OD_Constant.DEFAULT_SCHEMA;
        private string _distributorCode = null;
        private readonly IDapperRepositories _dapperRepositories;
        
        public SalesOrderService(ILogger<SalesOrderService> logger,
            IMapper mapper,
            IClientService clientService,
            IPromotionsService promoService,
            RDOSContext dataContext,
            IOrderStatusHistoryService orderStatusHisService,
            IOSNotificationService osNotifiService,
            IInventoryService inventoryService,
            IHttpContextAccessor httpContextAccessor,
            ICalculateTaxService caculateTaxService,
            IDapperRepositories dapperRepositories,
            ICalculateTaxService calculateTaxService
            )
        {
            // Private
            _firstTimeCustomerRepository = new DynamicBaseRepository<SO_FirstTimeCustomer>(dataContext);
            _sumPickingListHeaderRepository = new DynamicBaseRepository<SO_SumPickingListHeader>(dataContext);
            _sumPickingListDetailRepository = new DynamicBaseRepository<SO_SumPickingListDetail>(dataContext);
            _orderInformationsRepository = new DynamicBaseRepository<SO_OrderInformations>(dataContext);
            _orderItemsRepository = new DynamicBaseRepository<SO_OrderItems>(dataContext);
            _ffasoOrderItemRepository = new DynamicBaseRepository<FfasoOrderItem>(dataContext);
            _ffasoOrderInformationRepository = new DynamicBaseRepository<FfasoOrderInformation>(dataContext);
            _customerProgramDetailRepo = new DynamicBaseRepository<ProgramCustomersDetail>(dataContext);
            _customerProgramRepo = new DynamicBaseRepository<ProgramCustomers>(dataContext);
            _allocationDetailRepo = new DynamicBaseRepository<INV_AllocationDetail>(dataContext);
            _alocationtrackinglogRepo = new DynamicBaseRepository<InvAllocationTracking>(dataContext);
            _settingRepository = new DynamicBaseRepository<SO_SalesOrderSetting>(dataContext);
            _invTransactionRepo = new DynamicBaseRepository<INV_InventoryTransaction>(dataContext);
            _salesCalendarRepo = new DynamicBaseRepository<SaleCalendar>(dataContext);
            _saleCalendarGenerateRepo = new DynamicBaseRepository<SaleCalendarGenerate>(dataContext);

            // Public
            _reasonRepository = new DynamicBaseRepository<SO_Reason>(dataContext);
            _principalRepo = new DynamicBaseRepository<Principal>(dataContext);

            // Service
            _logger = logger;
            _mapper = mapper;
            _dataContext = dataContext;
            _clientService = clientService;
            _promoService = promoService;
            _orderStatusHisService = orderStatusHisService;
            _osNotifiService = osNotifiService;
            _inventoryService = inventoryService;

            // Client
            _clientINV = new RestClient(CommonData.SystemUrl.Where(x => x.Code == CommonData.SystemUrlCode.ODInventoryAPI).Select(x => x.Url).FirstOrDefault());
            _clientSalesConfig = new RestClient(CommonData.SystemUrl.Where(x => x.Code == CommonData.SystemUrlCode.SalesOrgAPI).Select(x => x.Url).FirstOrDefault());

            //set timeout for dataContect:
            _dataContext.Database.SetCommandTimeout(360);

            //Khoa enhance
            _ffadsSoLotRepo = new DynamicBaseRepository<FfadsSoLot>(dataContext);
            _ffadsSoPaymentRepo = new DynamicBaseRepository<FfadsSoPayment>(dataContext);

            _calculateTaxService = calculateTaxService;

            //DangMNN enhance
            _caculateTaxService = caculateTaxService;
            _dapperRepositories = dapperRepositories;

            _httpContextAccessor = httpContextAccessor;
            _schemaName = _httpContextAccessor.HttpContext?.Items["SchemaName"] as string ?? _schemaName;
            _distributorCode = _httpContextAccessor.HttpContext?.Items["DistributorCode"] as string ?? _distributorCode;
        }

        #region Common function
        public async Task<List<SO_OrderItems>> CommonGetOrderDetailsRefNumber(List<string> refnumbers)
        {
            return
             await _orderItemsRepository.GetAllQueryable(null, null, null, _schemaName).Where(x => !x.IsDeleted &&
                !string.IsNullOrWhiteSpace(x.OrderRefNumber) && refnumbers.Contains(x.OrderRefNumber)).AsNoTracking().ToListAsync();
        }


        public async Task<List<SaleOrderBaseModel>> CommonGetDetail(SaleOrderDetailQueryModel query)
        {
            try
            {
                var baseModel = await (from header in _orderInformationsRepository.GetAllQueryable(null, null, null, _schemaName).AsNoTracking()
                                       join detail in _orderItemsRepository.GetAllQueryable(null, null, null, _schemaName).AsNoTracking() on header.OrderRefNumber equals detail.OrderRefNumber into data
                                       from detail in data.DefaultIfEmpty()
                                       where header.OrderRefNumber == query.OrderRefNumber && header.DistributorCode == query.DistributorCode && !header.IsDeleted
                                       select new SaleOrderBaseModel
                                       {
                                           OrderInformation = header,
                                           OrderItem = detail
                                       }).AsNoTracking().AsSplitQuery().ToListAsync();
                return baseModel;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new List<SaleOrderBaseModel>();
            }
        }

        public async Task<List<SO_OrderItems>> CommonGetItemsByOrderRefNumbers(List<string> OrderRefNumbers)
        {
            var Items = await _orderItemsRepository.GetAllQueryable(null, null, null, _schemaName)
                .Where(x => OrderRefNumbers.Contains(x.OrderRefNumber)).ToListAsync();
            return Items;
        }

        public async Task<IEnumerable<SaleOrderBaseModel>> CommonGetAllQueryable(SaleOrderSearchParamsModel parameters)
        {
            try
            {
                var query = (from header in _orderInformationsRepository.GetAllQueryable(null, null, null, _schemaName)
                             .AsNoTracking()
                             join detail in _orderItemsRepository.GetAllQueryable(null, null, null, _schemaName).AsNoTracking() on header.OrderRefNumber equals detail.OrderRefNumber into data
                             from detail in data.DefaultIfEmpty()
                             where (parameters.OrderRefNumbers != null && parameters.OrderRefNumbers.Count > 0 ? parameters.OrderRefNumbers.Contains(header.OrderRefNumber) : true) &&
                             (parameters.ListDistributor != null && parameters.ListDistributor.Count > 0 ? parameters.ListDistributor.Contains(header.DistributorCode) : parameters.DistributorCode != null ? header.DistributorCode == parameters.DistributorCode : true) &&
                             (parameters.UpdatedDate.HasValue ? header.UpdatedDate.HasValue && header.UpdatedDate.Value.Date == parameters.UpdatedDate.Value.Date : true) &&
                             (parameters.StatusFilter != null && parameters.StatusFilter.Count > 0 ? parameters.StatusFilter.Contains(header.Status) : true) &&
                             (parameters.OrderDate.HasValue ? parameters.OrderDate.Value.Date == header.OrderDate.Date : true) &&
                             (parameters.FromDate.HasValue ? parameters.FromDate.Value.Date <= header.OrderDate.Date : true) &&
                             (parameters.ToDate.HasValue ? parameters.ToDate.Value.Date >= header.OrderDate.Date : true) &&
                             !header.IsDeleted
                             select new SaleOrderBaseModel
                             {
                                 OrderInformation = header,
                                 OrderItem = detail
                             }).AsNoTracking();

                if (parameters.Filters != null && parameters.Filters.Count > 0)
                {
                    query = await MappingQuerySO(query, parameters.Filters);
                }

                if (!string.IsNullOrWhiteSpace(parameters.SearchValue))
                {
                    query = query.Where(x =>
                        (!string.IsNullOrWhiteSpace(x.OrderInformation.CustomerId) && x.OrderInformation.CustomerId.ToLower().Trim().Contains(parameters.SearchValue.ToLower().Trim())) ||
                        (!string.IsNullOrWhiteSpace(x.OrderInformation.OrderRefNumber) && x.OrderInformation.OrderRefNumber.ToLower().Trim().Contains(parameters.SearchValue.ToLower().Trim())) ||
                        (!string.IsNullOrWhiteSpace(x.OrderInformation.CustomerName) && x.OrderInformation.CustomerName.ToLower().Trim().Contains(parameters.SearchValue.ToLower().Trim())) ||
                        (!string.IsNullOrWhiteSpace(x.OrderInformation.ReferenceRefNbr) && x.OrderInformation.ReferenceRefNbr.ToLower().Trim().Contains(parameters.SearchValue.ToLower().Trim()))
                    );
                }

                // List<SaleOrderBaseModel> query = query.ToList();
                if (parameters.OutletFilters != null && parameters.OutletFilters.Count > 0)
                {
                    List<string> SelectedItems = new List<string>();
                    foreach (var item in parameters.OutletFilters)
                    {
                        var result = query.Where(x => x.OrderInformation != null && !string.IsNullOrWhiteSpace(x.OrderInformation.CustomerId) && x.OrderInformation.CustomerId == item.CustomerCode && !string.IsNullOrWhiteSpace(x.OrderInformation.CustomerShiptoID) && x.OrderInformation.CustomerShiptoID == item.ShiptoCode).Select(x => x.OrderInformation.OrderRefNumber).ToList();

                        if (result != null && result.Count > 0)
                        {
                            SelectedItems.AddRange(result);
                        }
                    }
                    query = query.Where(x => SelectedItems.Contains(x.OrderInformation.OrderRefNumber));
                }
                return query.ToList();
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return null;
            }
        }

        public async Task<IEnumerable<SaleOrderBaseModel>> CommonGetAllQueryableWODistributor(SaleOrderSearchParamsModel parameters, List<string> Distributors)
        {
            try
            {
                var query = (from header in _orderInformationsRepository.GetAllQueryable(null, null, null, _schemaName).AsNoTracking()
                             join detail in _orderItemsRepository.GetAllQueryable(null, null, null, _schemaName).AsNoTracking() on header.OrderRefNumber equals detail.OrderRefNumber into data
                             from detail in data.DefaultIfEmpty()
                             where (parameters.OrderRefNumbers != null && parameters.OrderRefNumbers.Count > 0 ? parameters.OrderRefNumbers.Contains(header.OrderRefNumber) : true) &&
                             Distributors.Contains(header.DistributorCode) &&
                             (parameters.ListDistributor != null && parameters.ListDistributor.Count > 0 ? parameters.ListDistributor.Contains(header.DistributorCode) : parameters.DistributorCode != null ? header.DistributorCode == parameters.DistributorCode : true) &&
                             (parameters.UpdatedDate.HasValue ? header.UpdatedDate.HasValue && header.UpdatedDate.Value.Date == parameters.UpdatedDate.Value.Date : true) &&
                             (parameters.StatusFilter != null && parameters.StatusFilter.Count > 0 ? parameters.StatusFilter.Contains(header.Status) : true) &&
                             (parameters.OrderDate.HasValue ? parameters.OrderDate.Value.Date == header.OrderDate.Date : true) &&
                             (parameters.FromDate.HasValue ? parameters.FromDate.Value.Date <= header.OrderDate.Date : true) &&
                             (parameters.ToDate.HasValue ? parameters.ToDate.Value.Date >= header.OrderDate.Date : true) &&
                             !header.IsDeleted
                             select new SaleOrderBaseModel
                             {
                                 OrderInformation = header,
                                 OrderItem = detail
                             }).AsNoTracking();


                if (parameters.Filters != null && parameters.Filters.Count > 0)
                {
                    query = await MappingQuerySO(query, parameters.Filters);
                }


                if (!string.IsNullOrWhiteSpace(parameters.SearchValue))
                {
                    query = query.Where(x =>
                        (!string.IsNullOrWhiteSpace(x.OrderInformation.CustomerId) && x.OrderInformation.CustomerId.ToLower().Trim().Contains(parameters.SearchValue.ToLower().Trim())) ||
                        (!string.IsNullOrWhiteSpace(x.OrderInformation.OrderRefNumber) && x.OrderInformation.OrderRefNumber.ToLower().Trim().Contains(parameters.SearchValue.ToLower().Trim())) ||
                        (!string.IsNullOrWhiteSpace(x.OrderInformation.CustomerName) && x.OrderInformation.CustomerName.ToLower().Trim().Contains(parameters.SearchValue.ToLower().Trim())) ||
                        (!string.IsNullOrWhiteSpace(x.OrderInformation.ReferenceRefNbr) && x.OrderInformation.ReferenceRefNbr.ToLower().Trim().Contains(parameters.SearchValue.ToLower().Trim()))
                    );
                }

                // List<SaleOrderBaseModel> query = query.ToList();
                if (parameters.OutletFilters != null && parameters.OutletFilters.Count > 0)
                {
                    List<string> SelectedItems = new List<string>();
                    foreach (var item in parameters.OutletFilters)
                    {
                        var result = query.Where(x => x.OrderInformation != null && !string.IsNullOrWhiteSpace(x.OrderInformation.CustomerId) && x.OrderInformation.CustomerId == item.CustomerCode && !string.IsNullOrWhiteSpace(x.OrderInformation.CustomerShiptoID) && x.OrderInformation.CustomerShiptoID == item.ShiptoCode).Select(x => x.OrderInformation.OrderRefNumber).ToList();

                        if (result != null && result.Count > 0)
                        {
                            SelectedItems.AddRange(result);
                        }
                    }
                    query = query.Where(x => SelectedItems.Contains(x.OrderInformation.OrderRefNumber));
                }
                return query.ToList();


                // var res = (from header in _orderInformationsRepository.GetAllQueryable().AsNoTracking()
                //            join detail in _orderItemsRepository.GetAllQueryable().AsNoTracking() on header.OrderRefNumber equals detail.OrderRefNumber into data
                //            from detail in data.DefaultIfEmpty()
                //            where (parameters.OrderRefNumbers != null && parameters.OrderRefNumbers.Count > 0 ? parameters.OrderRefNumbers.Contains(header.OrderRefNumber) : true) && Distributors.Contains(header.DistributorCode) && !header.IsDeleted
                //            select new SaleOrderBaseModel
                //            {
                //                OrderInformation = header,
                //                OrderItem = detail
                //            }).AsNoTracking().ToList();

                // if (parameters.Filters != null && parameters.Filters.Count > 0)
                // {
                //     foreach (var filter in parameters.Filters)
                //     {
                //         var getter = typeof(SO_OrderInformations).GetProperty(filter.Property);
                //         res = res.Where(x =>
                //             filter.Values.Any(a => a == "" || a == null) ?
                //                 string.IsNullOrEmpty(getter.GetValue(x.OrderInformation, null).EmptyIfNull().ToString()) || filter.Values.Contains(getter.GetValue(x.OrderInformation, null).ToString().ToLower().Trim()) :
                //                 !string.IsNullOrEmpty(getter.GetValue(x.OrderInformation, null).EmptyIfNull().ToString()) && filter.Values.Contains(getter.GetValue(x.OrderInformation, null).ToString().ToLower().Trim())).ToList();
                //     }
                // }
                // return res.Where(x => x != null && x.OrderInformation != null && x.OrderItem != null).ToList();
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return null;
            }
        }

        public BaseResultModel CommonCalCulateOrderHeader(ref SaleOrderModel model, string token)
        {
            try
            {
                var items = model.OrderItems.Where(x =>
                    !x.IsDeleted &&
                    !(x.IsKit && x.ItemCode == null)
                ).ToList();
                model.Orig_Ord_SKUs = 0;
                model.Ord_SKUs = 0;
                model.Shipped_SKUs = 0;
                model.Orig_Ord_Qty = 0;
                model.Ord_Qty = 0;
                model.Shipped_Qty = 0;
                model.Orig_Promotion_Qty = 0;
                model.Promotion_Qty = 0;
                model.Shipped_Promotion_Qty = 0;
                model.Orig_Ord_Amt = 0;
                model.Ord_Amt = 0;
                model.Shipped_Amt = 0;
                model.Orig_Ord_Disc_Amt = 0;
                model.Ord_Disc_Amt = 0;
                model.Shipped_Disc_Amt = 0;
                model.Orig_Ordline_Disc_Amt = 0;
                model.Ordline_Disc_Amt = 0;
                model.Shipped_line_Disc_Amt = 0;
                model.Orig_Ord_Extend_Amt = 0;
                model.Ord_Extend_Amt = 0;
                model.Shipped_Extend_Amt = 0;
                model.TotalVAT = 0;
                model.Promotion_Amt = 0;
                foreach (var item in items)
                {
                    model.Orig_Ord_SKUs += item.OriginalOrderQuantities > 0 ? 1 : 0; //Số SP ban đầu trên đơn hàng
                    model.Ord_SKUs += item.OrderQuantities > 0 ? 1 : 0; //Số SP trên đơn hàng được xác nhận
                    model.Shipped_SKUs += item.ShippedQuantities > 0 ? 1 : 0; //Số sản phẩm giao thành công
                    model.Orig_Ord_Qty += item.OriginalOrderQuantities; //Tổng sản lượng đặt ban đầu trên đơn hàng
                    model.Ord_Qty += item.OrderBaseQuantities; //Tổng sản lượng xác nhận đặt trên đơn hàng
                    model.Shipped_Qty += item.ShippedBaseQuantities; //Tổng sản lượng giao thành công trên đơn hàng
                    if (item.DiscountID != null)
                    {
                        model.Orig_Promotion_Qty += item.OriginalOrderQuantities; //Tổng sản lượng KM ban đầu trên đơn hàng
                        model.Promotion_Qty += item.OrderQuantities; //Tổng sản lượng KM được xác nhận trên đơn hàng
                        model.Shipped_Promotion_Qty += item.ShippedQuantities; //Tổng sản lượng KM được giao thành công trên dơn hàng
                    }
                    model.Orig_Ord_Amt += item.Orig_Ord_Line_Amt;//Tổng doanh số đặt ban đầu trên đơn hàng
                    model.Ord_Amt += item.Ord_Line_Amt;//Tổng doanh số được xác nhận trên đơn hàng
                    model.Shipped_Amt += item.Shipped_Line_Amt; //Tổng doanh số được giao thành công trên đơn hàng

                    // model.Orig_Ord_Disc_Amt += item.Orig_Ord_line_Disc_Amt;  //Tổng tiền CK ban đầu trên ĐH
                    // model.Ord_Disc_Amt += item.Ord_line_Disc_Amt;  //Tổng tiền CK trên ĐH được xác nhận
                    // model.Shipped_Disc_Amt += item.Shipped_line_Disc_Amt;  //Tổng tiền CK giao thành công trên ĐH
                    model.Orig_Ordline_Disc_Amt += item.Orig_Ord_line_Disc_Amt;  //Tổng tiền KM ban đầu trên ĐH
                    model.Ordline_Disc_Amt += item.Ord_line_Disc_Amt;  //Tổng tiền KM trên ĐH được xác nhận
                    model.Shipped_line_Disc_Amt += item.Shipped_line_Disc_Amt;  //Tổng tiền KM giao thành công trên ĐH
                    model.Shipped_Extend_Amt += item.Shipped_Line_Extend_Amt;  //Tổng tiền sau CK và KM được giao thành công
                    model.TotalVAT += item.VAT;  //Tổng số thuế
                    item.Ord_Line_Extend_Amt = item.Ord_Line_Amt - item.Ord_line_Disc_Amt;
                    model.Promotion_Amt += item.Ord_line_Disc_Amt;
                    model.Orig_Ord_Extend_Amt += item.Orig_Ord_Line_Extend_Amt;
                    model.Ord_Extend_Amt += item.Ord_Line_Extend_Amt;  //Tổng tiền sau CK và KM được xác nhận

                }
                // var cusInfoResult = _clientService.CommonRequest<ResultModelWithObject<ListCustomerInfoModel>>(CommonData.SystemUrlCode.SystemAdminAPI, $"/CustomerInfomation/Search", Method.POST, token, new EcoparamsWithGenericFilter
                // {
                //     Filters = new List<GenericFilter> {
                //         new GenericFilter {
                //         Property = "CustomerCode",
                //         Values = new List<string> { model.CustomerId.Trim().ToLower() }
                //         }},
                //     IsDropdown = true,
                // });

                var requestCusDisProgram = new
                {
                    saleOrgCode = model.SalesOrgID,
                    sicCode = model.SIC_ID,
                    customerCode = model.CustomerId,
                    shiptoCode = model.CustomerShiptoID,
                    routeZoneCode = model.RouteZoneID,
                    dsaCode = model.DSAID,
                    branch = model.BranchId,
                    region = model.RegionId,
                    subRegion = model.SubRegionId,
                    area = model.AreaId,
                    subArea = model.SubAreaId,
                    distributorCode = model.DistributorCode
                };
                var cusDisprog = _clientService.CommonRequest<ResultModelWithObject<DiscountModel>>(CommonData.SystemUrlCode.ODTpAPI, $"/tpdiscount/getdiscountbycustomer", Method.POST, token, requestCusDisProgram);
                if (cusDisprog.Data != null)
                {
                    if (model.Orig_Ord_Amt > 0)
                    {
                        var discountResult = new
                        {
                            discountCode = cusDisprog.Data.code,
                            discountLevelId = cusDisprog.Data.listDiscountStructureDetails.Select(x => x.id).FirstOrDefault(),
                            purchaseAmount = model.Orig_Ord_Amt - model.Orig_Ordline_Disc_Amt
                        };
                        var discountamt = _clientService.CommonRequest<DiscountResultModel>(CommonData.SystemUrlCode.ODTpAPI, $"/tpdiscount/discountresult", Method.POST, token, discountResult);
                        model.Orig_Ord_Disc_Amt = discountamt.discountAmount;
                    }

                    if (model.Ord_Amt > 0)
                    {
                        var discountResult = new
                        {
                            discountCode = cusDisprog.Data.code,
                            discountLevelId = cusDisprog.Data.listDiscountStructureDetails.Select(x => x.id).FirstOrDefault(),
                            purchaseAmount = model.Ord_Amt - model.Ordline_Disc_Amt
                        };
                        var discountamt = _clientService.CommonRequest<DiscountResultModel>(CommonData.SystemUrlCode.ODTpAPI, $"/tpdiscount/discountresult", Method.POST, token, discountResult);
                        model.Ord_Disc_Amt = discountamt.discountAmount;
                    }
                    model.DiscountID = cusDisprog.Data.code;
                }
                model.Orig_Ord_Extend_Amt = model.Orig_Ord_Extend_Amt - model.Orig_Ord_Disc_Amt;  //Tổng tiền sau CK và KM ban đầu
                model.Ord_Extend_Amt = model.Ord_Extend_Amt - model.Ord_Disc_Amt;  //Tổng tiền sau CK và KM ban đầu
                model.TotalLine = model.OrderItems.Where(x =>
                    !x.IsDeleted &&
                    !(x.IsKit && x.ItemCode != null) &&
                    !(!string.IsNullOrWhiteSpace(x.PromotionCode) && x.ItemCode == null)
                ).GroupBy(x => x.InventoryID).Select(x => x.First()).Count();

                return new BaseResultModel
                {
                    Code = 200,
                    IsSuccess = true,
                    Message = "OK"
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }

        public async Task<BaseResultModel> CommonConfirm(SaleOrderModel model, string username, string token, bool isImport)
        {
            try
            {
                List<INV_TransactionModel> transactionData = new();
                List<SO_OrderItems> transactionItemList = model.OrderItems.Where(x =>
                    !x.IsDeleted &&
                    !(x.IsKit && x.ItemCode == null) &&
                    !(!string.IsNullOrWhiteSpace(x.PromotionCode) && x.ItemCode == null)
                ).ToList();

                foreach (var item in transactionItemList)
                {
                    transactionData.Add(new INV_TransactionModel
                    {
                        OrderCode = model.OrderRefNumber,
                        ItemId = item.ItemId,
                        ItemCode = item.ItemCode,
                        ItemDescription = item.ItemDescription,
                        Uom = item.UOM,
                        Quantity = item.OrderQuantities, // số lượng cần đặt
                        BaseQuantity = item.OrderBaseQuantities, //base cua thằng tr
                        OrderBaseQuantity = item.OrderBaseQuantities,
                        TransactionDate = DateTime.Now,
                        TransactionType = INV_TransactionType.SO_CONFIRM,
                        WareHouseCode = model.WareHouseID,
                        LocationCode = item.LocationID,
                        DistributorCode = model.DistributorCode,
                        DSACode = model.DSAID,
                        Description = model.Note
                    });

                }

                //call api transaction
                _client = new RestClient(CommonData.SystemUrl.Where(x => x.Code == CommonData.SystemUrlCode.ODInventoryAPI).Select(x => x.Url).FirstOrDefault());
                _client.Authenticator = new JwtAuthenticator($"{token}");
                var json = JsonConvert.SerializeObject(transactionData);
                var request = new RestRequest();
                if (isImport)
                {
                    request = new RestRequest($"InventoryTransaction/BulkCreate/ImportOrder", Method.POST);
                }
                else
                {
                    request = new RestRequest($"InventoryTransaction/BulkCreate", Method.POST);
                }
                request.AddJsonBody(json);
                // Add Header
                request.AddHeader(OD_Constant.KeyHeader, _distributorCode);
                var result = _client.Execute(request);

                var resultData = JsonConvert.DeserializeObject<BaseResultModel>(JsonConvert.DeserializeObject(result.Content).ToString());
                if (!resultData.IsSuccess)
                {
                    resultData.Message = "Inventory transaction: " + resultData.Message;
                    resultData.Data = null;
                    return resultData;
                }

                return new BaseResultModel
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "OK"
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }

        public async Task<BaseResultModel> CommonValidate(SaleOrderModel model, string token)
        {
            try
            {
                List<AvailableAllocationItemQuery> availableAllocationItemQuery = new();
                List<SO_OrderItems> checkAvailableData = model.OrderItems.Where(x =>
                x.ItemCode != null && !x.IsDeleted).ToList();
                foreach (var item in checkAvailableData)
                {
                    availableAllocationItemQuery.Add(new AvailableAllocationItemQuery
                    {
                        DistributorCode = model.DistributorCode,
                        WareHouseCode = model.WareHouseID,
                        LocationCode = item.LocationID,
                        ItemCode = item.ItemCode,
                        BaseQuantities = item.OrderBaseQuantities
                    });
                }

                //call api transaction
                _client = new RestClient(CommonData.SystemUrl.Where(x => x.Code == CommonData.SystemUrlCode.ODInventoryAPI).Select(x => x.Url).FirstOrDefault());
                _client.Authenticator = new JwtAuthenticator($"{token}");
                var request = new RestRequest($"AllocationItem/CheckAvailableAllocationItems", Method.POST, DataFormat.Json);
                request.AddJsonBody(availableAllocationItemQuery);

                // Add Header
                request.AddHeader(OD_Constant.KeyHeader, _distributorCode);

                var result = _client.Execute(request);
                var resultData = JsonConvert.DeserializeObject<BaseResultModel>(JsonConvert.DeserializeObject(result.Content).ToString());
                if (!resultData.IsSuccess)
                {
                    resultData.Message = "Inventory transaction: " + resultData.Message;
                    // resultData.Data = null;
                    return resultData;
                }


                //Verify Overbooked
                if (model.PromotionRefNumber != null)
                {
                    var promoCusDetails = await _customerProgramDetailRepo.GetAllQueryable(x =>
                        !string.IsNullOrEmpty(x.BudgetCode) &&
                        !string.IsNullOrEmpty(x.PromotionRefNumber) && x.PromotionRefNumber == model.PromotionRefNumber, null, null, _schemaName).ToListAsync();
                    if (promoCusDetails != null && promoCusDetails.Count > 0)
                    {
                        foreach (var cusDetail in promoCusDetails)
                        {
                            if (cusDetail.BudgetBookOver && cusDetail.BudgetBook > cusDetail.BudgetBooked)
                            {
                                var promo = await _customerProgramRepo
                                    .GetAllQueryable(x => x.ProgramCustomersKey == cusDetail.ProgramCustomersKey, null, null, _schemaName)
                                    .FirstOrDefaultAsync();

                                var principal = await _principalRepo.GetAllQueryable().FirstOrDefaultAsync();
                                float budgetNeedBook = cusDetail.BudgetBook - cusDetail.BudgetBooked;
                                var budgetDataReq = new BudgetReqModel
                                {
                                    budgetCode = cusDetail.BudgetCode,
                                    budgetType = cusDetail.BudgetType,
                                    customerCode = promo.CustomerCode,
                                    customerShipTo = promo.ShiptoCode,
                                    saleOrg = promo.SalesOrgCode,
                                    budgetAllocationLevel = cusDetail.BudgetAllocationLevel,
                                    budgetBook = budgetNeedBook,
                                    salesTerritoryValueCode = null,
                                    promotionCode = promo.ProgramCode,
                                    promotionLevel = cusDetail.DetailLevel,
                                    routeZoneCode = promo.RouteZoneCode,
                                    dsaCode = promo.DsaCode,
                                    subAreaCode = promo.SubArea,
                                    areaCode = promo.Area,
                                    subRegionCode = promo.SubRegion,
                                    regionCode = promo.Region,
                                    branchCode = promo.Branch,
                                    nationwideCode = principal.Country,
                                    salesOrgCode = promo.SalesOrgCode,
                                    referalCode = null, //?
                                    distributorCode = _distributorCode
                                };
                                switch (cusDetail.BudgetAllocationLevel)
                                {
                                    case "DSA":
                                        {
                                            budgetDataReq.salesTerritoryValueCode = promo.DsaCode;
                                            break;
                                        }
                                    case "TL01":
                                        {
                                            budgetDataReq.salesTerritoryValueCode = promo.Branch;
                                            break;
                                        }
                                    case "TL02":
                                        {
                                            budgetDataReq.salesTerritoryValueCode = promo.Region;
                                            break;
                                        }
                                    case "TL03":
                                        {
                                            budgetDataReq.salesTerritoryValueCode = promo.SubRegion;
                                            break;
                                        }
                                    case "TL04":
                                        {
                                            budgetDataReq.salesTerritoryValueCode = promo.Area;
                                            break;
                                        }
                                    case "TL05":
                                        {
                                            budgetDataReq.salesTerritoryValueCode = promo.SubArea;
                                            break;
                                        }
                                    default:
                                        break;
                                }

                                // check budget với số book cần (book - booked)
                                var budgetChecked = (await _clientService.CommonRequestAsync<ResultModelWithObject<BudgetResModel>>(CommonData.SystemUrlCode.ODTpAPI, $"external_checkbudget/checkbudget", Method.POST, token, budgetDataReq)).Data;

                                // if (budgetChecked != null)
                                // {
                                //     item.BudgetBook = budgetChecked.budgetBook;
                                //     item.BudgetBooked = budgetChecked.budgetBooked;
                                //     item.BudgetBookOver = budgetChecked.budgetBookOver;
                                // }

                                if (budgetChecked != null && budgetChecked.budgetBook == budgetChecked.budgetBooked)
                                {
                                    // Nếu đủ thì cập nhật số booked mới, cập nhật detail và continue
                                    cusDetail.BudgetBooked += budgetChecked.budgetBooked;
                                    _customerProgramDetailRepo.Update(cusDetail, _schemaName);
                                }
                                else if (budgetChecked != null && budgetChecked.budgetBook > budgetChecked.budgetBooked)
                                {
                                    // Nếu k đủ thì trả lại số book và trả lỗi
                                    budgetDataReq.budgetBook = -budgetChecked.budgetBooked;
                                    var budgetReturned = (await _clientService.CommonRequestAsync<ResultModelWithObject<BudgetResModel>>(CommonData.SystemUrlCode.ODTpAPI, $"external_checkbudget/checkbudget", Method.POST, token, budgetDataReq)).Data;
                                    return new BaseResultModel
                                    {
                                        Code = 400,
                                        Message = "The current budget cannot be confirmed because the remaining quantity of the order has not been met",
                                        IsSuccess = false,
                                    };
                                }
                                else
                                {
                                    return new BaseResultModel
                                    {
                                        Code = 400,
                                        Message = "The current budget cannot be confirmed because the remaining quantity of the order has not been met",
                                        IsSuccess = false,
                                    };
                                }

                            }
                            else
                            {
                                continue;
                            }
                        }
                    }
                }

                return new BaseResultModel
                {
                    Code = 200,
                    IsSuccess = true,
                    Message = "OK"
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace + " " + ex.StackTrace);
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }

        public async Task<BaseResultModel> GetPeriodID(DateTime? OrderDate, string token)
        {
            try
            {
                var year = (int)OrderDate.Value.Year;
                // Get sales calendar
                var salesCalendar = _salesCalendarRepo.FirstOrDefault(x => x.SaleYear == year);
                if (salesCalendar == null)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Code = 404,
                        Message = "Cannot found sales calendar"
                    };
                }

                var salesPeriod = _saleCalendarGenerateRepo.FirstOrDefault(x => 
                        x.SaleCalendarId == salesCalendar.Id &&
                        x.StartDate <= OrderDate.Value &&
                        OrderDate.Value <= x.EndDate
                        && x.Type == "MONTH"
                    );

                return new BaseResultModel
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Success",
                    Data = salesPeriod.Code
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }

        public async Task<ResultModelWithObject<SaleOrderModel>> CommonHandleInternalSoAttribute(SaleOrderModel model, string token)
        {
            token = token.Split(" ").Last();
            try
            {
                List<SO_OrderItems> selectedItems = model.OrderItems.Where(x =>
                !(!string.IsNullOrWhiteSpace(x.PromotionCode) && x.ItemCode == null) &&
                !x.IsDeleted).ToList();

                #region Distributor Info
                // MDM SaleOrg
                var MDMs = _clientService.CommonRequest<ResultModelWithObject<List<MdmModel>>>(CommonData.SystemUrlCode.SalesOrgAPI, $"DistributorSellingArea/GetListEmployeeManager/{model.DSAID}", RestSharp.Method.GET, token, null);
                if (MDMs.IsSuccess && MDMs.Data.Count > 0)
                {
                    foreach (var item in MDMs.Data.ToList())
                    {
                        if (item.IsDsa)
                        {
                            model.DSA_Manager_ID = item.EmployeeCode;
                        }
                        else
                        {
                            switch (item.TerritoryLevel)
                            {
                                case TerritorySettingConst.Branch:
                                    {
                                        model.Branch_Manager_ID = item.EmployeeCode;
                                        model.BranchId = item.TerritoryValue.Split("-").Last().Trim();
                                        break;
                                    }
                                case TerritorySettingConst.Region:
                                    {
                                        model.Region_Manager_ID = item.EmployeeCode;
                                        model.RegionId = item.TerritoryValue.Split("-").Last().Trim();
                                        break;
                                    }
                                case TerritorySettingConst.SubRegion:
                                    {
                                        model.Sub_Region_Manager_ID = item.EmployeeCode;
                                        model.SubRegionId = item.TerritoryValue.Split("-").Last().Trim();
                                        break;
                                    }
                                case TerritorySettingConst.Area:
                                    {
                                        model.Area_Manager_ID = item.EmployeeCode;
                                        model.AreaId = item.TerritoryValue.Split("-").Last().Trim();
                                        break;
                                    }
                                case TerritorySettingConst.SubArea:
                                    {
                                        model.Sub_Area_Manager_ID = item.EmployeeCode;
                                        model.SubAreaId = item.TerritoryValue.Split("-").Last().Trim();
                                        break;
                                    }
                                case null:
                                    {
                                        if (item.Source == "Country")
                                        {
                                            model.NSD_ID = item.EmployeeCode;
                                        }
                                        break;
                                    }
                                default: break;
                            }
                        }

                    }
                }
                #endregion

                #region SalePeriod
                //var year = (int)model.OrderDate.Year;
                //var resultSalesCalendar = await GetAllSaleYearReleased(token);
                //if (!resultSalesCalendar.IsSuccess)
                //{
                //    return new ResultModelWithObject<SaleOrderModel>
                //    {
                //        IsSuccess = false,
                //        Code = resultSalesCalendar.Code,
                //        Message = resultSalesCalendar.Message
                //    };
                //}

                //var salesCalendar = resultSalesCalendar.Data.Data.FirstOrDefault(x => x.SaleYear == year);
                //if (salesCalendar == null)
                //{
                //    return new ResultModelWithObject<SaleOrderModel>
                //    {
                //        IsSuccess = false,
                //        Code = 400,
                //        Message = "Cannot found sales calendar"
                //    };
                //}

                //// Get sales period
                //var salesPeriodResult = await GetGenerateByCalenderId(salesCalendar.Id, token);
                //if (!salesPeriodResult.IsSuccess)
                //{
                //    return new ResultModelWithObject<SaleOrderModel>
                //    {
                //        IsSuccess = false,
                //        Code = salesPeriodResult.Code,
                //        Message = salesPeriodResult.Message
                //    };
                //}

                //var listSalesPeriod = salesPeriodResult.Data;
                //var salesPeriod = listSalesPeriod
                //    .FirstOrDefault(x => x.StartDate <= model.OrderDate &&
                //    model.OrderDate <= x.EndDate
                //    && x.Type == "MONTH");

                BaseResultModel resGetPeriod = await GetPeriodID(model.OrderDate, token);

                if (!resGetPeriod.IsSuccess)
                {
                    return new ResultModelWithObject<SaleOrderModel>
                    {
                        IsSuccess = false,
                        Code = resGetPeriod.Code,
                        Message = resGetPeriod.Message
                    };
                }

                model.PeriodID = resGetPeriod?.Data?.ToString();
                #endregion

                return new ResultModelWithObject<SaleOrderModel>
                {
                    Code = 200,
                    IsSuccess = true,
                    Message = "OK",
                    Data = model
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new ResultModelWithObject<SaleOrderModel>
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }

        public async Task<ResultModelWithObject<List<SalesPeriodResultModel>>> GetGenerateByCalenderId(Guid id, string token)
        {
            try
            {
                // Handle Token
                string tokenSplit = token.Split(" ").Last();

                _client.Authenticator = new JwtAuthenticator($"Rdos {tokenSplit}");
                var requestData = new RestRequest($"SaleCalendar/GetGenerateByCalenderId/{id}", Method.GET, DataFormat.Json);
                var responeData = _client.Execute(requestData);

                if (responeData == null || responeData.Content == String.Empty)
                {
                    return new ResultModelWithObject<List<SalesPeriodResultModel>>
                    {
                        IsSuccess = false,
                        Code = 400,
                        Message = $"Cannot GetGenerateByCalenderId"
                    };
                }

                var resultData = JsonConvert.DeserializeObject<List<SalesPeriodResultModel>>(JsonConvert.DeserializeObject(responeData.Content).ToString());

                return new ResultModelWithObject<List<SalesPeriodResultModel>>
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Success",
                    Data = resultData
                };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message);
                return new ResultModelWithObject<List<SalesPeriodResultModel>>
                {
                    Code = 500,
                    IsSuccess = false,
                    Message = ex.InnerException?.Message ?? ex.Message
                };
            }
        }

        public async Task<ResultModelWithObject<ListSalesCalendar>> GetAllSaleYearReleased(string token)
        {
            try
            {
                // Handle Token
                string tokenSplit = token.Split(" ").Last();
                _client = new RestClient(CommonData.SystemUrl.Where(x => x.Code == CommonData.SystemUrlCode.SystemAdminAPI).Select(x => x.Url).FirstOrDefault());
                _client.Authenticator = new JwtAuthenticator($"Rdos {tokenSplit}");
                var requestData = new RestRequest($"SaleCalendar/GetAllSaleYearReleased", Method.POST, DataFormat.Json);
                var responeData = _client.Execute(requestData);

                if (responeData == null || responeData.Content == String.Empty)
                {
                    return new ResultModelWithObject<ListSalesCalendar>
                    {
                        IsSuccess = false,
                        Code = 400,
                        Message = $"Cannot GetAllSaleYearReleased"
                    };
                }

                var resultData = JsonConvert.DeserializeObject<ListSalesCalendar>(JsonConvert.DeserializeObject(responeData.Content).ToString());

                return new ResultModelWithObject<ListSalesCalendar>
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Success",
                    Data = resultData
                };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message);
                return new ResultModelWithObject<ListSalesCalendar>
                {
                    Code = 500,
                    IsSuccess = false,
                    Message = ex.InnerException?.Message ?? ex.Message
                };
            }
        }

        public async Task<BaseResultModel> CommonInventransactionService(List<INV_TransactionModel> transactionData, string token)
        {
            try
            {
                //call api transaction
                _client = new RestClient(CommonData.SystemUrl.Where(x => x.Code == CommonData.SystemUrlCode.ODInventoryAPI).Select(x => x.Url).FirstOrDefault());
                _client.Authenticator = new JwtAuthenticator($"{token}");
                var json = JsonConvert.SerializeObject(transactionData);
                var request = new RestRequest($"InventoryTransaction/BulkCreate", Method.POST);
                request.AddJsonBody(json);
                // Add Header
                request.AddHeader(OD_Constant.KeyHeader, _distributorCode);
                var result = _client.Execute(request);

                var resultData = JsonConvert.DeserializeObject<BaseResultModel>(JsonConvert.DeserializeObject(result.Content).ToString());
                if (!resultData.IsSuccess)
                {
                    resultData.Message = "Inventory transaction: " + resultData.Message;
                    resultData.Data = null;
                    return resultData;
                }

                return new BaseResultModel
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Ok"
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }

        private async Task<List<string>> ComomGetValueKeys(List<TerritoryMappingByValueModel> listChildren)
        {
            List<string> listterritoryValueKey = new();
            foreach (var item in listChildren)
            {
                listterritoryValueKey.Add(item.TerritoryValueKey);
                foreach (var child in item.ListChildren)
                {
                    listterritoryValueKey.Add(child.TerritoryValueKey);
                    if (child.ListChildren != null && child.ListChildren.Count > 0)
                    {
                        List<string> valueKeys = await ComomGetValueKeys(child.ListChildren);
                        listterritoryValueKey.AddRange(valueKeys);
                    }
                }
            }
            return listterritoryValueKey;
        }

        public async Task<BaseResultModel> HandleInventoryTransaction(SaleOrderModel model, string transactionType, string token)
        {
            try
            {
                List<INV_TransactionModel> transactionData = new();
                List<SO_OrderItems> transactionItemList = model.OrderItems.Where(x =>
                x.ItemCode != null &&
                !x.IsDeleted && !string.IsNullOrWhiteSpace(x.InventoryID)).ToList();

                foreach (var item in transactionItemList)
                {
                    switch (transactionType)
                    {
                        case INV_TransactionType.SO_BOOKED_CANCEL:
                            {
                                transactionData.Add(new INV_TransactionModel
                                {
                                    OrderCode = model.OrderRefNumber,
                                    ItemId = item.ItemId,
                                    ItemCode = item.ItemCode,
                                    ItemDescription = item.ItemDescription,
                                    Uom = item.UOM,
                                    Quantity = item.OrderQuantities, // số lượng cần đặt
                                    BaseQuantity = item.OrderBaseQuantities, //base cua thằng tr
                                    OrderBaseQuantity = item.OrderBaseQuantities,
                                    TransactionDate = DateTime.Now,
                                    TransactionType = transactionType,
                                    WareHouseCode = model.WareHouseID,
                                    LocationCode = item.LocationID,
                                    DistributorCode = model.DistributorCode,
                                    DSACode = model.DSAID,
                                    Description = model.Note,
                                    ReasonCode = model.ReasonCode,
                                    ReasonDescription = model.ReasonCode == null ? (await _reasonRepository.GetAllQueryable().FirstOrDefaultAsync(x => !string.IsNullOrWhiteSpace(x.ReasonCode) && x.ReasonCode == model.ReasonCode))?.Description ?? null : null
                                });
                                break;
                            }
                        case INV_TransactionType.SO_CL:
                            {

                                break;
                            }
                        case INV_TransactionType.SO_CONFIRM:
                            {
                                break;
                            }
                        case INV_TransactionType.SO_PICKING:
                            {
                                break;
                            }
                        case INV_TransactionType.SO_RE:
                            {
                                break;
                            }
                        case INV_TransactionType.SO_SHIPPED:
                            {
                                transactionData.Add(new INV_TransactionModel
                                {
                                    OrderCode = model.OrderRefNumber,
                                    ItemId = item.ItemId,
                                    ItemCode = item.ItemCode,
                                    ItemDescription = item.ItemDescription,
                                    Uom = item.UOM,
                                    Quantity = item.ShippedQuantities, // số lượng cần đặt
                                    BaseQuantity = item.ShippedBaseQuantities, //base cua thằng tr
                                    OrderBaseQuantity = item.ShippedBaseQuantities,
                                    TransactionDate = DateTime.Now,
                                    TransactionType = transactionType,
                                    WareHouseCode = model.WareHouseID,
                                    LocationCode = item.LocationID,
                                    DistributorCode = model.DistributorCode,
                                    DSACode = model.DSAID,
                                    Description = model.Note
                                });
                                break;
                            }
                        case INV_TransactionType.SO_SHIPPED_DIRECT:
                            {
                                break;
                            }
                        case INV_TransactionType.SO_SHIPPED_NOPICKING:
                            {
                                break;
                            }
                        default: break;
                    }
                }

                //call api transaction
                _client = new RestClient(CommonData.SystemUrl.Where(x => x.Code == CommonData.SystemUrlCode.ODInventoryAPI).Select(x => x.Url).FirstOrDefault());
                _client.Authenticator = new JwtAuthenticator($"{token}");
                var json = JsonConvert.SerializeObject(transactionData);
                var request = new RestRequest($"InventoryTransaction/BulkCreate", Method.POST);
                request.AddJsonBody(json);
                // Add Header
                request.AddHeader(OD_Constant.KeyHeader, _distributorCode);
                var result = _client.Execute(request);

                var resultData = JsonConvert.DeserializeObject<BaseResultModel>(JsonConvert.DeserializeObject(result.Content).ToString());
                if (!resultData.IsSuccess)
                {
                    resultData.Message = "Inventory transaction: " + resultData.Message;
                    resultData.Data = null;
                    return resultData;
                }

                return new BaseResultModel
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Ok"
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }

        public async Task<List<INV_TransactionModel>> HandleTransactionPendingOrderData(SaleOrderModel model, string transactionType, string token)
        {
            try
            {
                List<INV_TransactionModel> transactionData = new();
                List<SO_OrderItems> transactionItemList = model.OrderItems.Where(x =>
                x.ItemCode != null &&
                !x.IsDeleted && !string.IsNullOrWhiteSpace(x.InventoryID)).ToList();

                foreach (var item in transactionItemList)
                {
                    switch (transactionType)
                    {
                        case INV_TransactionType.SO_BOOKED_CANCEL:
                            {
                                transactionData.Add(new INV_TransactionModel
                                {
                                    OrderCode = model.OrderRefNumber,
                                    ItemId = item.ItemId,
                                    ItemCode = item.ItemCode,
                                    ItemDescription = item.ItemDescription,
                                    Uom = item.UOM,
                                    Quantity = item.OrderQuantities, // số lượng cần đặt
                                    BaseQuantity = item.OrderBaseQuantities, //base cua thằng tr
                                    OrderBaseQuantity = item.OrderBaseQuantities,
                                    TransactionDate = DateTime.Now,
                                    TransactionType = transactionType,
                                    WareHouseCode = model.WareHouseID,
                                    LocationCode = item.LocationID,
                                    DistributorCode = model.DistributorCode,
                                    DSACode = model.DSAID,
                                    ReasonCode = model.ReasonCode,
                                    Description = model.Note,
                                    ReasonDescription = model.ReasonCode == null ? (await _reasonRepository.GetAllQueryable().FirstOrDefaultAsync(x => !string.IsNullOrWhiteSpace(x.ReasonCode) && x.ReasonCode == model.ReasonCode))?.Description ?? null : null
                                });
                                break;
                            }
                        case INV_TransactionType.SO_CL:
                            {
                                transactionData.Add(new INV_TransactionModel
                                {
                                    OrderCode = model.OrderRefNumber,
                                    ItemId = item.ItemId,
                                    ItemCode = item.ItemCode,
                                    ItemDescription = item.ItemDescription,
                                    Uom = item.UOM,
                                    Quantity = item.ShippedQuantities,
                                    OrderBaseQuantity = item.ShippedBaseQuantities,
                                    BaseQuantity = item.ShippedBaseQuantities,
                                    TransactionDate = DateTime.Now,
                                    TransactionType = INV_TransactionType.SO_CL,
                                    WareHouseCode = model.WareHouseID,
                                    LocationCode = item.LocationID,
                                    DistributorCode = model.DistributorCode,
                                    DSACode = model.DSAID,
                                    Description = model.Note,
                                    ReasonCode = model.ReasonCode,
                                    ReasonDescription = model.ReasonCode != null ? (await _reasonRepository.GetAllQueryable().FirstOrDefaultAsync(x => !string.IsNullOrWhiteSpace(x.ReasonCode) && x.ReasonCode == model.ReasonCode))?.Description ?? null : null
                                });
                                break;
                            }
                        case INV_TransactionType.SO_SHIPPED:
                            {
                                transactionData.Add(new INV_TransactionModel
                                {
                                    OrderCode = model.OrderRefNumber,
                                    ItemId = item.ItemId,
                                    ItemCode = item.ItemCode,
                                    ItemDescription = item.ItemDescription,
                                    Uom = item.UOM,
                                    Quantity = item.ShippedQuantities, // số lượng cần đặt
                                    BaseQuantity = item.ShippedBaseQuantities, //base cua thằng tr
                                    OrderBaseQuantity = item.ShippedBaseQuantities,
                                    TransactionDate = DateTime.Now,
                                    TransactionType = transactionType,
                                    WareHouseCode = model.WareHouseID,
                                    LocationCode = item.LocationID,
                                    DistributorCode = model.DistributorCode,
                                    DSACode = model.DSAID,
                                    Description = model.Note
                                });
                                break;
                            }
                        case INV_TransactionType.SO_SHIPPED_DIRECT:
                            {
                                transactionData.Add(new INV_TransactionModel
                                {
                                    OrderCode = model.OrderRefNumber,
                                    ItemId = item.ItemId,
                                    ItemCode = item.ItemCode,
                                    ItemDescription = item.ItemDescription,
                                    Uom = item.UOM,
                                    Quantity = item.ShippedQuantities, // số lượng cần đặt
                                    BaseQuantity = item.ShippedBaseQuantities, //base cua thằng tr
                                    OrderBaseQuantity = item.OrderBaseQuantities,
                                    TransactionDate = DateTime.Now,
                                    TransactionType = transactionType,
                                    WareHouseCode = model.WareHouseID,
                                    LocationCode = item.LocationID,
                                    DistributorCode = model.DistributorCode,
                                    Description = model.Note,
                                    DSACode = model.DSAID,
                                });
                                break;
                            }
                        case INV_TransactionType.SO_SHIPPED_NOPICKING:
                            {
                                transactionData.Add(new INV_TransactionModel
                                {
                                    OrderCode = model.OrderRefNumber,
                                    ItemId = item.ItemId,
                                    ItemCode = item.ItemCode,
                                    ItemDescription = item.ItemDescription,
                                    Uom = item.UOM,
                                    Quantity = item.ShippedQuantities, // số lượng cần đặt
                                    BaseQuantity = item.ShippedBaseQuantities, //base cua thằng tr
                                    OrderBaseQuantity = item.OrderBaseQuantities,
                                    TransactionDate = DateTime.Now,
                                    TransactionType = transactionType,
                                    WareHouseCode = model.WareHouseID,
                                    LocationCode = item.LocationID,
                                    DistributorCode = model.DistributorCode,
                                    DSACode = model.DSAID,
                                    Description = model.Note
                                });
                                break;
                            }
                        default: break;
                    }
                }

                return transactionData;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new List<INV_TransactionModel>();
            }
        }

        public async Task<ResultModelWithObject<List<TerritoryMappingByValueModel>>> CommonGetListChildNodeByTerritoryValue(string territoryValue, string territoryStructureCode, string saleOrgCode, string token)
        {
            try
            {
                // Handle Token
                string tokenSplit = token.Split(" ").Last();

                var dataReq = new GetListDistributorModel()
                {
                    TerritoryStructureCode = territoryStructureCode,
                    TerritoryValue = territoryValue,
                    SaleOrgCode = saleOrgCode
                };

                _clientSalesConfig.Authenticator = new JwtAuthenticator($"Rdos {tokenSplit}");
                var requestSO = new RestRequest($"TerritoryMapping/GetListChildNodeByTerritoryValue", Method.POST, DataFormat.Json);
                requestSO.AddJsonBody(dataReq);
                var resultSO = _clientSalesConfig.Execute(requestSO);

                if (resultSO == null || resultSO.Content == String.Empty)
                {
                    return new ResultModelWithObject<List<TerritoryMappingByValueModel>>
                    {
                        IsSuccess = false,
                        Code = 400,
                        Message = $"Cannot GetListChildNodeByTerritoryValue"
                    };
                }

                var resultData = JsonConvert.DeserializeObject<ResultModelWithObject<List<TerritoryMappingByValueModel>>>(JsonConvert.DeserializeObject(resultSO.Content).ToString());

                if (!resultData.IsSuccess)
                {
                    return new ResultModelWithObject<List<TerritoryMappingByValueModel>>
                    {
                        IsSuccess = false,
                        Code = resultData.Code,
                        Message = resultData.Message
                    };
                }

                return new ResultModelWithObject<List<TerritoryMappingByValueModel>>
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Success",
                    Data = resultData.Data
                };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message);
                return new ResultModelWithObject<List<TerritoryMappingByValueModel>>
                {
                    Code = 500,
                    IsSuccess = false,
                    Message = ex.InnerException?.Message ?? ex.Message
                };
            }
        }
        #endregion

        #region External API
        public async Task<BaseResultModel> ExUpdateOrderSetting(int deliveryLeadDate, string username)
        {
            try
            {
                var settingInDb = await _settingRepository.GetAllQueryable(null, null, null, _schemaName).FirstOrDefaultAsync();
                if (settingInDb != null)
                {
                    settingInDb.DeliveryLeadDate = deliveryLeadDate;
                    settingInDb.UpdatedDate = DateTime.Now;
                    settingInDb.UpdatedBy = username;
                    _settingRepository.Update(settingInDb, _schemaName);
                }
                else
                {
                    SO_SalesOrderSetting insertSetting = new();
                    insertSetting.Id = Guid.NewGuid();
                    insertSetting.OrderRefNumber = null;
                    insertSetting.LeadDate = 1;
                    insertSetting.DeliveryLeadDate = deliveryLeadDate;
                    insertSetting.CreatedBy = username;
                    insertSetting.CreatedDate = DateTime.Now;
                    insertSetting.OwnerCode = _distributorCode;
                    insertSetting.OwnerType = OwnerTypeConstant.DISTRIBUTOR;
                    _settingRepository.Insert(insertSetting, _schemaName);
                }

                return new BaseResultModel
                {
                    IsSuccess = true,
                    Message = "Success",
                    Code = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message);
                return new BaseResultModel
                {
                    Code = 500,
                    IsSuccess = false,
                    Message = ex.InnerException?.Message ?? ex.Message
                };
            }
        }

        private async Task<ResultModelWithObject<IQueryable<ExQueryDataForPromotion>>> ExQueryCommonDataForPromotion(ExPromotionReportEcoParameters request)
        {
            try
            {
                List<string> listStatus = new List<string>
                {
                    SO_SaleOrderStatusConst.DELIVERED,
                    SO_SaleOrderStatusConst.PARTIALDELIVERED
                };

                var query = (from td in _orderItemsRepository.GetAllQueryable(null, null, null, _schemaName).AsNoTracking()
                             join th in _orderInformationsRepository.GetAllQueryable(x => x.OrderDate >= request.EffectiveDateFrom
                                                                                          && x.OrderDate <= request.ValidUntil
                                                                                          && listStatus.Contains(x.Status),
                                                                                          null, null, _schemaName).AsNoTracking()
                             on td.OrderRefNumber equals th.OrderRefNumber
                             where request.PromotionCode.ToLower().Equals(td.PromotionCode.ToLower())
                             select new ExQueryDataForPromotion { td = td, th = th });

                if (request.ListOrder != null && request.ListOrder.Any())
                {
                    query = query.Where(x => request.ListOrder.Contains(x.th.OrderRefNumber));
                }

                if (!string.IsNullOrEmpty(request.PromotionLevelCode))
                {
                    query = query.Where(x => x.td.PromotionLevel.Equals(request.PromotionLevelCode));
                }

                if (request.ListCustomer != null && request.ListCustomer.Any())
                {
                    query = query.Where(x => request.ListCustomer.Contains(x.th.CustomerId));
                }

                if (request.ListRouteZone != null && request.ListRouteZone.Any())
                {
                    query = query.Where(x => request.ListRouteZone.Contains(x.th.RouteZoneID));
                }

                if (!string.IsNullOrEmpty(request.ScopeType) && request.ListScope != null && request.ListScope.Any())
                {
                    if (request.ScopeType.Equals(PromotionSetting.ScopeDSA))
                    {
                        query = query.Where(x => request.ListScope.Contains(x.th.DSAID));
                    }
                    else
                    {
                        query = query.Where(x => x.th.SalesOrgID.Equals(request.SaleOrg) &&
                        (request.ListScope.Contains(x.th.BranchId) || request.ListScope.Contains(x.th.RegionId) ||
                        request.ListScope.Contains(x.th.SubRegionId) || request.ListScope.Contains(x.th.AreaId) ||
                        request.ListScope.Contains(x.th.SubAreaId)));
                    }
                }

                if (!string.IsNullOrEmpty(request.ApplicableObjectType) && request.ListApplicableObject != null && request.ListApplicableObject.Any())
                {
                    if (request.ApplicableObjectType.Equals(PromotionSetting.ObjectCustomerAttributes))
                    {
                        query = query.Where(x => request.ListApplicableObject.Contains(x.th.Shipto_Attribute10) || request.ListApplicableObject.Contains(x.th.Shipto_Attribute1) ||
                        request.ListApplicableObject.Contains(x.th.Shipto_Attribute2) || request.ListApplicableObject.Contains(x.th.Shipto_Attribute3) ||
                        request.ListApplicableObject.Contains(x.th.Shipto_Attribute4) || request.ListApplicableObject.Contains(x.th.Shipto_Attribute5) ||
                        request.ListApplicableObject.Contains(x.th.Shipto_Attribute6) || request.ListApplicableObject.Contains(x.th.Shipto_Attribute7) ||
                        request.ListApplicableObject.Contains(x.th.Shipto_Attribute8) || request.ListApplicableObject.Contains(x.th.Shipto_Attribute9));
                    }
                }
                return new ResultModelWithObject<IQueryable<ExQueryDataForPromotion>>
                {
                    IsSuccess = true,
                    Message = "Successfully",
                    Data = query
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message);
                return new ResultModelWithObject<IQueryable<ExQueryDataForPromotion>>
                {
                    Code = 500,
                    IsSuccess = false,
                    Message = ex.InnerException?.Message ?? ex.Message
                };
            }

        }

        public async Task<ResultModelWithObject<ListPromotionDetailReportOrderListModel>> ExTpGetDataForReportDetail(ExPromotionReportEcoParameters request)
        {
            try
            {
                ResultModelWithObject<IQueryable<ExQueryDataForPromotion>> resQuery = await ExQueryCommonDataForPromotion(request);
                if (!resQuery.IsSuccess)
                {
                    return new ResultModelWithObject<ListPromotionDetailReportOrderListModel>
                    {
                        IsSuccess = false,
                        Message = resQuery.Message,
                        Code = resQuery.Code
                    };
                }

                IQueryable<ExQueryDataForPromotion> query = resQuery.Data;
                var featureListTemp = query.Select(x => new PromotionDetailReportOrderListModel()
                {
                    CustomerID = x.th.CustomerId,
                    OrdDate = x.th.OrderDate,
                    ShiptoID = x.th.CustomerShiptoID,
                    ShiptoName = x.th.CustomerShiptoName,
                    PromotionLevel = x.td.PromotionLevel,
                    Shipped_Qty = x.td.ShippedQuantities,
                    PackSize = x.td.UOM,
                    ShippedLineDiscAmt = x.td.Shipped_line_Disc_Amt,
                    ReferenceLink = $"{x.th.SalesOrgID}-{x.th.BranchId}-{x.th.RegionId}-{x.th.AreaId}-{x.th.DSAID}-{x.th.RouteZoneID}-{x.th.CustomerId}",
                    SalesRepCode = x.th.SalesRepID,
                    InventoryID = string.IsNullOrEmpty(x.td.InventoryID) ? string.Empty : x.td.InventoryID,
                    InventoryName = string.IsNullOrEmpty(x.td.ItemDescription) ? string.Empty : x.td.ItemDescription,
                    OrdNbr = x.th.OrderRefNumber
                });

                if (request.Filter != null && request.Filter.Trim() != string.Empty && request.Filter.Trim() != "NA_EMPTY")
                {
                    featureListTemp = featureListTemp.Where(DynamicExpressionParser.ParseLambda(new[] { Expression.Parameter(typeof(PromotionDetailReportOrderListModel), "s") }, typeof(bool), request.Filter));
                }

                // Check Orderby
                if (request.OrderBy != null && request.OrderBy.Trim() != string.Empty && request.OrderBy.Trim() != "NA_EMPTY")
                {
                    featureListTemp = featureListTemp.OrderBy(request.OrderBy);
                }
                else
                {
                    featureListTemp = featureListTemp.OrderBy(x => x.OrdNbr);
                }

                // Check Dropdown
                if (request.IsDropdown)
                {
                    var featureListTempPagged1 = PagedList<PromotionDetailReportOrderListModel>.ToPagedList(featureListTemp.ToList(), 0, query.Count());
                    return new ResultModelWithObject<ListPromotionDetailReportOrderListModel>
                    {
                        IsSuccess = true,
                        Code = 200,
                        Message = "Successfully",
                        Data = new ListPromotionDetailReportOrderListModel { Items = featureListTempPagged1 }
                    };
                }

                int totalCount = featureListTemp.Count();
                int skip = (request.PageNumber - 1) * request.PageSize;
                int top = request.PageSize;
                var items = featureListTemp.Skip(skip).Take(top).ToList();
                var result = new PagedList<PromotionDetailReportOrderListModel>(items, totalCount, (skip / top) + 1, top);
                return new ResultModelWithObject<ListPromotionDetailReportOrderListModel>
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Successfully",
                    Data = new ListPromotionDetailReportOrderListModel { Items = result, MetaData = result.MetaData }
                };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message);
                return new ResultModelWithObject<ListPromotionDetailReportOrderListModel>
                {
                    Code = 500,
                    IsSuccess = false,
                    Message = ex.InnerException?.Message ?? ex.Message
                };
            }
        }

        public async Task<ResultModelWithObject<ListPromotionDetailReportOrderListModel>> ExTpGetOrdersForPopupPromotionReport(ExPromotionReportEcoParameters request)
        {
            try
            {
                ResultModelWithObject<IQueryable<ExQueryDataForPromotion>> resQuery = await ExQueryCommonDataForPromotion(request);
                if (!resQuery.IsSuccess)
                {
                    return new ResultModelWithObject<ListPromotionDetailReportOrderListModel>
                    {
                        IsSuccess = false,
                        Message = resQuery.Message,
                        Code = resQuery.Code
                    };
                }

                IQueryable<ExQueryDataForPromotion> query = resQuery.Data;
                var featureListTemp = query.Select(x => new PromotionDetailReportOrderListModel()
                {
                    OrdNbr = x.th.OrderRefNumber
                }).Distinct();

                if (request.Filter != null && request.Filter.Trim() != string.Empty && request.Filter.Trim() != "NA_EMPTY")
                {
                    featureListTemp = featureListTemp.Where(DynamicExpressionParser.ParseLambda(new[] { Expression.Parameter(typeof(PromotionDetailReportOrderListModel), "s") }, typeof(bool), request.Filter));
                }

                // Check Orderby
                if (request.OrderBy != null && request.OrderBy.Trim() != string.Empty && request.OrderBy.Trim() != "NA_EMPTY")
                {
                    featureListTemp = featureListTemp.OrderBy(request.OrderBy);
                }
                else
                {
                    featureListTemp = featureListTemp.OrderBy(x => x.OrdNbr);
                }

                // Check Dropdown
                if (request.IsDropdown)
                {
                    var featureListTempPagged1 = PagedList<PromotionDetailReportOrderListModel>.ToPagedList(featureListTemp.ToList(), 0, query.Count());
                    return new ResultModelWithObject<ListPromotionDetailReportOrderListModel>
                    {
                        IsSuccess = true,
                        Code = 200,
                        Message = "Successfully",
                        Data = new ListPromotionDetailReportOrderListModel { Items = featureListTempPagged1 }
                    };
                }

                int totalCount = featureListTemp.Count();
                int skip = (request.PageNumber - 1) * request.PageSize;
                int top = request.PageSize;
                var items = featureListTemp.Skip(skip).Take(top).ToList();
                var result = new PagedList<PromotionDetailReportOrderListModel>(items, totalCount, (skip / top) + 1, top);
                return new ResultModelWithObject<ListPromotionDetailReportOrderListModel>
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Successfully",
                    Data = new ListPromotionDetailReportOrderListModel { Items = result, MetaData = result.MetaData }
                };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message);
                return new ResultModelWithObject<ListPromotionDetailReportOrderListModel>
                {
                    Code = 500,
                    IsSuccess = false,
                    Message = ex.InnerException?.Message ?? ex.Message
                };
            }
        }

        public async Task<ResultModelWithObject<ListPromotionDetailReportRouteZoneListModel>> ExTpGetRouteZonesOrderForPromotionReport(ExPromotionReportEcoParameters request)
        {
            try
            {
                ResultModelWithObject<IQueryable<ExQueryDataForPromotion>> resQuery = await ExQueryCommonDataForPromotion(request);
                if (!resQuery.IsSuccess)
                {
                    return new ResultModelWithObject<ListPromotionDetailReportRouteZoneListModel>
                    {
                        IsSuccess = false,
                        Message = resQuery.Message,
                        Code = resQuery.Code
                    };
                }

                IQueryable<ExQueryDataForPromotion> query = resQuery.Data;
                var featureListTemp = query.GroupBy(n =>
                new
                {
                    n.th.RouteZoneID,
                    n.th.RouteZoneName,
                    n.td.PromotionLevel,
                    //n.th.ReferenceLink,
                    n.th.SalesRepID
                }).Select(x => new PromotionDetailReportRouteZoneListModel()
                {
                    RouteZoneId = x.Key.RouteZoneID,
                    RouteZoneDescription = null,
                    PromotionLevel = x.Key.PromotionLevel,
                    PromotionLevelName = null,
                    SalesRepCode = x.Key.SalesRepID,
                    ReferenceLink = $"{x.First().th.SalesOrgID}-{x.First().th.BranchId}-{x.First().th.RegionId}-{x.First().th.AreaId}-{x.First().th.DSAID}-{x.First().th.RouteZoneID}-{x.First().th.CustomerId}",
                });

                if (request.Filter != null && request.Filter.Trim() != string.Empty && request.Filter.Trim() != "NA_EMPTY")
                {
                    featureListTemp = featureListTemp.Where(DynamicExpressionParser.ParseLambda(new[] { Expression.Parameter(typeof(PromotionDetailReportRouteZoneListModel), "s") }, typeof(bool), request.Filter));
                }

                // Check Orderby
                if (request.OrderBy != null && request.OrderBy.Trim() != string.Empty && request.OrderBy.Trim() != "NA_EMPTY")
                {
                    featureListTemp = featureListTemp.OrderBy(request.OrderBy);
                }
                else
                {
                    featureListTemp = featureListTemp.OrderBy(x => x.RouteZoneId).ThenBy(x => x.PromotionLevel);
                }

                // Check Dropdown
                if (request.IsDropdown)
                {
                    var featureListTempPagged1 = PagedList<PromotionDetailReportRouteZoneListModel>.ToPagedList(featureListTemp.ToList(), 0, query.Count());
                    return new ResultModelWithObject<ListPromotionDetailReportRouteZoneListModel>
                    {
                        IsSuccess = true,
                        Code = 200,
                        Message = "Successfully",
                        Data = new ListPromotionDetailReportRouteZoneListModel { Items = featureListTempPagged1 }
                    };
                }

                int totalCount = featureListTemp.Count();
                int skip = (request.PageNumber - 1) * request.PageSize;
                int top = request.PageSize;
                var items = featureListTemp.Skip(skip).Take(top).ToList();
                var result = new PagedList<PromotionDetailReportRouteZoneListModel>(items, totalCount, (skip / top) + 1, top);
                return new ResultModelWithObject<ListPromotionDetailReportRouteZoneListModel>
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Successfully",
                    Data = new ListPromotionDetailReportRouteZoneListModel { Items = result, MetaData = result.MetaData }
                };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message);
                return new ResultModelWithObject<ListPromotionDetailReportRouteZoneListModel>
                {
                    Code = 500,
                    IsSuccess = false,
                    Message = ex.InnerException?.Message ?? ex.Message
                };
            }
        }

        public async Task<ResultModelWithObject<ListPromotionDetailReportRouteZoneListModel>> ExTpGetRouteZonesOrderForPopupPromotionReport(ExPromotionReportEcoParameters request)
        {
            try
            {
                ResultModelWithObject<IQueryable<ExQueryDataForPromotion>> resQuery = await ExQueryCommonDataForPromotion(request);
                if (!resQuery.IsSuccess)
                {
                    return new ResultModelWithObject<ListPromotionDetailReportRouteZoneListModel>
                    {
                        IsSuccess = false,
                        Message = resQuery.Message,
                        Code = resQuery.Code
                    };
                }

                IQueryable<ExQueryDataForPromotion> query = resQuery.Data;
                var featureListTemp = query.Select(x => new PromotionDetailReportRouteZoneListModel()
                {
                    RouteZoneId = x.th.RouteZoneID,
                    RouteZoneDescription = x.th.RouteZoneName
                }).Distinct();

                if (request.Filter != null && request.Filter.Trim() != string.Empty && request.Filter.Trim() != "NA_EMPTY")
                {
                    featureListTemp = featureListTemp.Where(DynamicExpressionParser.ParseLambda(new[] { Expression.Parameter(typeof(PromotionDetailReportRouteZoneListModel), "s") }, typeof(bool), request.Filter));
                }

                // Check Orderby
                if (request.OrderBy != null && request.OrderBy.Trim() != string.Empty && request.OrderBy.Trim() != "NA_EMPTY")
                {
                    featureListTemp = featureListTemp.OrderBy(request.OrderBy);
                }
                else
                {
                    featureListTemp = featureListTemp.OrderBy(x => x.RouteZoneId);
                }

                // Check Dropdown
                if (request.IsDropdown)
                {
                    var featureListTempPagged1 = PagedList<PromotionDetailReportRouteZoneListModel>.ToPagedList(featureListTemp.ToList(), 0, query.Count());
                    return new ResultModelWithObject<ListPromotionDetailReportRouteZoneListModel>
                    {
                        IsSuccess = true,
                        Code = 200,
                        Message = "Successfully",
                        Data = new ListPromotionDetailReportRouteZoneListModel { Items = featureListTempPagged1 }
                    };
                }

                int totalCount = featureListTemp.Count();
                int skip = (request.PageNumber - 1) * request.PageSize;
                int top = request.PageSize;
                var items = featureListTemp.Skip(skip).Take(top).ToList();
                var result = new PagedList<PromotionDetailReportRouteZoneListModel>(items, totalCount, (skip / top) + 1, top);
                return new ResultModelWithObject<ListPromotionDetailReportRouteZoneListModel>
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Successfully",
                    Data = new ListPromotionDetailReportRouteZoneListModel { Items = result, MetaData = result.MetaData }
                };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message);
                return new ResultModelWithObject<ListPromotionDetailReportRouteZoneListModel>
                {
                    Code = 500,
                    IsSuccess = false,
                    Message = ex.InnerException?.Message ?? ex.Message
                };
            }
        }

        public async Task<ResultModelWithObject<ListPromotionDetailReportPointSaleListModel>> ExTpGetCustomersOrderForPromotionReport(ExPromotionReportEcoParameters request)
        {
            try
            {
                ResultModelWithObject<IQueryable<ExQueryDataForPromotion>> resQuery = await ExQueryCommonDataForPromotion(request);
                if (!resQuery.IsSuccess)
                {
                    return new ResultModelWithObject<ListPromotionDetailReportPointSaleListModel>
                    {
                        IsSuccess = false,
                        Message = resQuery.Message,
                        Code = resQuery.Code
                    };
                }

                IQueryable<ExQueryDataForPromotion> query = resQuery.Data;
                var featureListTemp = query.GroupBy(n =>
                new
                {
                    n.th.CustomerId,
                    n.th.CustomerShiptoID,
                    n.th.CustomerShiptoName,
                    n.td.PromotionLevel,
                    //n.td.PromotionLevelName,
                    //n.th.ReferenceLink,
                    n.th.SalesRepID,
                }).Select(x => new PromotionDetailReportPointSaleListModel()
                {
                    CustomerID = x.Key.CustomerId,
                    ShiptoID = x.Key.CustomerShiptoID,
                    ShiptoName = x.Key.CustomerShiptoName,
                    PromotionLevel = x.Key.PromotionLevel,
                    PromotionLevelName = null,
                    ReferenceLink = $"{x.First().th.SalesOrgID}-{x.First().th.BranchId}-{x.First().th.RegionId}-{x.First().th.AreaId}-{x.First().th.DSAID}-{x.First().th.RouteZoneID}-{x.First().th.CustomerId}",
                    SalesRepCode = x.Key.SalesRepID
                });

                if (request.Filter != null && request.Filter.Trim() != string.Empty && request.Filter.Trim() != "NA_EMPTY")
                {
                    featureListTemp = featureListTemp.Where(DynamicExpressionParser.ParseLambda(new[] { Expression.Parameter(typeof(PromotionDetailReportPointSaleListModel), "s") }, typeof(bool), request.Filter));
                }

                // Check Orderby
                if (request.OrderBy != null && request.OrderBy.Trim() != string.Empty && request.OrderBy.Trim() != "NA_EMPTY")
                {
                    featureListTemp = featureListTemp.OrderBy(request.OrderBy);
                }
                else
                {
                    featureListTemp = featureListTemp.OrderBy(x => x.CustomerID);
                }

                // Check Dropdown
                if (request.IsDropdown)
                {
                    var featureListTempPagged1 = PagedList<PromotionDetailReportPointSaleListModel>.ToPagedList(featureListTemp.ToList(), 0, query.Count());
                    return new ResultModelWithObject<ListPromotionDetailReportPointSaleListModel>
                    {
                        IsSuccess = true,
                        Code = 200,
                        Message = "Successfully",
                        Data = new ListPromotionDetailReportPointSaleListModel { Items = featureListTempPagged1 }
                    };
                }

                int totalCount = featureListTemp.Count();
                int skip = (request.PageNumber - 1) * request.PageSize;
                int top = request.PageSize;
                var items = featureListTemp.Skip(skip).Take(top).ToList();
                var result = new PagedList<PromotionDetailReportPointSaleListModel>(items, totalCount, (skip / top) + 1, top);
                return new ResultModelWithObject<ListPromotionDetailReportPointSaleListModel>
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Successfully",
                    Data = new ListPromotionDetailReportPointSaleListModel { Items = result, MetaData = result.MetaData }
                };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message);
                return new ResultModelWithObject<ListPromotionDetailReportPointSaleListModel>
                {
                    Code = 500,
                    IsSuccess = false,
                    Message = ex.InnerException?.Message ?? ex.Message
                };
            }
        }

        public async Task<ResultModelWithObject<ListPromotionDetailReportPointSaleListModel>> ExTpGetCustomersOrderForPopupPromotionReport(ExPromotionReportEcoParameters request)
        {
            try
            {
                ResultModelWithObject<IQueryable<ExQueryDataForPromotion>> resQuery = await ExQueryCommonDataForPromotion(request);
                if (!resQuery.IsSuccess)
                {
                    return new ResultModelWithObject<ListPromotionDetailReportPointSaleListModel>
                    {
                        IsSuccess = false,
                        Message = resQuery.Message,
                        Code = resQuery.Code
                    };
                }

                IQueryable<ExQueryDataForPromotion> query = resQuery.Data;
                var featureListTemp = query.Select(x => new PromotionDetailReportPointSaleListModel()
                {
                    CustomerID = x.th.CustomerId,
                    CustomerName = x.th.CustomerName
                }).Distinct();

                if (request.Filter != null && request.Filter.Trim() != string.Empty && request.Filter.Trim() != "NA_EMPTY")
                {
                    featureListTemp = featureListTemp.Where(DynamicExpressionParser.ParseLambda(new[] { Expression.Parameter(typeof(PromotionDetailReportPointSaleListModel), "s") }, typeof(bool), request.Filter));
                }

                // Check Orderby
                if (request.OrderBy != null && request.OrderBy.Trim() != string.Empty && request.OrderBy.Trim() != "NA_EMPTY")
                {
                    featureListTemp = featureListTemp.OrderBy(request.OrderBy);
                }
                else
                {
                    featureListTemp = featureListTemp.OrderBy(x => x.CustomerID);
                }

                // Check Dropdown
                if (request.IsDropdown)
                {
                    var featureListTempPagged1 = PagedList<PromotionDetailReportPointSaleListModel>.ToPagedList(featureListTemp.ToList(), 0, query.Count());
                    return new ResultModelWithObject<ListPromotionDetailReportPointSaleListModel>
                    {
                        IsSuccess = true,
                        Code = 200,
                        Message = "Successfully",
                        Data = new ListPromotionDetailReportPointSaleListModel { Items = featureListTempPagged1 }
                    };
                }

                int totalCount = featureListTemp.Count();
                int skip = (request.PageNumber - 1) * request.PageSize;
                int top = request.PageSize;
                var items = featureListTemp.Skip(skip).Take(top).ToList();
                var result = new PagedList<PromotionDetailReportPointSaleListModel>(items, totalCount, (skip / top) + 1, top);
                return new ResultModelWithObject<ListPromotionDetailReportPointSaleListModel>
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Successfully",
                    Data = new ListPromotionDetailReportPointSaleListModel { Items = result, MetaData = result.MetaData }
                };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message);
                return new ResultModelWithObject<ListPromotionDetailReportPointSaleListModel>
                {
                    Code = 500,
                    IsSuccess = false,
                    Message = ex.InnerException?.Message ?? ex.Message
                };
            }
        }

        #endregion

        #region CUD
        public async Task<BaseResultModel> InsertOrder(SaleOrderModel model, string token, string username, bool includeConfirm = false)
        {
            try
            {
                //if (IsODSiteConstant) 
                    model.DistributorCode = _distributorCode;

                //Generate RefNumber
                var prefix = StringsHelper.GetPrefixYYM();
                var orderRefNumberIndb = await _orderInformationsRepository.GetAllQueryable(null, null, null, _schemaName)
                    .Where(x => x.OrderRefNumber.Contains(prefix)).AsNoTracking().Select(x => x.OrderRefNumber).OrderByDescending(x => x).FirstOrDefaultAsync();
                var generatedNumber = StringsHelper.GennerateCodeWithYearMonthFormat(prefix, orderRefNumberIndb != null ? orderRefNumberIndb : null);

                bool checkExisted = false;
                do
                {
                    var settingInDb = await _settingRepository.GetAllQueryable(null, null, null, _schemaName).FirstOrDefaultAsync();
                    if (settingInDb != null && settingInDb.OrderRefNumber == generatedNumber)
                    {
                        checkExisted = false;
                        generatedNumber = String.Format("{0}{1:00000}", prefix, generatedNumber != null ? generatedNumber.Substring(3).TryParseInt() + 1 : 0);
                    }
                    else
                    {
                        //checkExisted = true;
                        //settingInDb.OrderRefNumber = generatedNumber;
                        //settingInDb.UpdatedDate = DateTime.Now;
                        //settingInDb.UpdatedBy = username;
                        //_settingRepository.Update(settingInDb);
                        // check order ref number
                        SO_OrderInformations dataInDb = await _orderInformationsRepository.GetAllQueryable(null, null, null, _schemaName)
                            .FirstOrDefaultAsync(x => x.OrderRefNumber == generatedNumber);
                        if (dataInDb == null)
                        {
                            checkExisted = true;
                            if (settingInDb != null)
                            {
                                settingInDb.OrderRefNumber = generatedNumber;
                                settingInDb.UpdatedDate = DateTime.Now;
                                settingInDb.UpdatedBy = username;
                                //if (IsODSiteConstant)
                                //{
                                    settingInDb.OwnerCode = _distributorCode;
                                    settingInDb.OwnerType = OwnerTypeConstant.DISTRIBUTOR;
                                //}
                                _settingRepository.Update(settingInDb, _schemaName);
                            }
                            else
                            {
                                SO_SalesOrderSetting insertSetting = new();
                                insertSetting.Id = Guid.NewGuid();
                                insertSetting.OrderRefNumber = generatedNumber;
                                insertSetting.LeadDate = 0; // Chỗ này sẽ giải quyết sau, set tạm
                                insertSetting.CreatedBy = username;
                                insertSetting.CreatedDate = DateTime.Now;

                                //if (IsODSiteConstant)
                                //{
                                    insertSetting.OwnerCode = _distributorCode;
                                    insertSetting.OwnerType = OwnerTypeConstant.DISTRIBUTOR;
                                //}
                                _settingRepository.Insert(insertSetting, _schemaName);
                            }
                        }
                        else
                        {
                            checkExisted = false;
                            generatedNumber = String.Format("{0}{1:00000}", prefix, generatedNumber != null ? generatedNumber.Substring(3).TryParseInt() + 1 : 0);
                        }
                    }
                } while (!checkExisted);

                model.Id = Guid.NewGuid();
                model.CreatedBy = username;
                model.CreatedDate = DateTime.Now;
                model.UpdatedBy = null;
                model.UpdatedDate = null;
                model.OrderRefNumber = generatedNumber;
                model.OrderType = SO_SaleOrderTypeConst.SalesOrder;
                model.Owner_ID = model.SalesRepID;
                model.Source = SO_SOURCE_CONST.NOTMOBILE;

                var handlerAttResult = await CommonHandleInternalSoAttribute(model, token);
                if (handlerAttResult.IsSuccess)
                {
                    model = handlerAttResult.Data;
                }

                model.Status = includeConfirm ? SO_SaleOrderStatusConst.OPEN : model.Status;


                foreach (var item in model.OrderItems)
                {
                    var itemInsertData = item;
                    item.Id = Guid.NewGuid();
                    item.CreatedBy = username;
                    item.CreatedDate = DateTime.Now;
                    item.UpdatedBy = null;
                    item.UpdatedDate = null;
                    item.OrderRefNumber = model.OrderRefNumber;
                    if (includeConfirm)
                    {
                        item.Orig_Ord_Line_Amt = item.Ord_Line_Amt;
                        item.Orig_Ord_line_Disc_Amt = item.Ord_line_Disc_Amt;
                        item.Orig_Ord_Line_Extend_Amt = item.Ord_Line_Amt - item.Ord_line_Disc_Amt;
                        item.OriginalOrderQuantities = item.OrderQuantities;
                        item.OriginalOrderBaseQuantities = item.OrderBaseQuantities;
                    }
                    item.Ord_Line_Extend_Amt = item.Ord_Line_Amt - item.Ord_line_Disc_Amt;

                    //if (IsODSiteConstant)
                    //{
                        item.OwnerType = OwnerTypeConstant.DISTRIBUTOR;
                        item.OwnerCode = _distributorCode;
                    //}
                    _orderItemsRepository.Add(itemInsertData, _schemaName);
                }

                if (includeConfirm)
                {
                    var validateResult = await CommonValidate(model, token);
                    if (!validateResult.IsSuccess)
                    {
                        return validateResult;
                    }
                    var confirmResult = await CommonConfirm(model, username, token, false);
                    if (!confirmResult.IsSuccess)
                    {
                        return confirmResult;
                    }
                }

                CommonCalCulateOrderHeader(ref model, token);
                //if (IsODSiteConstant)
                //{
                    model.OwnerType = OwnerTypeConstant.DISTRIBUTOR;
                    model.OwnerCode = _distributorCode;
                //}
                _orderInformationsRepository.Add(model, _schemaName);
                _orderInformationsRepository.Save(_schemaName);

                if (model.Status != SO_SaleOrderStatusConst.DRAFT)
                {
                    OsorderStatusHistory hisStatusNew = new();
                    hisStatusNew.OrderRefNumber = model.OrderRefNumber;
                    hisStatusNew.ExternalOrdNbr = model.External_OrdNBR;
                    hisStatusNew.OrderDate = model.OrderDate;
                    hisStatusNew.DistributorCode = _distributorCode;
                    hisStatusNew.Sostatus = model.Status;
                    hisStatusNew.CreatedBy = username;
                    BaseResultModel resultSaveStatusHis = await _orderStatusHisService.SaveStatusHistory(hisStatusNew);
                    if (!resultSaveStatusHis.IsSuccess) return resultSaveStatusHis;
                }
                return new BaseResultModel
                {
                    Code = 200,
                    IsSuccess = true,
                    Message = "OK",
                    Data = new
                    {
                        OrderRefNumber = generatedNumber
                    }
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }

        public async Task<BaseResultModel> SaveWithConfirm(SaleOrderModel model, string token, string username)
        {
            try
            {
                //if (IsODSiteConstant) 
                    model.DistributorCode = _distributorCode;

                if (model.Id == Guid.Empty)
                {
                    return await InsertOrder(model, token, username, true);
                }
                else
                {
                    return await UpdateSO(model, token, username, true);
                }

            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }

        public async Task<BaseResultModel> InsertOrderFromFFA(SaleOrderModel model, string token, string username, string generatedNumber, List<Vat> vats, List<INV_InventoryTransaction> listInvTransaction, bool salesPriceIncludeVaT)
        {
            try
            {
                ////Generate RefNumber
                //var prefix = StringsHelper.GetPrefixYYM();
                //var orderRefNumberIndb = await _orderInformationsRepository.GetAllQueryable().Where(x => x.OrderRefNumber.Contains(prefix)).AsNoTracking().Select(x => x.OrderRefNumber).OrderByDescending(x => x).FirstOrDefaultAsync();
                //var generatedNumber = StringsHelper.GennerateCodeWithYearMonthFormat(prefix, orderRefNumberIndb != null ? orderRefNumberIndb : null);

                model.Id = Guid.NewGuid();
                // model.PromotionRefNumber = model.DistributorCode + model.CustomerId + model.CustomerShiptoID + model.OrderRefNumber;
                model.PromotionRefNumber = Guid.NewGuid().ToString();
                model.CreatedBy = username;
                model.CreatedDate = DateTime.Now;
                model.UpdatedBy = null;
                model.UpdatedDate = null;
                model.OrderRefNumber = generatedNumber;

                //model.ReferenceRefNbr = null;
                model.CancelNumber = null;
                model.ReasonCode = null;
                model.CancelDate = null;
                model.Disty_billtoID = null;
                model.DeliveredDate = null;
                model.isReturn = false;
                model.Status = SO_SaleOrderStatusConst.OPEN;
                model.IsPrintedDeliveryNote = false;
                model.PrintedDeliveryNoteCount = 0;
                model.LastedDeliveryNotePrintDate = null;
                model.MenuType = MenuTypeConst.SO_Menu02;
                model.Source = SO_SOURCE_CONST.MOBILE;
                model.ConfirmCount = 0;
                model.CompleteDate = null;
                model.ShipDate = null;
                model.Shipped_Promotion_Amt = 0;
                model.Shipped_SKUs = 0;
                model.Shipped_Qty = 0;
                model.Shipped_Promotion_Qty = 0;
                model.Shipped_Amt = 0;
                model.Shipped_Disc_Amt = 0;
                model.Shipped_line_Disc_Amt = 0;
                model.Shipped_Extend_Amt = 0;
                model.Ord_Disc_Amt = model.Orig_Ord_Disc_Amt;
                model.OwnerCode = _distributorCode;
                model.OwnerType = OwnerTypeConstant.DISTRIBUTOR;

                for (int i = 0; i < model.OrderItems.Count; i++)
                {
                    var item = model.OrderItems[i];
                    item.Id = Guid.NewGuid();
                    item.CreatedBy = username;
                    item.CreatedDate = DateTime.Now;
                    item.UpdatedBy = null;
                    item.UpdatedDate = null;
                    item.OrderRefNumber = model.OrderRefNumber;
                    //item.ItemId = 
                    item.ShippedQuantities = 0;
                    item.ShippedBaseQuantities = 0;
                    item.FailedQuantities = 0;
                    item.ShippingQuantities = 0;
                    item.RemainQuantities = 0;
                    item.DiscountID = model.DiscountID;
                    item.DiscountSchemeID = null;
                    item.DiscountDealID = null;
                    item.Shipped_Line_Amt = 0;
                    item.Shipped_line_Disc_Amt = 0;
                    item.Shipped_Line_Extend_Amt = 0;
                    item.IsDeleted = false;
                    item.ReturnBaseQuantities = 0;
                    item.ReturnQuantities = 0;
                    item.FailedBaseQuantities = 0;
                    item.ShippingBaseQuantities = 0;
                    item.OwnerCode = _distributorCode;
                    item.OwnerType = OwnerTypeConstant.DISTRIBUTOR;

                    // VAT, VATId, VatValue, VATCode
                    //VATDetail vatDetail = _clientService.CommonRequest<VATDetail>(CommonData.SystemUrlCode.SystemAdminAPI, $"Vat/GetVatById/{item.VatId}", Method.GET, $"Rdos {token.Split(" ").Last()}", null);
                    var vatDetail = vats.FirstOrDefault(x => x.Id == item.VatId);
                    if (vatDetail != null)
                    {
                        item.VatValue = vatDetail.VatValues;
                        item.VATCode = vatDetail.VatId;
                        //item.VAT = item.Ord_Line_Amt * ((decimal)vatDetail.VatValues / 100);
                    }

                    if (salesPriceIncludeVaT)
                        _calculateTaxService.CalculateTaxInCludeVAT(ref item);
                    else
                        _calculateTaxService.CalculateTaxNotInCludeVAT(ref item);

                    item.Ord_TotalLine_Disc_Amt =  _calculateTaxService.CalculateCommissionDiscount(model, item);

                    if (!string.IsNullOrWhiteSpace(item.PromotionType))
                    {
                        item.PromotionType = PROMO_PROMOTIONTYPECONST.Promotion;
                    }

                    _orderItemsRepository.Add(item, _schemaName);
                }

                //var confirmResult = await CommonConfirm(model, username, token, true);
                //if (!confirmResult.IsSuccess)
                //{
                //    return confirmResult;
                //}

                // Handle update transaction
                foreach (var invTransaction in listInvTransaction)
                {
                    invTransaction.OrderCode = model.OrderRefNumber;
                    invTransaction.UpdatedBy = model.CreatedBy;
                    invTransaction.UpdatedDate = DateTime.Now;
                    if (!invTransaction.IsCreateInFlow)
                    {
                        _invTransactionRepo.UpdateUnSaved(invTransaction, _schemaName);
                    }
                }

                CalCulateOrderHeaderFfa(ref model);
                _orderInformationsRepository.Add(model, _schemaName);

                if (model.Status != SO_SaleOrderStatusConst.DRAFT)
                {
                    OsorderStatusHistory hisStatusNew = new();
                    hisStatusNew.OrderRefNumber = model.OrderRefNumber;
                    hisStatusNew.ExternalOrdNbr = model.External_OrdNBR;
                    hisStatusNew.OrderDate = model.OrderDate;
                    hisStatusNew.DistributorCode = _distributorCode;
                    hisStatusNew.Sostatus = model.Status;
                    hisStatusNew.CreatedBy = username;
                    BaseResultModel resultSaveStatusHis = await _orderStatusHisService.SaveStatusHistory(hisStatusNew, false);
                    if (!resultSaveStatusHis.IsSuccess) return resultSaveStatusHis;
                }

                if (model.OrderItems.Any(x => !string.IsNullOrEmpty(x.PromotionCode)))
                {
                    await _promoService.ImportDataFromFFA(model, token);
                }
                return new BaseResultModel
                {
                    Code = 200,
                    IsSuccess = true,
                    Message = "OK",
                    Data = generatedNumber
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }

        public BaseResultModel CalCulateOrderHeaderFfa(ref SaleOrderModel model)
        {
            try
            {
                _calculateTaxService.CalculateTotalVAT(ref model);

                //var items = model.OrderItems.Where(x =>
                //    !x.IsDeleted &&
                //    !(x.IsKit && x.ItemCode == null)
                //).ToList();

                //model.Ord_SKUs = 0;
                //model.Ord_Qty = 0;
                //model.Promotion_Qty = 0;
                //model.Ord_Amt = 0;
                //model.Ord_Extend_Amt = 0;
                //model.TotalVAT = 0;

                //model.Orig_Ord_SKUs = 0;
                //model.Shipped_SKUs = 0;
                //model.Orig_Ord_Qty = 0;
                //model.Shipped_Qty = 0;
                //model.Orig_Promotion_Qty = 0;
                //model.Shipped_Promotion_Qty = 0;
                //model.Orig_Ord_Amt = 0;
                //model.Shipped_Amt = 0;
                //model.Orig_Ord_Disc_Amt = 0;
                //model.Ord_Disc_Amt = 0;
                //model.Shipped_Disc_Amt = 0;
                //model.Orig_Ordline_Disc_Amt = 0;
                //model.Ordline_Disc_Amt = 0;
                //model.Shipped_line_Disc_Amt = 0;
                //model.Orig_Ord_Extend_Amt = 0;
                //model.Shipped_Extend_Amt = 0;





                //model.Promotion_Amt = 0;
                //foreach (var item in items)
                //{
                //    model.Ord_SKUs += item.OrderQuantities > 0 ? 1 : 0; //Số SP trên đơn hàng được xác nhận
                //    model.Ord_Qty += item.OrderBaseQuantities; //Tổng sản lượng xác nhận đặt trên đơn hàng
                //    if (item.DiscountID != null)
                //    {
                //        model.Promotion_Qty += item.OrderQuantities; //Tổng sản lượng KM được xác nhận trên đơn hàng
                //    }
                //    model.Ord_Amt += item.Ord_Line_Amt;//Tổng doanh số được xác nhận trên đơn hàng
                //    model.TotalVAT += item.VAT;  //Tổng số thuế
                //    item.Ord_Line_Extend_Amt = item.Ord_Line_Amt - item.Ord_line_Disc_Amt;
                //    model.Ord_Extend_Amt += item.Ord_Line_Extend_Amt;  //Tổng tiền sau CK và KM được xác nhận
                //}
                //model.Ord_Extend_Amt = model.Ord_Extend_Amt - model.Ord_Disc_Amt;  //Tổng tiền sau CK và KM ban đầu

                return new BaseResultModel
                {
                    Code = 200,
                    IsSuccess = true,
                    Message = "OK"
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }

        public async Task<BaseResultModel> InsertOrderFromOneShop(SaleOrderModel model,List<INV_InventoryTransaction> listInvTransaction, string token, List<ODMappingOrderStatus> listMappingOrderStatus)
        {
            try
            {
                model.TotalLine = 0;
                model.Orig_Ord_SKUs = 0;
                model.Ord_SKUs = 0;
                foreach (var item in model.OrderItems)
                {
                    model.TotalLine += 1;
                    model.Orig_Ord_SKUs += 1;
                    model.Ord_SKUs += 1;
                    if (!string.IsNullOrWhiteSpace(item.PromotionType))
                    {
                        item.PromotionType = PROMO_PROMOTIONTYPECONST.Promotion;
                    }
                    _orderItemsRepository.Add(item, _schemaName);
                }

                // Handle update transaction
                foreach (var invTransaction in listInvTransaction)
                {
                    invTransaction.OrderCode = model.OrderRefNumber;
                    invTransaction.UpdatedBy = model.CreatedBy;
                    invTransaction.UpdatedDate = DateTime.Now;
                    if (!invTransaction.IsCreateInFlow)
                    {
                        _invTransactionRepo.UpdateUnSaved(invTransaction, _schemaName);
                    }
                }

                _orderInformationsRepository.Add(model, _schemaName);

                if (model.Status != SO_SaleOrderStatusConst.DRAFT)
                {
                    var statusMappingCurrent = listMappingOrderStatus.Where(x => x.SaleOrderStatus == model.SOStatus && x.OneShopOrderStatus == model.OSStatus).FirstOrDefault();
                    OsorderStatusHistory hisStatusNew = new();
                    hisStatusNew.OrderRefNumber = model.OrderRefNumber;
                    hisStatusNew.ExternalOrdNbr = model.External_OrdNBR;
                    hisStatusNew.OrderDate = model.OrderDate;
                    hisStatusNew.DistributorCode = _distributorCode;
                    //hisStatusNew.Sostatus = model.Status;
                    hisStatusNew.Sostatus = model.SOStatus;
                    hisStatusNew.SOStatusName = statusMappingCurrent?.SaleOrderStatusName;
                    //hisStatusNew.OneShopStatus = listMappingOrderStatus.Where(x => x.SaleOrderStatus == model.Status).Select(d => d.OneShopOrderStatus).FirstOrDefault();
                    hisStatusNew.OneShopStatus = model.OSStatus;
                    hisStatusNew.OneShopStatusName = statusMappingCurrent?.OneShopOrderStatusName;
                    hisStatusNew.CreatedBy = model.CreatedBy;
                    hisStatusNew.OutletCode = model.OSOutletCode;
                    BaseResultModel resultSaveStatusHis = await _orderStatusHisService.SaveStatusHistory(hisStatusNew, false, true);
                    if (!resultSaveStatusHis.IsSuccess) return resultSaveStatusHis;
                }

                if (model.OrderItems.Any(x => !string.IsNullOrEmpty(x.PromotionCode)))
                {
                    await _promoService.ImportDataFromFFA(model, token);
                }
                return new BaseResultModel
                {
                    Code = 200,
                    IsSuccess = true,
                    Message = "OK"
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }

        public async Task<BaseResultModel> UpdateSO(SaleOrderModel model, string token, string username, bool includeConfirm = false)
        {
            try
            {
                //if (IsODSiteConstant) 
                    model.DistributorCode = _distributorCode;

                var baseModel = await CommonGetDetail(new SaleOrderDetailQueryModel { DistributorCode = model.DistributorCode, OrderRefNumber = model.OrderRefNumber });
                if (baseModel == null || baseModel.Count == 0)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Code = 400,
                        Message = "SaleOrder not found"
                    };
                }

                foreach (var item in model.OrderItems)
                {
                    //if (IsODSiteConstant)
                    //{
                        item.OwnerType = OwnerTypeConstant.DISTRIBUTOR;
                        item.OwnerCode = _distributorCode;
                    //}

                    item.Ord_Line_Extend_Amt = item.Ord_Line_Amt - item.Ord_line_Disc_Amt;
                    if (includeConfirm)
                    {
                        item.Orig_Ord_Line_Amt = item.Ord_Line_Amt;
                        item.Orig_Ord_line_Disc_Amt = item.Ord_line_Disc_Amt;
                        item.Orig_Ord_Line_Extend_Amt = item.Ord_Line_Amt - item.Ord_line_Disc_Amt;
                        item.OriginalOrderQuantities = item.OrderQuantities;
                        item.OriginalOrderBaseQuantities = item.OrderBaseQuantities;
                    }
                    if (item.Id != Guid.Empty)
                    {
                        item.UpdatedBy = username;
                        item.UpdatedDate = DateTime.Now;
                        _orderItemsRepository.UpdateUnSaved(item, _schemaName);
                    }
                    else
                    {
                        var itemInsertData = item;
                        item.Id = Guid.NewGuid();
                        item.CreatedBy = username;
                        item.CreatedDate = DateTime.Now;
                        item.OrderRefNumber = model.OrderRefNumber;
                        _orderItemsRepository.Add(itemInsertData, _schemaName);
                    }
                }

                if (includeConfirm)
                {
                    var validateResult = await CommonValidate(model, token);
                    if (!validateResult.IsSuccess)
                    {

                        model.UpdatedBy = username;
                        model.UpdatedDate = DateTime.Now;
                        _orderInformationsRepository.UpdateUnSaved(model, _schemaName);
                        _orderInformationsRepository.Save(_schemaName);
                        return validateResult;
                    }
                    var confirmResult = await CommonConfirm(model, username, token, false);

                    if (!confirmResult.IsSuccess)
                    {

                        return confirmResult;
                    }
                }
                var calculateResult = CommonCalCulateOrderHeader(ref model, token);
                // if (model.Status != SO_SaleOrderStatusConst.DRAFT)
                // {
                //     return new BaseResultModel
                //     {
                //         IsSuccess = false,
                //         Code = 400,
                //         Message = "Only draft SO is allowed"
                //     };
                // }
                // var handlerAttResult = await CommonHandleInternalSoAttribute(model, token);
                // if (handlerAttResult.IsSuccess)
                // {
                //     model = handlerAttResult.Data;
                // }

                model.Status = includeConfirm ? SO_SaleOrderStatusConst.OPEN : model.Status;
                model.CreatedDate = baseModel.Select(x => x.OrderInformation.CreatedDate).FirstOrDefault();
                model.CreatedBy = baseModel.Select(x => x.OrderInformation.CreatedBy).FirstOrDefault();
                model.UpdatedBy = username;
                model.UpdatedDate = DateTime.Now;

                //if (IsODSiteConstant)
                //{
                    model.OwnerType = OwnerTypeConstant.DISTRIBUTOR;
                    model.OwnerCode = _distributorCode;
                //}

                _orderInformationsRepository.UpdateUnSaved(model, _schemaName);
                _orderInformationsRepository.Save(_schemaName);

                if (model.Status != SO_SaleOrderStatusConst.DRAFT)
                {
                    OsorderStatusHistory hisStatusNew = new();
                    hisStatusNew.OrderRefNumber = model.OrderRefNumber;
                    hisStatusNew.ExternalOrdNbr = model.External_OrdNBR;
                    hisStatusNew.OrderDate = model.OrderDate;
                    hisStatusNew.DistributorCode = _distributorCode;
                    hisStatusNew.Sostatus = model.Status;
                    hisStatusNew.CreatedBy = username;
                    BaseResultModel resultSaveStatusHis = await _orderStatusHisService.SaveStatusHistory(hisStatusNew);
                    if (!resultSaveStatusHis.IsSuccess) return resultSaveStatusHis;
                }

                return new BaseResultModel
                {
                    Code = 200,
                    IsSuccess = true,
                    Message = "OK",
                    Data = new
                    {
                        OrderRefNumber = model.OrderRefNumber
                    }
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }

        public async Task<BaseResultModel> CancelMutipleDeliveredSO(List<SO_OrderList> OrderList, string distributorCode, string username, string token)
        {
            try
            {
                //if (IsODSiteConstant) 
                    distributorCode = _distributorCode;
                List<String> orderRefNumbers = OrderList.Select(x => x.OrderRefNumber).ToList();
                var listSaleOrderModel = (await SearchSO(new SaleOrderSearchParamsModel
                {
                    IsDropdown = true,
                    OrderRefNumbers = orderRefNumbers,
                    DistributorCode = distributorCode
                })).Data.Items;

                if (listSaleOrderModel.Count < orderRefNumbers.Count)
                {
                    return new BaseResultModel
                    {
                        Code = 400,
                        IsSuccess = false,
                        Message = "Missing SO in list Input"
                    };
                }
                List<string> inventoryErrorMessage = new();
                foreach (var saleOrder in listSaleOrderModel)
                {
                    var reasonCode = OrderList.Where(x => x.OrderRefNumber == saleOrder.OrderRefNumber).Select(x => x.ReasonCode).FirstOrDefault();
                    saleOrder.ReasonCode = reasonCode;
                    if (saleOrder.Status == SO_SaleOrderStatusConst.DELIVERED | saleOrder.Status == SO_SaleOrderStatusConst.PARTIALDELIVERED)
                    {
                        saleOrder.UpdatedBy = username;
                        saleOrder.UpdatedDate = DateTime.Now;

                        #region INV transaction
                        List<INV_TransactionModel> cancelTransactionList = new();
                        foreach (var item in saleOrder.OrderItems.Where(x => !x.IsDeleted && x.ItemCode != null).ToList())
                        {
                            cancelTransactionList.Add(new INV_TransactionModel
                            {
                                OrderCode = saleOrder.OrderRefNumber,
                                ItemId = item.ItemId,
                                ItemCode = item.ItemCode,
                                ItemDescription = item.ItemDescription,
                                Uom = item.UOM,
                                Quantity = item.ShippedQuantities,
                                OrderBaseQuantity = item.ShippedBaseQuantities,
                                BaseQuantity = item.ShippedBaseQuantities,
                                TransactionDate = DateTime.Now,
                                TransactionType = INV_TransactionType.SO_CL,
                                WareHouseCode = saleOrder.WareHouseID,
                                LocationCode = item.LocationID,
                                DistributorCode = saleOrder.DistributorCode,
                                DSACode = saleOrder.DSAID,
                                Description = saleOrder.Note,
                                ReasonCode = saleOrder.ReasonCode,
                                ReasonDescription = saleOrder.ReasonCode != null ? (await _reasonRepository.GetAllQueryable().FirstOrDefaultAsync(x => !string.IsNullOrWhiteSpace(x.ReasonCode) && x.ReasonCode == saleOrder.ReasonCode))?.Description ?? null : null
                            });
                        }
                        //call api transaction
                        _client = new RestClient(CommonData.SystemUrl.Where(x => x.Code == CommonData.SystemUrlCode.ODInventoryAPI).Select(x => x.Url).FirstOrDefault());
                        _client.Authenticator = new JwtAuthenticator($"{token}");
                        var json = JsonConvert.SerializeObject(cancelTransactionList);
                        var request = new RestRequest($"InventoryTransaction/BulkCreate", Method.POST);
                        request.AddJsonBody(json);
                        // Add Header
                        request.AddHeader(OD_Constant.KeyHeader, _distributorCode);

                        var resultInventoryItem = _client.Execute(request);
                        var resultData = JsonConvert.DeserializeObject<BaseResultModel>(JsonConvert.DeserializeObject(resultInventoryItem.Content).ToString());
                        if (!resultData.IsSuccess)
                        {
                            inventoryErrorMessage.Add(resultData.Message);
                        }
                        #endregion
                        saleOrder.Status = SO_SaleOrderStatusConst.CANCEL;
                        saleOrder.CancelDate = DateTime.Now;
                        _orderInformationsRepository.UpdateUnSaved(saleOrder, _schemaName);

                        if (saleOrder.Status != SO_SaleOrderStatusConst.DRAFT)
                        {
                            var _osStatus = new ODMappingOrderStatus();
                            if (!string.IsNullOrWhiteSpace(saleOrder.External_OrdNBR) 
                                && saleOrder.Source == SO_SOURCE_CONST.ONESHOP)
                            {
                                _osStatus = await _orderStatusHisService.HandleOSMappingStatus(saleOrder.Status);
                            }
                            else {
                                _osStatus = null;
                            }

                            saleOrder.OSStatus = _osStatus?.OneShopOrderStatus;

                            OsorderStatusHistory hisStatusNew = new();
                            hisStatusNew.OrderRefNumber = saleOrder.OrderRefNumber;
                            hisStatusNew.ExternalOrdNbr = saleOrder.External_OrdNBR;
                            hisStatusNew.OrderDate = saleOrder.OrderDate;
                            hisStatusNew.DistributorCode = _distributorCode;
                            hisStatusNew.Sostatus = saleOrder.Status;
                            hisStatusNew.SOStatusName = _osStatus?.SaleOrderStatusName;
                            hisStatusNew.CreatedBy = username;
                            hisStatusNew.OutletCode = saleOrder.OSOutletCode;
                            hisStatusNew.OneShopStatus = _osStatus?.OneShopOrderStatus;
                            hisStatusNew.OneShopStatusName = _osStatus?.OneShopOrderStatusName;
                            await _orderStatusHisService.SaveStatusHistory(hisStatusNew, false);
                        }

                        // Trả Booked prômtion budget
                        if (saleOrder.PromotionRefNumber != null)
                        {
                            var promoCusDetails = await _customerProgramDetailRepo.GetAllQueryable(x =>
                                !string.IsNullOrEmpty(x.BudgetCode) &&
                                !string.IsNullOrEmpty(x.PromotionRefNumber) &&
                                x.PromotionRefNumber == saleOrder.PromotionRefNumber,
                                null, null, _schemaName)
                                .ToListAsync();

                            if (promoCusDetails != null && promoCusDetails.Count > 0)
                            {
                                foreach (var cusDetail in promoCusDetails)
                                {
                                    if (cusDetail.BudgetBooked > 0)
                                    {
                                        var promo = await _customerProgramRepo
                                            .GetAllQueryable(x => x.ProgramCustomersKey == cusDetail.ProgramCustomersKey, null, null, _schemaName)
                                            .FirstOrDefaultAsync();

                                        var principal = await _principalRepo.GetAllQueryable().FirstOrDefaultAsync();
                                        var budgetDataReq = new BudgetReqModel
                                        {
                                            budgetCode = cusDetail.BudgetCode,
                                            budgetType = cusDetail.BudgetType,
                                            customerCode = promo.CustomerCode,
                                            customerShipTo = promo.ShiptoCode,
                                            saleOrg = promo.SalesOrgCode,
                                            budgetAllocationLevel = cusDetail.BudgetAllocationLevel,
                                            budgetBook = -cusDetail.BudgetBooked,
                                            salesTerritoryValueCode = null,
                                            promotionCode = promo.ProgramCode,
                                            promotionLevel = cusDetail.DetailLevel,
                                            routeZoneCode = promo.RouteZoneCode,
                                            dsaCode = promo.DsaCode,
                                            subAreaCode = promo.SubArea,
                                            areaCode = promo.Area,
                                            subRegionCode = promo.SubRegion,
                                            regionCode = promo.Region,
                                            branchCode = promo.Branch,
                                            nationwideCode = principal.Country,
                                            salesOrgCode = promo.SalesOrgCode,
                                            referalCode = null, //?
                                            distributorCode = _distributorCode
                                        };
                                        switch (cusDetail.BudgetAllocationLevel)
                                        {
                                            case "DSA":
                                                {
                                                    budgetDataReq.salesTerritoryValueCode = promo.DsaCode;
                                                    break;
                                                }
                                            case "TL01":
                                                {
                                                    budgetDataReq.salesTerritoryValueCode = promo.Branch;
                                                    break;
                                                }
                                            case "TL02":
                                                {
                                                    budgetDataReq.salesTerritoryValueCode = promo.Region;
                                                    break;
                                                }
                                            case "TL03":
                                                {
                                                    budgetDataReq.salesTerritoryValueCode = promo.SubRegion;
                                                    break;
                                                }
                                            case "TL04":
                                                {
                                                    budgetDataReq.salesTerritoryValueCode = promo.Area;
                                                    break;
                                                }
                                            case "TL05":
                                                {
                                                    budgetDataReq.salesTerritoryValueCode = promo.SubArea;
                                                    break;
                                                }
                                            default:
                                                break;
                                        }

                                        // Trả budget
                                        var budgetChecked = (await _clientService.CommonRequestAsync<ResultModelWithObject<BudgetResModel>>(CommonData.SystemUrlCode.ODTpAPI, $"external_checkbudget/checkbudget", Method.POST, token, budgetDataReq)).Data;
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                            }
                        }
                        else
                        {
                            foreach (var item in saleOrder.OrderItems.Where(x => !x.IsDeleted && !string.IsNullOrEmpty(x.PromotionBudgetCode) && x.PromotionBudgetQuantities > 0).ToList())
                            {
                                await HandleCancelBudgetSO(item, saleOrder, token);
                            }
                        }

                    }
                    else
                    {
                        return new BaseResultModel
                        {
                            Code = 400,
                            IsSuccess = false,
                            Message = "Only Cancel order "
                        };
                    }
                }

                if (inventoryErrorMessage != null && inventoryErrorMessage.Count > 0)
                {
                    return new BaseResultModel
                    {
                        Code = 400,
                        IsSuccess = false,
                        Message = "Fail when create Inventory transactions",
                        Data = inventoryErrorMessage
                    };
                }
                _orderInformationsRepository.Save(_schemaName);

                foreach (var saleOrder in listSaleOrderModel)
                {
                    Serilog.Log.Information($"############ Chuẩn bị vào SendNotification : {saleOrder.OSStatus} - {saleOrder.Status}");
                    if (saleOrder.OSStatus != null)
                    {
                        // Send notification
                        OSNotificationModel reqNoti = new();
                        reqNoti.External_OrdNBR = saleOrder.External_OrdNBR;
                        reqNoti.OrderRefNumber = saleOrder.OrderRefNumber;
                        reqNoti.OSStatus = saleOrder.OSStatus;
                        reqNoti.SOStatus = saleOrder.Status;
                        reqNoti.DistributorCode = saleOrder.DistributorCode;
                        reqNoti.DistributorName = saleOrder.DistributorName;
                        reqNoti.OutletCode = saleOrder.OSOutletCode;
                        reqNoti.Purpose = OSNotificationPurpose.GetPurpose(saleOrder.Status);

                        await _osNotifiService.SendNotification(reqNoti, token);
                    }

                }

                return new BaseResultModel
                {
                    Code = 200,
                    IsSuccess = true,
                    Message = "OK"
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }
        public async Task<BaseResultModel> DeleteSO(SaleOrderDetailQueryModel query, string username)
        {
            try
            {
                //if (IsODSiteConstant) 
                query.DistributorCode = _distributorCode;
                var baseModel = await CommonGetDetail(query);
                if (baseModel == null || baseModel.Count == 0)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Code = 400,
                        Message = "SO not found"
                    };
                }
                var headerDeleteData = _mapper.Map<SO_OrderInformations>(baseModel.Select(x => x.OrderInformation).FirstOrDefault());
                if (headerDeleteData.Status != SO_SaleOrderStatusConst.DRAFT)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Code = 400,
                        Message = "Only draft SO is allowed"
                    };
                }

                headerDeleteData.IsDeleted = true;
                headerDeleteData.UpdatedBy = username;
                headerDeleteData.UpdatedDate = DateTime.Now;
                _orderInformationsRepository.UpdateUnSaved(headerDeleteData, _schemaName);

                foreach (var item in baseModel.Where(x => x.OrderItem != null).Select(x => x.OrderItem).ToList())
                {
                    item.IsDeleted = true;
                    item.UpdatedBy = username;
                    item.UpdatedDate = DateTime.Now;
                    _orderItemsRepository.UpdateUnSaved(item, _schemaName);
                }

                _orderInformationsRepository.Save(_schemaName);
                return new BaseResultModel
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "OK"
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }

        public async Task<BaseResultModel> ConfirmSO(SaleOrderModel model, string token, string username)
        {
            try
            {
                model.DistributorCode = _distributorCode;
                var baseModel = await CommonGetDetail(new SaleOrderDetailQueryModel { DistributorCode = model.DistributorCode, OrderRefNumber = model.OrderRefNumber });

                if (baseModel == null || baseModel.Count == 0)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Code = 400,
                        Message = "SaleOrder not found"
                    };
                }

                if (baseModel.Any(x => x.OrderInformation.Status == SO_SaleOrderStatusConst.OPEN))
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Code = 400,
                        Message = "SalesOrder already opened"
                    };
                }

                foreach (var item in model.OrderItems)
                {


                    item.OriginalOrderQuantities = item.OrderQuantities;
                    item.OriginalOrderBaseQuantities = item.OrderBaseQuantities;
                    item.Orig_Ord_Line_Extend_Amt = item.Ord_Line_Amt + item.VAT - item.Ord_line_Disc_Amt;
                    item.Ord_Line_Extend_Amt = item.Ord_Line_Amt + item.VAT - item.Ord_line_Disc_Amt;

                    if (item.Id != Guid.Empty)
                    {
                        item.UpdatedBy = username;
                        item.UpdatedDate = DateTime.Now;
                        _orderItemsRepository.UpdateUnSaved(item, _schemaName);
                    }
                    else
                    {
                        var itemInsertData = item;
                        item.Id = Guid.NewGuid();
                        item.CreatedBy = username;
                        item.CreatedDate = DateTime.Now;
                        item.OrderRefNumber = model.OrderRefNumber;
                        _orderItemsRepository.Add(itemInsertData, _schemaName);
                    }
                }

                var validateResult = await CommonValidate(model, token);
                if (!validateResult.IsSuccess)
                {
                    _orderInformationsRepository.UpdateUnSaved(model, _schemaName);
                    _orderInformationsRepository.Save(_schemaName);
                    return validateResult;
                }

                var commonConfirmResult = await CommonConfirm(model, username, token, false);
                if (!commonConfirmResult.IsSuccess)
                {

                    return commonConfirmResult;
                }

                var calculateResult = CommonCalCulateOrderHeader(ref model, token);
                if (model.Status != SO_SaleOrderStatusConst.DRAFT)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Code = 400,
                        Message = "Only Confirm Order Darft"
                    };
                }
                model.Status = SO_SaleOrderStatusConst.OPEN;
                model.UpdatedBy = username;
                model.UpdatedDate = DateTime.Now;
                model.CreatedDate = baseModel.Select(x => x.OrderInformation.CreatedDate).FirstOrDefault();
                model.CreatedBy = baseModel.Select(x => x.OrderInformation.CreatedBy).FirstOrDefault();

                //if (IsODSiteConstant)
                //{
                    model.OwnerType = OwnerTypeConstant.DISTRIBUTOR;
                    model.OwnerCode = _distributorCode;
                //}
                _orderInformationsRepository.UpdateUnSaved(model, _schemaName);
                _orderInformationsRepository.Save(_schemaName);

                if (model.Status != SO_SaleOrderStatusConst.DRAFT)
                {
                    OsorderStatusHistory hisStatusNew = new();
                    hisStatusNew.OrderRefNumber = model.OrderRefNumber;
                    hisStatusNew.ExternalOrdNbr = model.External_OrdNBR;
                    hisStatusNew.OrderDate = model.OrderDate;
                    hisStatusNew.DistributorCode = _distributorCode;
                    hisStatusNew.Sostatus = model.Status;
                    hisStatusNew.CreatedBy = username;
                    BaseResultModel resultSaveStatusHis = await _orderStatusHisService.SaveStatusHistory(hisStatusNew);
                    if (!resultSaveStatusHis.IsSuccess) return resultSaveStatusHis;
                }
                return new BaseResultModel
                {
                    Code = 200,
                    IsSuccess = true,
                    Message = "OK"
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }

        public async Task<BaseResultModel> CancelNewSO(SaleOrderModel model, string token, string username, bool isFromOs = false)
        {
            try
            {
                // if (model.Status == SO_SaleOrderStatusConst.OPEN || model.Status == SO_SaleOrderStatusConst.DRAFT || model.Status == SO_SaleOrderStatusConst.PARTIALDELIVERED || model.Status == SO_SaleOrderStatusConst.DELIVERED)
                var modelIndb = _orderInformationsRepository
                    .GetAllQueryable(x => !string.IsNullOrWhiteSpace(x.OrderRefNumber) &&
                    x.OrderRefNumber == model.OrderRefNumber,
                    null, null, _schemaName)
                    .AsNoTracking()
                    .FirstOrDefault();

                if (modelIndb == null)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Code = 400,
                        Message = "Order notfound"
                    };
                }
                if (modelIndb.Status == SO_SaleOrderStatusConst.OPEN || modelIndb.Status == SO_SaleOrderStatusConst.DRAFT)
                {
                    if (modelIndb.Status == SO_SaleOrderStatusConst.OPEN)
                    {
                        var transactionResult = await HandleInventoryTransaction(model, INV_TransactionType.SO_BOOKED_CANCEL, token);
                        if (!transactionResult.IsSuccess) return transactionResult;
                    }
                    model.Status = SO_SaleOrderStatusConst.CANCEL;
                    model.CancelDate = DateTime.Now;
                    //if (IsODSiteConstant)
                    //{
                        model.OwnerType = OwnerTypeConstant.DISTRIBUTOR;
                        model.OwnerCode = _distributorCode;
                    //}
                    _orderInformationsRepository.UpdateUnSaved(model, _schemaName);

                    // Trả Booked prômtion budget
                    if (model.PromotionRefNumber != null)
                    {
                        var promoCusDetails = await _customerProgramDetailRepo.GetAllQueryable(x =>
                            !string.IsNullOrEmpty(x.BudgetCode) &&
                            !string.IsNullOrEmpty(x.PromotionRefNumber) &&
                            x.PromotionRefNumber == model.PromotionRefNumber,
                            null, null, _schemaName)
                            .ToListAsync();

                        if (promoCusDetails != null && promoCusDetails.Count > 0)
                        {
                            foreach (var cusDetail in promoCusDetails)
                            {
                                if (cusDetail.BudgetBooked > 0)
                                {
                                    var promo = await _customerProgramRepo
                                        .GetAllQueryable(x => x.ProgramCustomersKey == cusDetail.ProgramCustomersKey,
                                        null, null, _schemaName)
                                        .FirstOrDefaultAsync();

                                    var principal = await _principalRepo.GetAllQueryable().FirstOrDefaultAsync();
                                    var budgetDataReq = new BudgetReqModel
                                    {
                                        budgetCode = cusDetail.BudgetCode,
                                        budgetType = cusDetail.BudgetType,
                                        customerCode = promo.CustomerCode,
                                        customerShipTo = promo.ShiptoCode,
                                        saleOrg = promo.SalesOrgCode,
                                        budgetAllocationLevel = cusDetail.BudgetAllocationLevel,
                                        budgetBook = -cusDetail.BudgetBooked,
                                        salesTerritoryValueCode = null,
                                        promotionCode = promo.ProgramCode,
                                        promotionLevel = cusDetail.DetailLevel,
                                        routeZoneCode = promo.RouteZoneCode,
                                        dsaCode = promo.DsaCode,
                                        subAreaCode = promo.SubArea,
                                        areaCode = promo.Area,
                                        subRegionCode = promo.SubRegion,
                                        regionCode = promo.Region,
                                        branchCode = promo.Branch,
                                        nationwideCode = principal.Country,
                                        salesOrgCode = promo.SalesOrgCode,
                                        referalCode = model.OrderRefNumber,
                                        distributorCode = _distributorCode
                                    };
                                    switch (cusDetail.BudgetAllocationLevel)
                                    {
                                        case "DSA":
                                            {
                                                budgetDataReq.salesTerritoryValueCode = promo.DsaCode;
                                                break;
                                            }
                                        case "TL01":
                                            {
                                                budgetDataReq.salesTerritoryValueCode = promo.Branch;
                                                break;
                                            }
                                        case "TL02":
                                            {
                                                budgetDataReq.salesTerritoryValueCode = promo.Region;
                                                break;
                                            }
                                        case "TL03":
                                            {
                                                budgetDataReq.salesTerritoryValueCode = promo.SubRegion;
                                                break;
                                            }
                                        case "TL04":
                                            {
                                                budgetDataReq.salesTerritoryValueCode = promo.Area;
                                                break;
                                            }
                                        case "TL05":
                                            {
                                                budgetDataReq.salesTerritoryValueCode = promo.SubArea;
                                                break;
                                            }
                                        default:
                                            break;
                                    }

                                    // Trả budget
                                    var budgetChecked = (await _clientService.CommonRequestAsync<ResultModelWithObject<BudgetResModel>>(CommonData.SystemUrlCode.ODTpAPI, $"external_checkbudget/checkbudget", Method.POST, token, budgetDataReq)).Data;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach(var item in model.OrderItems.Where(x => !x.IsDeleted && !string.IsNullOrEmpty(x.PromotionBudgetCode) && x.PromotionBudgetQuantities > 0).ToList())
                        {
                            await HandleCancelBudgetSO(item, model, token);
                        }
                    }

                    _orderInformationsRepository.Save(_schemaName);
                    if (model.Status != SO_SaleOrderStatusConst.DRAFT)
                    {
                        var _osStatus = new ODMappingOrderStatus();
                        if (!string.IsNullOrWhiteSpace(model.External_OrdNBR) 
                            && model.Source == SO_SOURCE_CONST.ONESHOP)
                        {
                            _osStatus = await _orderStatusHisService.HandleOSMappingStatus(model.Status, isFromOs);
                        }
                        else {
                            _osStatus = null;
                        }

                        model.OSStatus = _osStatus?.OneShopOrderStatus;

                        OsorderStatusHistory hisStatusNew = new();
                        hisStatusNew.OrderRefNumber = model.OrderRefNumber;
                        hisStatusNew.ExternalOrdNbr = model.External_OrdNBR;
                        hisStatusNew.OrderDate = model.OrderDate;
                        hisStatusNew.DistributorCode = _distributorCode;
                        hisStatusNew.Sostatus = model.Status;
                        hisStatusNew.SOStatusName = _osStatus?.SaleOrderStatusName;
                        hisStatusNew.CreatedBy = username;
                        hisStatusNew.OutletCode = model.OSOutletCode;
                        hisStatusNew.OneShopStatus = _osStatus?.OneShopOrderStatus;
                        hisStatusNew.OneShopStatusName = _osStatus?.OneShopOrderStatusName;
                        BaseResultModel resultSaveStatusHis = await _orderStatusHisService.SaveStatusHistory(hisStatusNew);
                        if (!resultSaveStatusHis.IsSuccess) return resultSaveStatusHis;

                        Serilog.Log.Information($"############ Chuẩn bị vào SendNotification : {model.OSStatus} - {model.Status}");
                        if (model.OSStatus != null && !isFromOs)
                        {
                            // Send notification
                            OSNotificationModel reqNoti = new();
                            reqNoti.External_OrdNBR = model.External_OrdNBR;
                            reqNoti.OrderRefNumber = model.OrderRefNumber;
                            reqNoti.OSStatus = model.OSStatus;
                            reqNoti.SOStatus = model.Status;
                            reqNoti.DistributorCode = model.DistributorCode;
                            reqNoti.DistributorName = model.DistributorName;
                            reqNoti.OutletCode = model.OSOutletCode;
                            reqNoti.Purpose = OSNotificationPurpose.GetPurpose(model.Status);

                            await _osNotifiService.SendNotification(reqNoti, token, isFromOs);
                        }

                    }
                    return new BaseResultModel
                    {
                        IsSuccess = true,
                        Code = 200,
                        Message = "Ok"
                    };
                }
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 400,
                    Message = "only darft and opened sale orders is allowed"
                };

            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }

        public async Task<BaseResultModel> CompleteSO(SaleOrderDetailQueryModel query, string token, string username)
        {
            try
            {
                query.DistributorCode = _distributorCode;
                var getDetailResult = await GetDetailSO(query);
                if (!getDetailResult.IsSuccess)
                {
                    return new BaseResultModel
                    {
                        Code = 400,
                        Data = null,
                        Message = getDetailResult.Message,
                        IsSuccess = false,
                    };
                }
                SaleOrderModel detailSO = getDetailResult.Data;
                if (!detailSO.IsDirect)
                {
                    return new BaseResultModel
                    {
                        Code = 400,
                        Data = null,
                        Message = "Can only complete Indirect SalesOrder",
                        IsSuccess = false,
                    };
                }

                if (detailSO.Status != SO_SaleOrderStatusConst.OPEN)
                {
                    return new BaseResultModel
                    {
                        Code = 400,
                        Data = null,
                        Message = "Only complete opened SO ",
                        IsSuccess = false,
                    };
                }
                detailSO.Shipped_Extend_Amt = detailSO.Ord_Extend_Amt;
                detailSO.Shipped_Amt = 0;
                detailSO.Shipped_Qty = 0;
                detailSO.Shipped_SKUs = 0;
                detailSO.Shipped_Promotion_Amt = 0;
                List<INV_TransactionModel> transactionData = new();

                foreach (var item in detailSO.OrderItems.Where(x => x.ItemCode != null || x.IsKit).ToList())
                {
                    item.ShippedQuantities = item.OrderQuantities;
                    item.ShippedBaseQuantities = item.OrderBaseQuantities;
                    item.Shipped_Line_Amt = item.Ord_Line_Amt;
                    item.Shipped_Line_Extend_Amt = item.Ord_Line_Extend_Amt;
                    item.Shipped_line_Disc_Amt = item.Ord_line_Disc_Amt;
                    if (item.ItemCode != null)
                    {
                        // detailSO.Shipped_Extend_Amt += item.Shipped_Line_Extend_Amt;
                        detailSO.Shipped_Amt += item.Shipped_Line_Amt;
                        detailSO.Shipped_Qty += item.ShippedBaseQuantities;
                        detailSO.Shipped_SKUs += 1;
                        detailSO.Shipped_Promotion_Amt += item.Shipped_line_Disc_Amt;

                        transactionData.Add(new INV_TransactionModel
                        {
                            OrderCode = detailSO.OrderRefNumber,
                            ItemId = item.ItemId,
                            ItemCode = item.ItemCode,
                            ItemDescription = item.ItemDescription,
                            Uom = item.UOM,
                            Quantity = item.ShippedQuantities, // số lượng cần đặt
                            BaseQuantity = item.ShippedBaseQuantities, //base cua thằng tr
                            OrderBaseQuantity = item.OrderBaseQuantities,
                            TransactionDate = DateTime.Now,
                            TransactionType = INV_TransactionType.SO_SHIPPED_DIRECT,
                            WareHouseCode = detailSO.WareHouseID,
                            LocationCode = item.LocationID,
                            DistributorCode = detailSO.DistributorCode,
                            DSACode = detailSO.DSAID,
                            Description = detailSO.Note
                        });
                    }

                }

                //call api transaction
                _client = new RestClient(CommonData.SystemUrl.Where(x => x.Code == CommonData.SystemUrlCode.ODInventoryAPI).Select(x => x.Url).FirstOrDefault());
                _client.Authenticator = new JwtAuthenticator($"{token}");
                var json = JsonConvert.SerializeObject(transactionData);
                var request = new RestRequest($"InventoryTransaction/BulkCreate", Method.POST);
                request.AddJsonBody(json);
                // Add Header
                request.AddHeader(OD_Constant.KeyHeader, _distributorCode);
                var result = _client.Execute(request);

                var resultData = JsonConvert.DeserializeObject<BaseResultModel>(JsonConvert.DeserializeObject(result.Content).ToString());
                if (!resultData.IsSuccess)
                {
                   resultData.Message = "Inventory transaction: " + resultData.Message;
                   resultData.Data = null;
                   return resultData;
                }
                detailSO.Status = SO_SaleOrderStatusConst.DELIVERED;


                var requestCusDisProgram = new
                {
                    saleOrgCode = detailSO.SalesOrgID,
                    sicCode = detailSO.SIC_ID,
                    customerCode = detailSO.CustomerId,
                    shiptoCode = detailSO.CustomerShiptoID,
                    routeZoneCode = detailSO.RouteZoneID,
                    dsaCode = detailSO.DSAID,
                    branch = detailSO.BranchId,
                    region = detailSO.RegionId,
                    subRegion = detailSO.SubRegionId,
                    area = detailSO.AreaId,
                    subArea = detailSO.SubAreaId,
                    distributorCode = detailSO.DistributorCode
                    // saleOrgCode = "SOGT3LV",
                    // sicCode = "SICIT0202",
                    // customerCode = "C123456789",
                    // shiptoCode = "S001",
                    // routeZoneCode = "RZ7K",
                    // dsaCode = "DSATDN01",
                    // branch = "TL01-N",
                    // region = "TL02-N1",
                    // area = "TL04-N12",
                };
                var cusDisprog = _clientService.CommonRequest<ResultModelWithObject<DiscountModel>>(CommonData.SystemUrlCode.ODTpAPI, $"/tpdiscount/getdiscountbycustomer", Method.POST, token, requestCusDisProgram);

                if (cusDisprog != null && cusDisprog.Data != null)
                {
                    var discountResult = new
                    {
                        discountCode = cusDisprog.Data.code,
                        discountLevelId = cusDisprog.Data.listDiscountStructureDetails.Select(x => x.id).FirstOrDefault(),
                        purchaseAmount = detailSO.Shipped_Amt - detailSO.Shipped_line_Disc_Amt
                    };
                    var discountamt = _clientService.CommonRequest<DiscountResultModel>(CommonData.SystemUrlCode.ODTpAPI, $"/tpdiscount/discountresult", Method.POST, token, discountResult);
                    if (discountamt != null) 
                    {
                        detailSO.DiscountID = cusDisprog.Data.code;
                        detailSO.Shipped_Disc_Amt = discountamt.discountAmount;
                    }
                }

                detailSO.CompleteDate = DateTime.Now;
                _orderInformationsRepository.Update(detailSO, _schemaName);
                _orderItemsRepository.UpdateRange(detailSO.OrderItems, _schemaName);
                _orderItemsRepository.Save(_schemaName);

                if (detailSO.Status != SO_SaleOrderStatusConst.DRAFT)
                {
                    var _osStatus = new ODMappingOrderStatus();
                    if (!string.IsNullOrWhiteSpace(detailSO.External_OrdNBR) 
                        && detailSO.Source == SO_SOURCE_CONST.ONESHOP)
                    {
                        _osStatus = await _orderStatusHisService.HandleOSMappingStatus(detailSO.Status);
                    }
                    else {
                        _osStatus = null;
                    }

                    detailSO.OSStatus = _osStatus?.OneShopOrderStatus;

                    OsorderStatusHistory hisStatusNew = new();
                    hisStatusNew.OrderRefNumber = detailSO.OrderRefNumber;
                    hisStatusNew.ExternalOrdNbr = detailSO.External_OrdNBR;
                    hisStatusNew.OrderDate = detailSO.OrderDate;
                    hisStatusNew.DistributorCode = _distributorCode;
                    hisStatusNew.Sostatus = detailSO.Status;
                    hisStatusNew.SOStatusName = _osStatus?.SaleOrderStatusName;
                    hisStatusNew.CreatedBy = username;
                    hisStatusNew.OutletCode = detailSO.OSOutletCode;
                    hisStatusNew.OneShopStatus = _osStatus?.OneShopOrderStatus;
                    hisStatusNew.OneShopStatusName = _osStatus?.OneShopOrderStatusName;

                    BaseResultModel resultSaveStatusHis = await _orderStatusHisService.SaveStatusHistory(hisStatusNew);
                    if (!resultSaveStatusHis.IsSuccess) return resultSaveStatusHis;

                    Serilog.Log.Information($"############ Chuẩn bị vào SendNotification : {detailSO.OSStatus} - {detailSO.Status}");
                    if (detailSO.OSStatus != null)
                    {
                        // Send notification
                        OSNotificationModel reqNoti = new();
                        reqNoti.External_OrdNBR = detailSO.External_OrdNBR;
                        reqNoti.OrderRefNumber = detailSO.OrderRefNumber;
                        reqNoti.OSStatus = detailSO.OSStatus;
                        reqNoti.SOStatus = detailSO.Status;
                        reqNoti.DistributorCode = detailSO.DistributorCode;
                        reqNoti.DistributorName = detailSO.DistributorName;
                        reqNoti.OutletCode = detailSO.OSOutletCode;
                        reqNoti.Purpose = OSNotificationPurpose.GetPurpose(detailSO.Status);

                        await _osNotifiService.SendNotification(reqNoti, token);
                    }
                }
                return new BaseResultModel
                {
                    Code = 200,
                    Message = "OK",
                    IsSuccess = true,
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }

        #endregion

        #region Get

        public async Task<ResultModelWithObject<SaleOrderModel>> GetDetailSO(SaleOrderDetailQueryModel query)
        {
            try
            {
                query.DistributorCode = _distributorCode;

                var baseModel = await CommonGetDetail(query);
                if (baseModel.Count == 0)
                {
                    return new ResultModelWithObject<SaleOrderModel>
                    {
                        Code = 404,
                        Message = "Cannot found",
                        IsSuccess = false
                    };
                }
                SaleOrderModel detailModel = _mapper.Map<SaleOrderModel>(baseModel.Where(x => x.OrderInformation != null).Select(x => x.OrderInformation).FirstOrDefault());
                detailModel.OrderItems = new List<SO_OrderItems>();

                foreach (var item in baseModel.Where(x => x.OrderItem != null && !x.OrderItem.IsDeleted).Select(x => x.OrderItem).ToList())
                {
                    item.UnitPriceAfterTax = item.UnitPriceAfterTax ?? 0;
                    item.UnitPriceBeforeTax = item.UnitPriceBeforeTax ?? 0;
                    detailModel.OrderItems.Add(item);
                }


                var sumpickingDetail = await _sumPickingListDetailRepository
                    .GetAllQueryable(null, null, null, _schemaName)
                    .Where(x => x.OrderRefNumber == detailModel.OrderRefNumber)
                    .FirstOrDefaultAsync();

                if (sumpickingDetail != null)
                {
                    var sumpicking = await _sumPickingListHeaderRepository
                        .GetAllQueryable(null, null, null, _schemaName)
                        .Where(x => x.SumPickingRefNumber == sumpickingDetail.SumPickingRefNumber)
                        .FirstOrDefaultAsync();

                    if (sumpicking != null)
                    {
                        detailModel.SumpickingRefNumber = sumpicking.SumPickingRefNumber;
                        detailModel.Shipper = sumpicking.DriverCode;
                    }
                }

                return new ResultModelWithObject<SaleOrderModel>
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "OK",
                    Data = detailModel
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new ResultModelWithObject<SaleOrderModel>
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }

        public async Task<ResultModelWithObject<ListSOModel>> SearchSO(SaleOrderSearchParamsModel parameters, string token = null, bool dispose = false)
        {
            try
            {
                var res = (from header in _orderInformationsRepository.GetAllQueryable(null, null, null, _schemaName).AsNoTracking()
                           join detail in _orderItemsRepository.GetAllQueryable(null, null, null, _schemaName).AsNoTracking() on header.OrderRefNumber equals detail.OrderRefNumber into data
                           from detail in data.DefaultIfEmpty()
                           where (parameters.OrderRefNumbers != null && parameters.OrderRefNumbers.Count > 0 ? parameters.OrderRefNumbers.Contains(header.OrderRefNumber) : true) &&
                           (parameters.ListDistributor != null && parameters.ListDistributor.Count > 0 ? parameters.ListDistributor.Contains(header.DistributorCode) : parameters.DistributorCode != null ? header.DistributorCode == parameters.DistributorCode : true) &&
                           (parameters.UpdatedDate.HasValue ? header.UpdatedDate.HasValue && header.UpdatedDate.Value.Date == parameters.UpdatedDate.Value.Date : true) &&
                           (parameters.StatusFilter != null && parameters.StatusFilter.Count > 0 ? parameters.StatusFilter.Contains(header.Status) : true) &&
                           (parameters.OrderDate.HasValue ? parameters.OrderDate.Value.Date == header.OrderDate.Date : true) &&
                           (parameters.FromDate.HasValue ? header.OrderDate.Date >= parameters.FromDate.Value.Date : true) &&
                           (parameters.FromDate.HasValue ? header.OrderDate.Date <= parameters.ToDate.Value.Date : true) &&
                           !header.IsDeleted
                           select new SaleOrderBaseModel
                           {
                               OrderInformation = header,
                               OrderItem = detail
                           }).AsNoTracking();


                if (parameters.Filters != null && parameters.Filters.Count > 0)
                {
                    res = await MappingQuerySO(res, parameters.Filters);
                }


                if (!string.IsNullOrWhiteSpace(parameters.SearchValue))
                {
                    res = res.Where(x =>
                        (!string.IsNullOrWhiteSpace(x.OrderInformation.CustomerId) && x.OrderInformation.CustomerId.ToLower().Trim().Contains(parameters.SearchValue.ToLower().Trim())) ||
                        (!string.IsNullOrWhiteSpace(x.OrderInformation.OrderRefNumber) && x.OrderInformation.OrderRefNumber.ToLower().Trim().Contains(parameters.SearchValue.ToLower().Trim())) ||
                        (!string.IsNullOrWhiteSpace(x.OrderInformation.CustomerName) && x.OrderInformation.CustomerName.ToLower().Trim().Contains(parameters.SearchValue.ToLower().Trim())) ||
                        (!string.IsNullOrWhiteSpace(x.OrderInformation.ReferenceRefNbr) && x.OrderInformation.ReferenceRefNbr.ToLower().Trim().Contains(parameters.SearchValue.ToLower().Trim()))
                    );
                }

                // var res = res;

                if (parameters.OutletFilters != null && parameters.OutletFilters.Count > 0)
                {
                    List<string> SelectedItems = new List<string>();
                    foreach (var item in parameters.OutletFilters)
                    {
                        var result = res.Where(x => x.OrderInformation != null && !string.IsNullOrWhiteSpace(x.OrderInformation.CustomerId) && x.OrderInformation.CustomerId == item.CustomerCode && !string.IsNullOrWhiteSpace(x.OrderInformation.CustomerShiptoID) && x.OrderInformation.CustomerShiptoID == item.ShiptoCode).Select(x => x.OrderInformation.OrderRefNumber).Distinct().ToList();
                        if (result != null && result.Count > 0)
                        {
                            SelectedItems.AddRange(result);
                        }
                    }
                    res = res.Where(x => SelectedItems.Contains(x.OrderInformation.OrderRefNumber));
                }

                var listSO = _mapper.Map<List<SaleOrderModel>>(res.Where(x => x.OrderInformation != null).Select(x => x.OrderInformation).Distinct().OrderByDescending(x => x.OrderRefNumber).ToList());
                foreach (var item in listSO)
                {
                    if (parameters.IncludeSumpicking)
                    {
                        var sumpickingDetails = await _sumPickingListDetailRepository
                            .GetAllQueryable(x => x.OrderRefNumber == item.OrderRefNumber, null, null, _schemaName)
                            .ToListAsync();

                        foreach (var sumpickingDetail in sumpickingDetails)
                        {
                            var sumpicking = await _sumPickingListHeaderRepository
                                .GetAllQueryable(x => x.SumPickingRefNumber == sumpickingDetail.SumPickingRefNumber &&
                                !x.IsDeleted && x.Status.ToLower().Trim() == SO_SaleOrderStatusConst.CONFIRM.ToLower().Trim(),
                                null, null, _schemaName)
                                .FirstOrDefaultAsync();

                            if (sumpicking != null)
                            {
                                item.SumpickingRefNumber = sumpicking.SumPickingRefNumber;
                                item.Shipper = sumpicking.DriverCode;
                                break;
                            }
                        }

                    }
                    item.OrderItems = res.Where(x => x.OrderItem != null).Select(x => x.OrderItem).Where(x => x.OrderRefNumber == item.OrderRefNumber).ToList();
                }

                if (!string.IsNullOrWhiteSpace(parameters.SumaryPickingNbr))
                {
                    listSO = listSO.Where(x => !string.IsNullOrWhiteSpace(x.SumpickingRefNumber) && x.SumpickingRefNumber == parameters.SumaryPickingNbr).ToList();
                }
                if (!string.IsNullOrWhiteSpace(parameters.Shipper))
                {
                    listSO = listSO.Where(x => !string.IsNullOrWhiteSpace(x.Shipper) && x.Shipper == parameters.Shipper).ToList();
                }

                if (!string.IsNullOrWhiteSpace(parameters.DeliveryProcessSearchValue))
                {
                    listSO = listSO.Where(x =>
                        !string.IsNullOrWhiteSpace(x.OrderRefNumber) && x.OrderRefNumber.ToLower().Trim().Contains(parameters.DeliveryProcessSearchValue.ToLower().Trim()) ||
                        !string.IsNullOrWhiteSpace(x.SalesRepID) && x.SalesRepID.ToLower().Trim().Contains(parameters.DeliveryProcessSearchValue.ToLower().Trim()) ||
                        !string.IsNullOrWhiteSpace(x.Shipper) && x.Shipper.ToLower().Trim().Contains(parameters.DeliveryProcessSearchValue.ToLower().Trim()) ||
                        !string.IsNullOrWhiteSpace(x.SumpickingRefNumber) && x.SumpickingRefNumber.ToLower().Trim().Contains(parameters.DeliveryProcessSearchValue.ToLower().Trim())
                    ).ToList();
                }

                if (!string.IsNullOrWhiteSpace(parameters.Address))
                {
                    listSO = listSO.Where(x => x.CustomerAddress.Trim().ToLower().Contains(parameters.Address.Trim().ToLower())).ToList();
                }

                if (!string.IsNullOrWhiteSpace(parameters.PickingListSearchValue))
                {
                    listSO = listSO.Where(x =>
                        !string.IsNullOrWhiteSpace(x.OrderRefNumber) && x.OrderRefNumber.ToLower().Trim().Contains(parameters.PickingListSearchValue.ToLower().Trim()) ||
                        !string.IsNullOrWhiteSpace(x.CustomerId) && x.CustomerId.ToLower().Trim().Contains(parameters.PickingListSearchValue.ToLower().Trim()) ||
                        !string.IsNullOrWhiteSpace(x.CustomerName) && x.CustomerName.ToLower().Trim().Contains(parameters.PickingListSearchValue.ToLower().Trim())
                    ).ToList();
                }

                if (parameters.ExcludeBaseLine)
                {
                    string allowCancelDate = await _clientService.CommonRequestAsync<string>(SystemUrlCode.SystemAdminAPI, $"Temp/GetDateallowCancel/{DateTime.Now.ToString("yyyy-MM-dd")}", Method.GET, token, null);
                    listSO = listSO.Where(x => x.OrderDate.Date >= DateTime.Parse(allowCancelDate).Date).ToList();

                }


                if (parameters.IsDropdown)
                {
                    var page1 = PagedList<SaleOrderModel>.ToPagedList(listSO, 0, listSO.Count());

                    var reponse = new ListSOModel { Items = listSO.ToList() };
                    return new ResultModelWithObject<ListSOModel>
                    {
                        IsSuccess = true,
                        Code = 200,
                        Message = "Success",
                        Data = reponse
                    };
                }

                var poTempPagged = PagedList<SaleOrderModel>.ToPagedList(listSO, (parameters.PageNumber - 1) * parameters.PageSize, parameters.PageSize);
                var repsonse = new ListSOModel { Items = poTempPagged, MetaData = poTempPagged.MetaData };

                if (dispose)
                {
                    _orderInformationsRepository.Dispose(_schemaName);
                }
                //return metadata
                return new ResultModelWithObject<ListSOModel>
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Success",
                    Data = repsonse
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new ResultModelWithObject<ListSOModel>
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }

        public async Task<ResultModelWithObject<ListSOModel>> SearchSOv2(SaleOrderSearchParamsModel parameters, string token = null, bool dispose = false, bool isInternal = false)
        {
            try
            {
                //var res = _orderInformationsRepository.GetAllQueryable(x =>
                //    (parameters.OrderRefNumbers != null && parameters.OrderRefNumbers.Count > 0 ? parameters.OrderRefNumbers.Contains(x.OrderRefNumber) : true) &&
                //    (parameters.ListDistributor != null && parameters.ListDistributor.Count > 0 ? parameters.ListDistributor.Contains(x.DistributorCode) : parameters.DistributorCode != null ? x.DistributorCode == parameters.DistributorCode : true) &&
                //    (parameters.UpdatedDate.HasValue ? x.UpdatedDate.HasValue && x.UpdatedDate.Value.Date == parameters.UpdatedDate.Value.Date : true) &&
                //    (parameters.StatusFilter != null && parameters.StatusFilter.Count > 0 ? parameters.StatusFilter.Contains(x.Status) : true) &&
                //    (parameters.OrderDate.HasValue ? parameters.OrderDate.Value.Date == x.OrderDate.Date : true) &&
                //    (parameters.FromDate.HasValue ? x.OrderDate.Date >= parameters.FromDate.Value.Date : true) &&
                //    (parameters.FromDate.HasValue ? x.OrderDate.Date <= parameters.ToDate.Value.Date : true) &&
                //    (!string.IsNullOrWhiteSpace(parameters.Address) ? x.CustomerAddress.Trim().ToLower().Contains(parameters.Address.Trim().ToLower()) : true) &&
                //    !x.IsDeleted
                //);
                DateTime currentDate = DateTime.Now;
                if (!parameters.IsDropdown && !parameters.FromDate.HasValue && !parameters.ToDate.HasValue)
                {
                    parameters.FromDate = currentDate.AddDays(-60).StartOfDate();
                    parameters.ToDate = currentDate.EndOfDate();
                }

                IQueryable<SO_OrderInformations> res;
                if (isInternal)
                {
                    res = _orderInformationsRepository.GetAllQueryable(x =>
                        (parameters.OrderRefNumbers != null && parameters.OrderRefNumbers.Any() ? parameters.OrderRefNumbers.Contains(x.OrderRefNumber) : true) &&
                        (parameters.StatusFilter != null && parameters.StatusFilter.Any() ? parameters.StatusFilter.Contains(x.Status) : true) &&
                        (parameters.DistributorCode != null ? x.DistributorCode == parameters.DistributorCode : true) &&
                        !x.IsDeleted, null, null, _schemaName
                    ).AsNoTracking();
                }
                else
                {
                    res = _orderInformationsRepository.GetAllQueryable(x =>
                        (parameters.OrderRefNumbers != null && parameters.OrderRefNumbers.Any() ? parameters.OrderRefNumbers.Contains(x.OrderRefNumber) : true) &&
                        (parameters.ListDistributor != null && parameters.ListDistributor.Any() ? parameters.ListDistributor.Contains(x.DistributorCode) : parameters.DistributorCode != null ? x.DistributorCode == parameters.DistributorCode : true) &&
                        (!parameters.UpdatedDate.HasValue || (x.UpdatedDate != null && x.UpdatedDate.Value.Date == parameters.UpdatedDate.Value.Date)) &&
                        (parameters.StatusFilter != null && parameters.StatusFilter.Any() ? parameters.StatusFilter.Contains(x.Status) : true) &&
                        (!parameters.OrderDate.HasValue || x.OrderDate.Date == parameters.OrderDate.Value.Date) &&
                        (!parameters.FromDate.HasValue || x.OrderDate.Date >= parameters.FromDate.Value.Date) &&
                        (!parameters.ToDate.HasValue || x.OrderDate.Date <= parameters.ToDate.Value.Date) &&
                        !x.IsDeleted, null, null, _schemaName
                    );
                }

                //if has filter expression 
                if (parameters.Filter != null && parameters.Filter.Trim() != string.Empty && parameters.Filter.Trim() != "NA_EMPTY")
                {
                    var parameter = Expression.Parameter(typeof(SO_OrderInformations), "s");
                    var lambda = DynamicExpressionParser.ParseLambda(new[] { parameter }, typeof(bool), parameters.Filter);
                    res = res.Where((Func<SO_OrderInformations, bool>)lambda.Compile()).AsQueryable();
                }


                if (parameters.ExcludeBaseLine)
                {
                    string allowCancelDate = await _clientService.CommonRequestAsync<string>(SystemUrlCode.SystemAdminAPI, $"Temp/GetDateallowCancel/{DateTime.Now.ToString("yyyy-MM-dd")}", Method.GET, token, null);
                    res = res.Where(x => x.OrderDate.Date >= DateTime.Parse(allowCancelDate).Date);
                }

                if (!string.IsNullOrWhiteSpace(parameters.Address))
                {
                    res = res.Where(x =>
                        (!string.IsNullOrWhiteSpace(x.CustomerAddress) && x.CustomerAddress.ToLower().Trim().Contains(parameters.Address.ToLower().Trim())));
                }


                if (parameters.Filters != null && parameters.Filters.Count > 0)
                {
                    res = await MappingQuerySOv2(res, parameters.Filters);
                }

                if (!string.IsNullOrWhiteSpace(parameters.SearchValue))
                {
                    res = res.Where(x =>
                        (!string.IsNullOrWhiteSpace(x.CustomerId) && x.CustomerId.ToLower().Trim().Contains(parameters.SearchValue.ToLower().Trim())) ||
                        (!string.IsNullOrWhiteSpace(x.OrderRefNumber) && x.OrderRefNumber.ToLower().Trim().Contains(parameters.SearchValue.ToLower().Trim())) ||
                        (!string.IsNullOrWhiteSpace(x.CustomerName) && x.CustomerName.ToLower().Trim().Contains(parameters.SearchValue.ToLower().Trim())) ||
                        (!string.IsNullOrWhiteSpace(x.ReferenceRefNbr) && x.ReferenceRefNbr.ToLower().Trim().Contains(parameters.SearchValue.ToLower().Trim()))
                    );
                }

                if (!string.IsNullOrWhiteSpace(parameters.PickingListSearchValue))
                {
                    res = res.Where(x =>
                        !string.IsNullOrWhiteSpace(x.OrderRefNumber) && x.OrderRefNumber.ToLower().Trim().Contains(parameters.PickingListSearchValue.ToLower().Trim()) ||
                        !string.IsNullOrWhiteSpace(x.CustomerId) && x.CustomerId.ToLower().Trim().Contains(parameters.PickingListSearchValue.ToLower().Trim()) ||
                        !string.IsNullOrWhiteSpace(x.CustomerName) && x.CustomerName.ToLower().Trim().Contains(parameters.PickingListSearchValue.ToLower().Trim())
                    );
                }

                res = res.AsSplitQuery().AsQueryable();

                if (parameters.OutletFilters != null && parameters.OutletFilters.Count > 0)
                {
                    List<string> SelectedItems = new List<string>();
                    foreach (var item in parameters.OutletFilters)
                    {
                        var result = res.Where(x => x != null && !string.IsNullOrWhiteSpace(x.CustomerId) && x.CustomerId == item.CustomerCode && !string.IsNullOrWhiteSpace(x.CustomerShiptoID) && x.CustomerShiptoID == item.ShiptoCode).Select(x => x.OrderRefNumber).Distinct().ToList();
                        if (result != null && result.Count > 0)
                        {
                            SelectedItems.AddRange(result);
                        }
                    }
                    res = res.Where(x => SelectedItems.Contains(x.OrderRefNumber));
                }
                res = res.OrderByDescending(x => x.OrderDate).ThenBy(x => x.SalesRepName);
                var listSO = new List<SaleOrderModel>();

                var reponse = new ListSOModel();

                if (parameters.IsDropdown)
                {
                    listSO = _mapper.Map<List<SaleOrderModel>>(res.Where(x => x != null).ToList());
                    if (parameters.IncludeItem)
                    {
                        foreach (var so in listSO)
                        {
                            so.OrderItems = await _orderItemsRepository.GetAllQueryable(x => x.OrderRefNumber == so.OrderRefNumber, null, null, _schemaName).ToListAsync();
                        }
                    }
                    reponse = new ListSOModel { Items = listSO.ToList() };
                }
                else
                {
                    //var poTempPagged = PagedList<SO_OrderInformations>.ToPagedListQueryAble(res, (parameters.PageNumber - 1) * parameters.PageSize, parameters.PageSize);
                    listSO = _mapper.Map<List<SaleOrderModel>>(res.Where(x => x != null).ToList()).Where(x => x != null).ToList();
                    foreach (var so in listSO)
                    {
                        if (parameters.IncludeSumpicking)
                        {
                            var sumpickingDetails = await _sumPickingListDetailRepository.GetAllQueryable(x => x.OrderRefNumber == so.OrderRefNumber, null, null, _schemaName).ToListAsync();
                            foreach (var sumpickingDetail in sumpickingDetails)
                            {
                                var sumpicking = await _sumPickingListHeaderRepository.GetAllQueryable(x => x.SumPickingRefNumber == sumpickingDetail.SumPickingRefNumber &&
                                !x.IsDeleted &&
                                x.Status.ToLower().Trim() == SO_SaleOrderStatusConst.CONFIRM.ToLower().Trim(),
                                null, null, _schemaName).FirstOrDefaultAsync();

                                if (sumpicking != null)
                                {
                                    so.SumpickingRefNumber = sumpicking.SumPickingRefNumber;
                                    so.Shipper = sumpicking.DriverCode;
                                    break;
                                }
                            }
                        }

                        if (parameters.IncludeItem)
                        {
                            so.OrderItems = await _orderItemsRepository.GetAllQueryable(x => x.OrderRefNumber == so.OrderRefNumber, null, null, _schemaName).ToListAsync();
                        }
                        else
                        {
                            so.OrderItems = new();
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(parameters.SumaryPickingNbr))
                    {
                        listSO = listSO.Where(x => !string.IsNullOrWhiteSpace(x.SumpickingRefNumber) && x.SumpickingRefNumber == parameters.SumaryPickingNbr).ToList();
                    }
                    var poTempPagged = PagedList<SaleOrderModel>.ToPagedList(listSO.ToList(), (parameters.PageNumber - 1) * parameters.PageSize, parameters.PageSize);
                    reponse = new ListSOModel { Items = poTempPagged, MetaData = poTempPagged.MetaData };
                }


                if (dispose)
                {
                    _orderInformationsRepository.Dispose(_schemaName);
                }
                //return metadata
                return new ResultModelWithObject<ListSOModel>
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Success",
                    Data = reponse
                };


                // foreach (var item in listSO)
                // {
                //     if (parameters.IncludeSumpicking)
                //     {
                //         var sumpickingDetails = await _sumPickingListDetailRepository.GetAllQueryable(x => x.OrderRefNumber == item.OrderRefNumber).ToListAsync();
                //         foreach (var sumpickingDetail in sumpickingDetails)
                //         {
                //             var sumpicking = await _sumPickingListHeaderRepository.GetAllQueryable(x => x.SumPickingRefNumber == sumpickingDetail.SumPickingRefNumber && !x.IsDeleted && x.Status.ToLower().Trim() == SO_SaleOrderStatusConst.CONFIRM.ToLower().Trim()).FirstOrDefaultAsync();
                //             if (sumpicking != null)
                //             {
                //                 item.SumpickingRefNumber = sumpicking.SumPickingRefNumber;
                //                 item.Shipper = sumpicking.DriverCode;
                //                 break;
                //             }
                //         }

                //     }
                //     item.OrderItems = res.Where(x => x.OrderItem != null).Select(x => x.OrderItem).Where(x => x.OrderRefNumber == item.OrderRefNumber).ToList();
                // }

                // if (!string.IsNullOrWhiteSpace(parameters.SumaryPickingNbr))
                // {
                //     listSO = listSO.Where(x => !string.IsNullOrWhiteSpace(x.SumpickingRefNumber) && x.SumpickingRefNumber == parameters.SumaryPickingNbr);
                // }
                // if (!string.IsNullOrWhiteSpace(parameters.Shipper))
                // {
                //     listSO = listSO.Where(x => !string.IsNullOrWhiteSpace(x.Shipper) && x.Shipper == parameters.Shipper);
                // }

                // if (!string.IsNullOrWhiteSpace(parameters.DeliveryProcessSearchValue))
                // {
                //     listSO = listSO.Where(x =>
                //         !string.IsNullOrWhiteSpace(x.OrderRefNumber) && x.OrderRefNumber.ToLower().Trim().Contains(parameters.DeliveryProcessSearchValue.ToLower().Trim()) ||
                //         !string.IsNullOrWhiteSpace(x.SalesRepID) && x.SalesRepID.ToLower().Trim().Contains(parameters.DeliveryProcessSearchValue.ToLower().Trim()) ||
                //         !string.IsNullOrWhiteSpace(x.Shipper) && x.Shipper.ToLower().Trim().Contains(parameters.DeliveryProcessSearchValue.ToLower().Trim()) ||
                //         !string.IsNullOrWhiteSpace(x.SumpickingRefNumber) && x.SumpickingRefNumber.ToLower().Trim().Contains(parameters.DeliveryProcessSearchValue.ToLower().Trim())
                //     );
                // }


            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new ResultModelWithObject<ListSOModel>
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }

        public async Task<ResultModelWithObject<SearchListModel<SO_OrderItems>>> GetSuggestionItems(SaleOrderSearchParamsModel parameters, string token, bool buyed = true)
        {
            try
            {
                var searchData = await CommonGetAllQueryable(new SaleOrderSearchParamsModel
                {
                    DistributorCode = parameters.DistributorCode,
                    Filters = new List<GenericFilter> { new GenericFilter { Property = "CustomerId", Values = new List<string> { parameters.CustomerCode.ToLower().Trim() } } },
                    IsDropdown = true,
                });

                var orderItems = searchData
                    .Select(x => x.OrderItem)
                    .Where(x => x != null && !x.IsDeleted &&
                           x.ItemId != Guid.Empty &&
                           !(x.IsKit && x.ItemCode != null) &&
                            string.IsNullOrWhiteSpace(x.PromotionCode))
                            .Select(x => new SO_OrderItems
                            {
                                InventoryID = x.InventoryID,
                                LocationID = x.LocationID,
                                ItemId = x.ItemId,
                                ItemCode = x.ItemCode,
                                ItemDescription = x.ItemDescription,
                                UOM = x.UOM,
                                UnitRate = x.UnitRate,
                                VatValue = x.VatValue,
                                VATCode = x.VATCode,
                                InventoryAttibute1 = x.InventoryAttibute1,
                                InventoryAttibute2 = x.InventoryAttibute2,
                                InventoryAttibute3 = x.InventoryAttibute3,
                                InventoryAttibute4 = x.InventoryAttibute4,
                                InventoryAttibute5 = x.InventoryAttibute5,
                                InventoryAttibute6 = x.InventoryAttibute6,
                                InventoryAttibute7 = x.InventoryAttibute7,
                                InventoryAttibute8 = x.InventoryAttibute8,
                                InventoryAttibute9 = x.InventoryAttibute9,
                                InventoryAttibute10 = x.InventoryAttibute10,
                                ItemGroupCode = x.ItemGroupCode,
                                ItemPoint = x.ItemPoint,
                                BaseUnit = x.BaseUnit,
                                SalesUnit = x.SalesUnit,
                                PurchaseUnit = x.PurchaseUnit,
                                VatId = x.VatId,
                                Hierarchy = x.Hierarchy,
                                ItemShortName = x.ItemShortName,
                                IsKit = x.IsKit,
                                KitId = x.KitId,
                                KitKey = x.KitKey,
                            }).OrderBy(x => x.ItemCode).ToList();

                var response = new SearchListModel<SO_OrderItems>();
                if (!buyed)
                {
                    //call api transaction
                    _clientINV.Authenticator = new JwtAuthenticator($"{token}");
                    var request = new RestRequest($"AllocationItem/Search", Method.POST, DataFormat.Json);
                    SearchAllocationItemWithDistributorModel allocationItemSearchData = new()
                    {
                        Filter = $"s.Available > 0 ",
                        DistributorCode = parameters.DistributorCode,
                        // WareHouseCode = parameters.WareHouseCode,
                        IsDropdown = true
                    };
                    if (!string.IsNullOrWhiteSpace(parameters.WareHouseCode))
                    {
                        allocationItemSearchData.Filter += $" && !string.IsNullOrWhiteSpace(s.WareHouseCode) && s.WareHouseCode.Equals(\"{parameters.WareHouseCode}\")";
                    }
                    request.AddJsonBody(allocationItemSearchData);
                    // Add Header
                    request.AddHeader(OD_Constant.KeyHeader, _distributorCode);
                    var result = _clientINV.Execute(request);

                    var resultData = JsonConvert.DeserializeObject<ResultModelWithObject<SearchListModel<INV_AllocationDetailModel>>>(JsonConvert.DeserializeObject(result.Content).ToString());
                    if (!resultData.IsSuccess || resultData.Data == null || resultData.Data.Items == null || resultData.Data.Items.IsEmpty())
                    {
                        orderItems = new List<SO_OrderItems>();
                    }
                    var ExcludedItems = orderItems.Select(x => x.ItemCode).ToList();
                    resultData.Data.Items = resultData.Data.Items.Where(x => x != null && !ExcludedItems.Contains(x.ItemCode)).GroupBy(x => x.ItemCode).Select(x => x.First()).ToList();
                    //Filter
                    if (parameters.Filter != null && parameters.Filter.Trim() != string.Empty && parameters.Filter.Trim() != "NA_EMPTY")
                    {
                        //var optionsAssembly = ScriptOptions.Default.AddReferences(typeof(INV_AllocationDetailModel).Assembly);
                        //var filterExpressionTemp = CSharpScript.EvaluateAsync<Func<INV_AllocationDetailModel, bool>>(($"s=> {parameters.Filter}"), optionsAssembly);
                        //Func<INV_AllocationDetailModel, bool> filterExpression = filterExpressionTemp.Result;
                        //var checkCondition = resultData.Data.Items.Where(filterExpression);
                        //resultData.Data.Items = checkCondition.ToList();

                        var parameter = Expression.Parameter(typeof(INV_AllocationDetailModel), "s");
                        var lambda = DynamicExpressionParser.ParseLambda(new[] { parameter }, typeof(bool), parameters.Filter);
                        resultData.Data.Items = resultData.Data.Items.Where((Func<INV_AllocationDetailModel, bool>)lambda.Compile()).AsQueryable().ToList();
                    }


                    if (!parameters.IsDropdown)
                    {
                        var tempData = PagedList<INV_AllocationDetailModel>.ToPagedList(resultData.Data.Items, (parameters.PageNumber - 1) * parameters.PageSize, parameters.PageSize);
                        resultData.Data.Items = tempData;
                        response.MetaData = tempData.MetaData;
                    }
                    List<SO_OrderItems> itemsList = new();
                    foreach (var item in resultData.Data.Items)
                    {
                        token = token.Split(" ").Last();
                        //call api transaction
                        _client = new RestClient(CommonData.SystemUrl.Where(x => x.Code == CommonData.SystemUrlCode.SystemAdminAPI).Select(x => x.Url).FirstOrDefault());
                        _client.Authenticator = new JwtAuthenticator($"Rdos {token}");
                        var requestItemsDetail = new RestRequest($"InventoryItem/GetInventoryItemById/{item.ItemId}", Method.GET, DataFormat.Json);
                        // request.AddJsonBody(availableAllocationItemQuery);
                        var resultItemsDetail = _client.Execute(requestItemsDetail);
                        var ItemDetail = JsonConvert.DeserializeObject<ItemMng_IventoryItemModel>(JsonConvert.DeserializeObject(resultItemsDetail.Content).ToString());
                        var saleUnit = ItemDetail.InventoryItem.SalesUnit;
                        var saleUom = ItemDetail.UomConversion.Where(x => x.FromUnit == saleUnit).Select(x => x.FromUnitName).FirstOrDefault();
                        var unitRate = ItemDetail.UomConversion.Where(x => x.FromUnit == saleUnit).Select(x => x.ConversionFactor).FirstOrDefault();
                        if ((ItemDetail.InventoryItem.Status.Trim().ToLower() == "inactive" || ItemDetail.InventoryItem.Status.Trim().ToLower() == "0") || !ItemDetail.InventoryItem.OrderItem)
                        {
                            continue;
                        }

                        //call api Detail UOM
                        _client = new RestClient(CommonData.SystemUrl.Where(x => x.Code == CommonData.SystemUrlCode.SystemAdminAPI).Select(x => x.Url).FirstOrDefault());
                        token = token.Split(" ").Last();
                        _client.Authenticator = new JwtAuthenticator($"Rdos {token}");
                        var requestVatDetail = new RestRequest($"Vat/GetVatById/{ItemDetail.InventoryItem.Vat}", Method.GET, DataFormat.Json);
                        var resultVatDetail = _client.Execute(requestVatDetail);
                        var vatDetail = JsonConvert.DeserializeObject<VATDetail>(JsonConvert.DeserializeObject(resultVatDetail.Content).ToString());

                        itemsList.Add(new SO_OrderItems
                        {
                            InventoryID = item.ItemCode,
                            LocationID = item.LocationCode,
                            ItemId = ItemDetail.InventoryItem.Id,
                            ItemCode = item.ItemCode,
                            ItemDescription = item.ItemDescription,
                            UOM = saleUom,
                            UnitRate = (int)unitRate,
                            ItemGroupCode = item.ItemGroupCode,
                            ItemPoint = ItemDetail.InventoryItem.Point,
                            BaseUnit = ItemDetail.InventoryItem.BaseUnit,
                            SalesUnit = ItemDetail.InventoryItem.SalesUnit,
                            PurchaseUnit = ItemDetail.InventoryItem.PurchaseUnit,
                            VatId = ItemDetail.InventoryItem.Vat,
                            Hierarchy = ItemDetail.InventoryItem.Hierarchy,
                            ItemShortName = ItemDetail.InventoryItem.ShortName,
                            VatValue = vatDetail.VatValues,
                            VATCode = vatDetail.VatId
                        });
                    }
                    response.Items = itemsList.OrderBy(x => x.ItemCode).ToList();
                }
                else
                {
                    orderItems = orderItems.GroupBy(x => x.ItemCode).Select(x => x.OrderByDescending(x => x.OrderRefNumber).First()).OrderBy(x => x.ItemCode).ToList();

                    if (parameters.IsDropdown)
                    {
                        response = new SearchListModel<SO_OrderItems> { Items = orderItems };
                        // return new ResultModelWithObject<SearchListModel<SO_OrderItems>>
                        // {
                        //     IsSuccess = true,
                        //     Code = 200,
                        //     Message = "Success",
                        //     Data = res
                        // };
                    }
                    else
                    {
                        var poTempPagged = PagedList<SO_OrderItems>.ToPagedList(orderItems, (parameters.PageNumber - 1) * parameters.PageSize, parameters.PageSize);
                        response = new SearchListModel<SO_OrderItems> { Items = poTempPagged, MetaData = poTempPagged.MetaData };
                    }

                }
                _orderInformationsRepository.Dispose(_schemaName);
                return new ResultModelWithObject<SearchListModel<SO_OrderItems>>
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Success",
                    Data = response
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new ResultModelWithObject<SearchListModel<SO_OrderItems>>
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }
        #endregion

        #region Report
        public async Task<HODataModel> HOGetDetail(string userName, string token)
        {
            HODataModel result = new()
            {
                IsHO = false,
                Distributors = new List<string>()
            };

            ResultModelWithObject<HOResultModel> employeeResult = await _clientService.CommonRequestAsync<ResultModelWithObject<HOResultModel>>(SystemUrlCode.SystemAdminAPI, $"PrincipleEmployee/Ex_GetEmployeeByUserName/{userName}", Method.GET, token, null);

            if (employeeResult.IsSuccess && employeeResult.Data != null)
            {
                if (!string.IsNullOrEmpty(employeeResult.Data.AccountName) && !string.IsNullOrEmpty(employeeResult.Data.EmployeeCode))
                {
                    result.IsHO = true;
                    ResultModelWithObject<EmployeeHoInforResultModel> requestDistributorList = await _clientService.CommonRequestAsync<ResultModelWithObject<EmployeeHoInforResultModel>>(SystemUrlCode.SalesOrgAPI, $"SalesOrganization/GetEmployeeInformation/{employeeResult.Data.EmployeeCode}", Method.GET, token, null);
                    if (requestDistributorList.IsSuccess && requestDistributorList.Data != null)
                    {
                        foreach (var item in requestDistributorList.Data.ListDistributor)
                        {
                            result.Distributors.Add(item.DistributorCode);
                        }
                    }
                }
            }
            return result;
        }

        //SO.RP01
        public async Task<BaseResultModel> ReportTrackingOrder(ReportTrackingOrderQueryModel parameters, string userName, string token)
        {
            if (parameters.IsOver3Month()) return parameters.IsOver3MonthResult();
            try
            {
                if (string.IsNullOrWhiteSpace(parameters.DistributorCode))
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Message = "DistributorCode cannot null",
                        Code = 400
                    };
                }

                // Status
                List<string> listStatusReport = new List<string>{
                    SO_SaleOrderStatusConst.OPEN,
                    SO_SaleOrderStatusConst.SHIPPING,
                    SO_SaleOrderStatusConst.WAITNGSHIPPING,
                    SO_SaleOrderStatusConst.DELIVERED,
                    SO_SaleOrderStatusConst.PARTIALDELIVERED,
                    SO_SaleOrderStatusConst.FAILED,
                    SO_SaleOrderStatusConst.CONFIRM,
                    SO_SaleOrderStatusConst.COMPLETE_DRAFT
                };

                // Chuyển list status thành đầu vào của func
                string orderStatus = string.Join(",", listStatusReport).Trim();

                string _query;
                if (!string.IsNullOrWhiteSpace(parameters.SaleRepId))
                {
                    _query = $@"SELECT * from ODReportTrackingOrderPaging('{parameters.DistributorCode}', '{parameters.SaleRepId}', '{orderStatus}', '{parameters.FromDate.Value.Date.StartOfDate().ToString(FormatConstants.DATETIME_REQ_DB)}', '{parameters.ToDate.Value.Date.EndOfDate().ToString(FormatConstants.DATETIME_REQ_DB)}', {parameters.PageNumber}, {parameters.PageSize}, '{_schemaName}')";
                }
                else
                {
                    _query = $@"SELECT * from ODReportTrackingOrderPaging('{parameters.DistributorCode}', null, '{orderStatus}', '{parameters.FromDate.Value.Date.StartOfDate().ToString(FormatConstants.DATETIME_REQ_DB)}', '{parameters.ToDate.Value.Date.EndOfDate().ToString(FormatConstants.DATETIME_REQ_DB)}', {parameters.PageNumber}, {parameters.PageSize}, '{_schemaName}')";
                }

                List<FnReportTrackingOrderModel> listDataCollect = await _dataContext.FnReportTrackingOrderModels.FromSqlRaw(_query).AsNoTracking().ToListAsync();

                ReportTrackingOrderModel dataRes = new();
                dataRes.DistributorCode = parameters.DistributorCode;

                ListReportTrackingOrderResultModel listReturn = new();


                foreach (var item in listDataCollect)
                {
                    ReportTrackingOrderResultModel detailRes = new();
                    detailRes.OrderRefNumber = item.OrderRefNumber;
                    detailRes.SalesRepID = item.SalesRepID;
                    detailRes.CustomerId = item.CustomerId;
                    detailRes.CustomerName = item.CustomerName;
                    detailRes.OrderDate = item.OrderDate;
                    detailRes.Amount = item.Amount;
                    detailRes.Status = item.Status;
                    detailRes.TotalBaseQty = item.TotalBaseQty;
                    listReturn.Items.Add(detailRes);
                }

                FnReportTrackingOrderModel detail = listDataCollect.FirstOrDefault();

                MetaData metaData = new MetaData
                {
                    TotalCount = detail != null ? detail.TotalCount : 0,
                    PageSize = parameters.PageSize,
                    CurrentPage = parameters.PageNumber,
                    TotalPages = detail != null ? detail.TotalPages : 0
                };

                listReturn.MetaData = metaData;
                dataRes.OrderInformations = listReturn;

                _dataContext.Dispose();
                return new BaseResultModel
                {
                    Code = 200,
                    Data = dataRes,
                    IsSuccess = true,
                    Message = "OK"
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }

        //SO.RP02
        public async Task<BaseResultModel> ReportShippingStatus(ReportOrderShippingQueryModel parameters, string userName, string token)
        {
            if (parameters.IsOver3Month()) return parameters.IsOver3MonthResult();
            try
            {
                var hODataModel = await HOGetDetail(userName, token);
                List<string> listDistributor = new List<string>();

                if (!hODataModel.IsHO)
                {
                    listDistributor.Add(parameters.DistributorCode);
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(parameters.DistributorCode))
                    {
                        if (hODataModel.Distributors.Any(x => x != null && x == parameters.DistributorCode))
                        {
                            hODataModel.Distributors = new List<string>() { parameters.DistributorCode };
                        }
                    }

                    listDistributor.AddRange(hODataModel.Distributors);
                }

                string _listDistributor = string.Join(",", listDistributor).Trim();
                string _query;
                if (!string.IsNullOrWhiteSpace(parameters.ItemGroupCode))
                {
                    _query = $@"SELECT * FROM f_reportshippingstatus('{_listDistributor}', '{parameters.ItemGroupCode}', '{parameters.FromDate.Value.Date.StartOfDate().ToString(FormatConstants.DATETIME_REQ_DB)}', '{parameters.ToDate.Value.Date.EndOfDate().ToString(FormatConstants.DATETIME_REQ_DB)}', '{_schemaName}')";
                }
                else
                {
                    _query = $@"SELECT * FROM f_reportshippingstatus('{_listDistributor}', null, '{parameters.FromDate.Value.Date.StartOfDate().ToString(FormatConstants.DATETIME_REQ_DB)}', '{parameters.ToDate.Value.Date.EndOfDate().ToString(FormatConstants.DATETIME_REQ_DB)}', '{_schemaName}')";
                }
                
                List<ReportShippingStatusResultModel> dataCollect =(List<ReportShippingStatusResultModel>)_dapperRepositories.Query<ReportShippingStatusResultModel>(_query);
                List<ReportOrderShippingModel> listDataRes = new();

                List<ReportShippingStatusResultModel> listGroupDistributor = dataCollect.GroupBy(x => x.DistributorCode).Select(x => x.First()).ToList();
                foreach (var dis in listGroupDistributor)
                {
                    ReportOrderShippingModel dataRes = new();
                    dataRes.DistributorCode = dis.DistributorCode;
                    dataRes.OrderItems = new List<ReportOrderShippingDetail>();

                    List<ReportShippingStatusResultModel> listDataByDis = dataCollect.Where(x => x.DistributorCode == dis.DistributorCode).ToList();
                    foreach (var item in listDataByDis)
                    {
                        ReportOrderShippingDetail dataDetailRes = new();
                        dataDetailRes.InventoryID = item.InventoryID;
                        dataDetailRes.Description = item.ItemDescription;
                        dataDetailRes.SlthungOrder = item.SlthungOrder;
                        dataDetailRes.SllocOrder = item.SllocOrder;
                        dataDetailRes.SlchaiOrder = item.SlchaiOrder;
                        dataDetailRes.SlthungShipped = item.SlthungShipped;
                        dataDetailRes.SllocShipped = item.SllocShipped;
                        dataDetailRes.SlchaiShipped = item.SlchaiShipped;
                        dataRes.OrderItems.Add(dataDetailRes);
                    }

                    listDataRes.Add(dataRes);
                }

                _dataContext.Dispose();
                return new BaseResultModel
                {
                    Code = 200,
                    Data = listDataRes,
                    IsSuccess = true,
                    Message = "Successfully"
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }

        private async Task<ResultModelWithObject<ListSalesReportDetailModel>> GetListDataForSalesDetailModel(SalesReportQuery parameters)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(parameters.DistributorCode) ||
                    parameters.FromDate == null ||
                    parameters.ToDate == null)
                {
                    return new ResultModelWithObject<ListSalesReportDetailModel>
                    {
                        IsSuccess = false,
                        Message = "Cannot null parameters: DistributorCode, FromDate, ToDate",
                        Code = 400,
                        Data = null
                    };
                }

                List<string> listStatusReport = new List<string>{
                            SO_SaleOrderStatusConst.DELIVERED,
                            SO_SaleOrderStatusConst.PARTIALDELIVERED
                };

                var query = _orderInformationsRepository.GetAllQueryable(null, null, null, _schemaName)
                            .AsNoTracking()
                            .Where(x =>
                                parameters.DistributorCode == x.DistributorCode &&
                                (!string.IsNullOrEmpty(parameters.RouteZoneCode) ? parameters.RouteZoneCode == x.RouteZoneID : true) &&
                                parameters.FromDate.Value.Date <= x.OrderDate.Date &&
                                parameters.ToDate.Value.Date >= x.OrderDate.Date &&
                                listStatusReport.Contains(x.Status))
                            .Select(item => new SalesReportDetailModel
                            {
                                RouteZoneCode = item.RouteZoneID,
                                CustomerCode = item.CustomerId,
                                CustomerName = item.CustomerName,
                                OrderDate = item.OrderDate,
                                SONumber = item.OrderRefNumber,
                                TotalAmount = item.Shipped_Amt,
                                VAT = item.TotalVAT,
                                DiscountAmount = item.Shipped_Disc_Amt,
                                Revenue = item.Shipped_Extend_Amt
                            })
                            .AsQueryable();

                var dataToPaged = PagedList<SalesReportDetailModel>.ToPagedListQueryAble(query, (parameters.PageNumber - 1) * parameters.PageSize, parameters.PageSize);
                var repsonse = new ListSalesReportDetailModel { Items = dataToPaged, MetaData = dataToPaged.MetaData };

                return new ResultModelWithObject<ListSalesReportDetailModel>
                {
                    IsSuccess = true,
                    Message = "Successfully",
                    Code = 200,
                    Data = repsonse
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new ResultModelWithObject<ListSalesReportDetailModel>
                {
                    IsSuccess = false,
                    Message = ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace,
                    Code = 500,
                    Data = null
                };
            }
        }

        // SO.RP03
        public async Task<BaseResultModel> SalesDetailReport(SalesReportQuery parameters, string userName, string token)
        {
            if (parameters.IsOver3Month()) return parameters.IsOver3MonthResult();
            try
            {
                if (string.IsNullOrWhiteSpace(parameters.DistributorCode))
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Message = "DistributorCode cannot null",
                        Code = 400
                    };
                }

                if (parameters.IsListDetail)
                {
                    ResultModelWithObject<ListSalesReportDetailModel> result = await GetListDataForSalesDetailModel(parameters);

                    return new BaseResultModel
                    {
                        Code = result.Code,
                        IsSuccess = result.IsSuccess,
                        Message = result.Message,
                        Data = result.Data
                    };
                }
                else
                {
                    // Status
                    List<string> listStatusReport = new List<string>{
                            SO_SaleOrderStatusConst.DELIVERED,
                            SO_SaleOrderStatusConst.PARTIALDELIVERED
                    };

                    // Chuyển list status thành đầu vào của func
                    string orderStatus = string.Join(",", listStatusReport).Trim();

                    string _query;
                    if (!string.IsNullOrWhiteSpace(parameters.RouteZoneCode))
                    {
                        _query = $@"SELECT * from ODSalesDetailReport('{parameters.DistributorCode}', '{parameters.RouteZoneCode}', '{orderStatus}', '{parameters.FromDate.Value.Date.StartOfDate()}', '{parameters.ToDate.Value.Date.EndOfDate()}', '{_schemaName}')";
                    }
                    else
                    {
                        _query = $@"SELECT * from ODSalesDetailReport('{parameters.DistributorCode}', null, '{orderStatus}', '{parameters.FromDate.Value.Date.StartOfDate()}', '{parameters.ToDate.Value.Date.EndOfDate()}', '{_schemaName}')";
                    }

                    FnSaslesDetailReportModel dataCollect = await _dataContext.FnSaslesDetailReportModels.FromSqlRaw(_query).AsNoTracking().FirstOrDefaultAsync();
                    SalesReportModel dataRes = new();
                    dataRes.DistributorCode = parameters.DistributorCode;

                    // Trường hợp không có data
                    if (dataCollect == null)
                    {
                        dataRes.TotalAmount = 0;
                        dataRes.VAT = 0;
                        dataRes.DiscountAmount = 0;
                        dataRes.Revenue = 0;

                        return new BaseResultModel
                        {
                            IsSuccess = true,
                            Message = "Successfully",
                            Code = 200,
                            Data = dataRes
                        };
                    }

                    dataRes.DistributorCode = dataCollect.DistributorCode;
                    dataRes.TotalAmount = dataCollect.TotalAmount;
                    dataRes.VAT = dataCollect.VAT;
                    dataRes.DiscountAmount = dataCollect.DiscountAmount;
                    dataRes.Revenue = dataCollect.Revenue;

                    parameters.PageSize = 10;
                    parameters.PageNumber = 1;
                    ResultModelWithObject<ListSalesReportDetailModel> result = await GetListDataForSalesDetailModel(parameters);
                    if (!result.IsSuccess)
                    {
                        return new BaseResultModel
                        {
                            Code = result.Code,
                            IsSuccess = false,
                            Message = result.Message
                        };
                    }

                    dataRes.OrderInformations = result.Data;

                    _dataContext.Dispose();
                    return new BaseResultModel
                    {
                        Code = 200,
                        Data = dataRes,
                        IsSuccess = true,
                        Message = "Successfully"
                    };
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }

        //RP04
        public async Task<BaseResultModel> SalesSynthesisReport(SummaryRevenueReportQuery parameters, string userName, string token)
        {
            if (parameters.IsOver3Month()) return parameters.IsOver3MonthResult();
            try
            {
                var hODataModel = await HOGetDetail(userName, token);
                List<string> listDistributor = new List<string>();

                if (!hODataModel.IsHO)
                {
                    listDistributor.Add(parameters.DistributorCode);
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(parameters.DistributorCode))
                    {
                        if (hODataModel.Distributors.Any(x => x != null && x == parameters.DistributorCode))
                        {
                            hODataModel.Distributors = new List<string>() { parameters.DistributorCode };
                        }
                    }

                    listDistributor.AddRange(hODataModel.Distributors);
                }

                string _listDistributor = string.Join(",", listDistributor).Trim();
                string _query;
                if (!string.IsNullOrWhiteSpace(parameters.RouteZoneCode))
                {
                    _query = $@"SELECT * FROM ODSalesSynthesisReport('{_listDistributor}', '{parameters.RouteZoneCode}', '{parameters.ReportType}', '{parameters.FromDate.Value.Date.StartOfDate()}', '{parameters.ToDate.Value.Date.EndOfDate()}', '{_schemaName}')";
                }
                else
                {
                    _query = $@"SELECT * FROM ODSalesSynthesisReport('{_listDistributor}', null, '{parameters.ReportType}', '{parameters.FromDate.Value.Date.StartOfDate()}', '{parameters.ToDate.Value.Date.EndOfDate().ToString(FormatConstants.DATETIME_REQ_DB)}', '{_schemaName}')";
                }

                List<FnSalesSynthesisReportModel> dataCollect = await _dataContext.FnSalesSynthesisReportModels.FromSqlRaw(_query).AsNoTracking().ToListAsync();

                List<RevenueReportRespone> listDataRes = new();

                List<FnSalesSynthesisReportModel> listGroupDistributor = dataCollect.GroupBy(x => x.DistributorCode).Select(x => x.First()).ToList();

                foreach (var dis in listGroupDistributor)
                {
                    RevenueReportRespone dataRes = new();
                    dataRes.DistributorCode = dis.DistributorCode;

                    List<FnSalesSynthesisReportModel> listGroupRouteZone = dataCollect.GroupBy(x => x.RouteZoneID).Select(x => x.First()).ToList();
                    foreach (var rz in listGroupRouteZone)
                    {
                        RevenueReportRouteZoneRespone dataRzRes = new();
                        dataRzRes.RouteZoneCode = rz.RouteZoneID;

                        foreach (var item in dataCollect.Where(x => x.RouteZoneID == rz.RouteZoneID && x.DistributorCode == dis.DistributorCode).ToList())
                        {
                            RevenueReportDetailRespone dataDetailRes = new();
                            dataDetailRes.OrderDate = item.OrderDate;
                            dataDetailRes.Amount = item.Amount;
                            dataRzRes.ListData.Add(dataDetailRes);
                        }
                        dataRzRes.ListData = dataRzRes.ListData.OrderByDescending(x => x.OrderDate).ToList();
                        dataRes.ListRouteZone.Add(dataRzRes);
                    }

                    listDataRes.Add(dataRes);
                }

                _dataContext.Dispose();
                return new BaseResultModel
                {
                    Code = 200,
                    Data = listDataRes,
                    IsSuccess = true,
                    Message = "OK"
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }


        //SO.RP05
        public async Task<BaseResultModel> ProductivityReport(ProductivityReportRequest parameters, string token)
        {
            if (parameters.IsOver3Month()) return parameters.IsOver3MonthResult();
            try
            {
                List<ProductivityReportModel> saleOrders = new();
                List<string> listDistribtuorCode = new();
                if (!string.IsNullOrWhiteSpace(parameters.DistributorCode))
                {
                    listDistribtuorCode.Add(parameters.DistributorCode);
                }
                //if (!string.IsNullOrWhiteSpace(parameters.TerritoryStructureCode) && !string.IsNullOrWhiteSpace(parameters.TerritoryValue))
                //{
                //    List<ProductivityReportRouteZoneRespone> listDataRespone = new();
                //    // Check level territory structure
                //    ResultModelWithObject<List<TerritoryStructureDetailModel>> resultTerritoryStructureDetail = await GetTerritoryStructureDetail(parameters.TerritoryStructureCode, token);
                //    if (!resultTerritoryStructureDetail.IsSuccess)
                //    {
                //        return new BaseResultModel
                //        {
                //            Code = resultTerritoryStructureDetail.Code,
                //            IsSuccess = false,
                //            Message = resultTerritoryStructureDetail.Message
                //        };
                //    }

                //    int level = resultTerritoryStructureDetail.Data.Count();

                //    // Get territory mapping
                //    ResultModelWithObject<List<TerritoryMappingByValueModel>> resultTerritoryMapping = await CommonGetListChildNodeByTerritoryValue(parameters.TerritoryValue, parameters.TerritoryStructureCode, parameters.SaleOrgCode, token);
                //    if (!resultTerritoryMapping.IsSuccess)
                //    {
                //        return new BaseResultModel
                //        {
                //            Code = resultTerritoryMapping.Code,
                //            IsSuccess = false,
                //            Message = resultTerritoryMapping.Message
                //        };
                //    }

                //    List<TerritoryMappingByValueModel> listLastNode = resultTerritoryMapping.Data;
                //    listLastNode.Select(d => d.EffectiveDate);
                //    if (listLastNode != null && listLastNode.Count > 0)
                //    {
                //        var lastNodeCurrent = listLastNode.FirstOrDefault();
                //        if (lastNodeCurrent.Level != level && string.IsNullOrWhiteSpace(parameters.DistributorCode))
                //        {
                //            foreach (var node in lastNodeCurrent.ListChildren)
                //            {
                //                var resultListDistributor = await GetListDistributorByTerritoryValue(node.TerritoryValueKey, node.TerritoryStructureCode, node.SaleOrgCode, token);
                //                if (!resultListDistributor.IsSuccess)
                //                {
                //                    return new BaseResultModel
                //                    {
                //                        Code = resultListDistributor.Code,
                //                        IsSuccess = false,
                //                        Message = resultListDistributor.Message
                //                    };
                //                }
                //                var listDistributor = resultListDistributor.Data;
                //                if (listDistributor != null && listDistributor.Count > 0)
                //                {
                //                    foreach (var distributor in listDistributor)
                //                    {
                //                        listDistribtuorCode.Add(distributor.DistributorCode);
                //                    }
                //                }
                //            }
                //        }
                //        else
                //        {
                //            var resultListDistributor = await GetListDistributorByTerritoryValue(lastNodeCurrent.TerritoryValueKey, lastNodeCurrent.TerritoryStructureCode, lastNodeCurrent.SaleOrgCode, token);
                //            if (!resultListDistributor.IsSuccess)
                //            {
                //                return new BaseResultModel
                //                {
                //                    Code = resultListDistributor.Code,
                //                    IsSuccess = false,
                //                    Message = resultListDistributor.Message
                //                };
                //            }
                //            var listDistributor = resultListDistributor.Data;
                //            if (listDistributor != null && listDistributor.Count > 0)
                //            {
                //                foreach (var distributor in listDistributor)
                //                {
                //                    listDistribtuorCode.Add(distributor.DistributorCode);
                //                }
                //            }
                //        }
                //    }


                //    if (listDistribtuorCode?.Count > 0)
                //    {
                //        var listLastNodeDic = listLastNode.ToDictionary(d => (d.SaleOrgCode, d.TerritoryStructureCode, d.TerritoryValueKey), m => m);
                //        string distributorCodes = !string.IsNullOrWhiteSpace(parameters.DistributorCode) ? parameters.DistributorCode : string.Join(",", listDistribtuorCode.Distinct().ToList());
                //        //var responseFromFn = await _dataContext.FnProductivityReportModels
                //        //   .FromSqlRaw($@"select * from f_TLC_ProductivityReport('{distributorCodes}','{parameters.FromDate.StartOfDate().ToString(FormatConstants.DATETIME_REQ_DB)}','{parameters.ToDate.EndOfDate().ToString(FormatConstants.DATETIME_REQ_DB)}', '{_schemaName}')")
                //        //   .AsNoTracking()
                //        //   .ToListAsync();
                //        //_ = responseFromFn.GroupBy(d => (d.SalesOrgID, d.TerritoryStrID, d.TerritoryValueKey)).Select(d =>
                //        //{
                //        //    listDataRespone.Add(new ProductivityReportSalesTerritoryRespone
                //        //    {
                //        //        TerritoryLevelValues = d.Key.TerritoryValueKey,
                //        //        TerritoryValeDescription = listLastNodeDic != null && listLastNodeDic.ContainsKey(d.Key) ? listLastNodeDic[d.Key].TerritoryValueDescription : string.Empty,
                //        //        ListData = d.Select(l => new ProductivityTerritoryValueDetailRespone
                //        //        {
                //        //            //InventoryId = l.InventoryID,
                //        //            //OrderSKUQty = l.OrderBaseQuantities,
                //        //            //Description = l.ItemDescription,
                //        //            //ShippedSKUQty = l.ShippedBaseQuantities,
                //        //            //BaseUom = l.BaseUomCode

                //        //        }).OrderByDescending(d => d.InventoryId).ToList()
                //        //    });
                //        //    return d;
                //        //}).ToList();
                //        var queryStr = $@"select * from f_tlc_productivityreport('{distributorCodes}','{parameters.FromDate.StartOfDate().ToString(FormatConstants.DATETIME_REQ_DB)}','{parameters.ToDate.EndOfDate().ToString(FormatConstants.DATETIME_REQ_DB)}', '{_schemaName}')";
                //        var responseFromFn =(List<ReportProductivityReport>)_dapperRepositories.Query<ReportProductivityReport>(queryStr);
                //        _ = responseFromFn.GroupBy(d => d.RouteZoneID).Select(d =>
                //        {
                //            listDataRespone.Add(new ProductivityReportRouteZoneRespone
                //            {
                //                RouteZoneCode = d.Key,
                //                ListData = d.Select(l => new ProductivityDetailRespone
                //                {
                //                    RouteZone = l.RouteZoneID,
                //                    InventoryId = l.InventoryID,
                //                    Description = l.ItemDescription,
                //                    SLThungOrder = l.SLThungOrder,
                //                    SLLocOrder = l.SLLocOrder,
                //                    SLChaiOrder = l.SLChaiOrder,
                //                    SLThungShipped = l.SLThungShipped,
                //                    SLLocShipped = l.SLLocShipped,
                //                    SLChaiShipped = l.SLChaiShipped
                //                }).OrderByDescending(d => d.InventoryId).ToList()
                //            });
                //            return d;
                //        }).ToList();
                //    }

                //    //release resources
                //    _orderInformationsRepository.Dispose(_schemaName);
                //    _dataContext.Dispose();

                //    return new BaseResultModel
                //    {
                //        Code = 200,
                //        Data = listDataRespone,
                //        IsSuccess = true,
                //        Message = "OK"
                //    };
                //}
                //else
                //{
                //    string routeZoneCodes = parameters.ListRouteZoneCode?.Count > 0 ? string.Join(",", parameters.ListRouteZoneCode) : string.Empty;

                //    string distributorCodes = !string.IsNullOrWhiteSpace(parameters.DistributorCode) ? parameters.DistributorCode : string.Join(",", listDistribtuorCode.Distinct().ToList());
                //    var responseFromFn = await _dataContext.FnProductivityReportWithRouteZoneModels
                //        .FromSqlRaw($@"select * from ODProductivityReportWithRouteZone('{distributorCodes}','{routeZoneCodes}','{parameters.FromDate.StartOfDate().ToString(FormatConstants.DATETIME_REQ_DB)}','{parameters.ToDate.EndOfDate().ToString(FormatConstants.DATETIME_REQ_DB)}', '{_schemaName}')")
                //        .AsNoTracking()
                //        .ToListAsync();
                //    List<ProductivityReportRouteZoneRespone> listDataRespone = new();
                //    _ = responseFromFn.GroupBy(d => d.RouteZoneID).Select(d =>
                //    {
                //        listDataRespone.Add(new ProductivityReportRouteZoneRespone
                //        {
                //            RouteZoneCode = d.Key,
                //            ListData = d.Select(l => new ProductivityDetailRespone
                //            {
                //                RouteZone = l.RouteZoneID,
                //                InventoryId = l.InventoryID,
                //                Description = l.ItemDescription,
                //            }).OrderByDescending(d => d.InventoryId).ToList()
                //        });
                //        return d;
                //    }).ToList();
                //    //release resources
                //    _orderInformationsRepository.Dispose(_schemaName);
                //    _dataContext.Dispose();
                //    return new BaseResultModel
                //    {
                //        Code = 200,
                //        Data = listDataRespone,
                //        IsSuccess = true,
                //        Message = "OK"
                //    };
                //}

                List<ProductivityReportRouteZoneRespone> listDataRespone = new();
                // Check level territory structure
                ResultModelWithObject<List<TerritoryStructureDetailModel>> resultTerritoryStructureDetail = await GetTerritoryStructureDetail(parameters.TerritoryStructureCode, token);
                if (!resultTerritoryStructureDetail.IsSuccess)
                {
                    return new BaseResultModel
                    {
                        Code = resultTerritoryStructureDetail.Code,
                        IsSuccess = false,
                        Message = resultTerritoryStructureDetail.Message
                    };
                }

                int level = resultTerritoryStructureDetail.Data.Count();

                // Get territory mapping
                ResultModelWithObject<List<TerritoryMappingByValueModel>> resultTerritoryMapping = await CommonGetListChildNodeByTerritoryValue(parameters.TerritoryValue, parameters.TerritoryStructureCode, parameters.SaleOrgCode, token);
                if (!resultTerritoryMapping.IsSuccess)
                {
                    return new BaseResultModel
                    {
                        Code = resultTerritoryMapping.Code,
                        IsSuccess = false,
                        Message = resultTerritoryMapping.Message
                    };
                }

                List<TerritoryMappingByValueModel> listLastNode = resultTerritoryMapping.Data;
                listLastNode.Select(d => d.EffectiveDate);
                if (listLastNode != null && listLastNode.Count > 0)
                {
                    var lastNodeCurrent = listLastNode.FirstOrDefault();
                    if (lastNodeCurrent.Level != level && string.IsNullOrWhiteSpace(parameters.DistributorCode))
                    {
                        foreach (var node in lastNodeCurrent.ListChildren)
                        {
                            var resultListDistributor = await GetListDistributorByTerritoryValue(node.TerritoryValueKey, node.TerritoryStructureCode, node.SaleOrgCode, token);
                            if (!resultListDistributor.IsSuccess)
                            {
                                return new BaseResultModel
                                {
                                    Code = resultListDistributor.Code,
                                    IsSuccess = false,
                                    Message = resultListDistributor.Message
                                };
                            }
                            var listDistributor = resultListDistributor.Data;
                            if (listDistributor != null && listDistributor.Count > 0)
                            {
                                foreach (var distributor in listDistributor)
                                {
                                    listDistribtuorCode.Add(distributor.DistributorCode);
                                }
                            }
                        }
                    }
                    else
                    {
                        var resultListDistributor = await GetListDistributorByTerritoryValue(lastNodeCurrent.TerritoryValueKey, lastNodeCurrent.TerritoryStructureCode, lastNodeCurrent.SaleOrgCode, token);
                        if (!resultListDistributor.IsSuccess)
                        {
                            return new BaseResultModel
                            {
                                Code = resultListDistributor.Code,
                                IsSuccess = false,
                                Message = resultListDistributor.Message
                            };
                        }
                        var listDistributor = resultListDistributor.Data;
                        if (listDistributor != null && listDistributor.Count > 0)
                        {
                            foreach (var distributor in listDistributor)
                            {
                                listDistribtuorCode.Add(distributor.DistributorCode);
                            }
                        }
                    }
                }
                if (listDistribtuorCode.Count > 0)
                {
                    string routeZoneCodes = parameters.ListRouteZoneCode?.Count > 0 ? string.Join(",", parameters.ListRouteZoneCode) : string.Empty;
                    var listLastNodeDic = listLastNode.ToDictionary(d => (d.SaleOrgCode, d.TerritoryStructureCode, d.TerritoryValueKey), m => m);
                    string distributorCodes = !string.IsNullOrWhiteSpace(parameters.DistributorCode) ? parameters.DistributorCode : string.Join(",", listDistribtuorCode.Distinct().ToList());
                    var queryStr = string.Empty;
                    if(string.IsNullOrEmpty(routeZoneCodes))
                    {
                        queryStr = $@"select * from f_tlc_productivityreport('{distributorCodes}', null,'{parameters.FromDate.StartOfDate().ToString(FormatConstants.DATETIME_REQ_DB)}','{parameters.ToDate.EndOfDate().ToString(FormatConstants.DATETIME_REQ_DB)}', '{_schemaName}')";
                    }
                    else
                    {
                        queryStr = $@"select * from f_tlc_productivityreport('{distributorCodes}','{routeZoneCodes}','{parameters.FromDate.StartOfDate().ToString(FormatConstants.DATETIME_REQ_DB)}','{parameters.ToDate.EndOfDate().ToString(FormatConstants.DATETIME_REQ_DB)}', '{_schemaName}')";
                    }
                    var responseFromFn = (List<ReportProductivityReport>)_dapperRepositories.Query<ReportProductivityReport>(queryStr);
                    _ = responseFromFn.GroupBy(d => d.RouteZoneID).Select(d =>
                    {
                        listDataRespone.Add(new ProductivityReportRouteZoneRespone
                        {
                            RouteZoneCode = d.Key,
                            ListData = d.Select(l => new ProductivityDetailRespone
                            {
                                RouteZone = l.RouteZoneID,
                                InventoryId = l.InventoryID,
                                Description = l.ItemDescription,
                                SLThungOrder = l.SLThungOrder,
                                SLLocOrder = l.SLLocOrder,
                                SLChaiOrder = l.SLChaiOrder,
                                SLThungShipped = l.SLThungShipped,
                                SLLocShipped = l.SLLocShipped,
                                SLChaiShipped = l.SLChaiShipped
                            }).OrderByDescending(d => d.InventoryId).ToList()
                        });
                        return d;
                    }).ToList();
                }

                //release resources
                _orderInformationsRepository.Dispose(_schemaName);
                _dataContext.Dispose();

                return new BaseResultModel
                {
                    Code = 200,
                    Data = listDataRespone,
                    IsSuccess = true,
                    Message = "OK"
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }

        //RP06
        public async Task<BaseResultModel> ProductivityByDayReport(ProductivityReportRequest parameters, string token)
        {
            if (parameters.IsOver3Month()) return parameters.IsOver3MonthResult();
            try
            {
                List<ProductivityByDayReportModel> saleOrders = new();
                List<string> listDistribtuorCode = new();
                if (!string.IsNullOrEmpty(parameters.DistributorCode)) listDistribtuorCode.Add(parameters.DistributorCode);
                //if (!string.IsNullOrWhiteSpace(parameters.TerritoryStructureCode) && !string.IsNullOrWhiteSpace(parameters.TerritoryValue))
                //{
                //    var listDataRespone = new List<ProductivityByDayReportTerritoryValueRespone>();
                //    // Check level territory structure
                //    var resultTerritoryStructureDetail = await GetTerritoryStructureDetail(parameters.TerritoryStructureCode, token);
                //    if (!resultTerritoryStructureDetail.IsSuccess)
                //    {
                //        return new BaseResultModel
                //        {
                //            Code = resultTerritoryStructureDetail.Code,
                //            IsSuccess = false,
                //            Message = resultTerritoryStructureDetail.Message
                //        };
                //    }

                //    var level = resultTerritoryStructureDetail.Data.Count();

                //    // Get territory mapping
                //    var resultTerritoryMapping = await CommonGetListChildNodeByTerritoryValue(parameters.TerritoryValue, parameters.TerritoryStructureCode, parameters.SaleOrgCode, token);
                //    if (!resultTerritoryMapping.IsSuccess)
                //    {
                //        return new BaseResultModel
                //        {
                //            Code = resultTerritoryMapping.Code,
                //            IsSuccess = false,
                //            Message = resultTerritoryMapping.Message
                //        };
                //    }

                //    var listLastNode = resultTerritoryMapping.Data;
                //    if (listLastNode != null && listLastNode.Count > 0)
                //    {
                //        var lastNodeCurrent = listLastNode.FirstOrDefault();
                //        if (lastNodeCurrent.Level != level && string.IsNullOrWhiteSpace(parameters.DistributorCode))
                //        {
                //            foreach (var node in lastNodeCurrent.ListChildren)
                //            {
                //                var resultListDistributor = await GetListDistributorByTerritoryValue(node.TerritoryValueKey, node.TerritoryStructureCode, node.SaleOrgCode, token);
                //                if (!resultListDistributor.IsSuccess)
                //                {
                //                    return new BaseResultModel
                //                    {
                //                        Code = resultListDistributor.Code,
                //                        IsSuccess = false,
                //                        Message = resultListDistributor.Message
                //                    };
                //                }
                //                var listDistributor = resultListDistributor.Data;
                //                if (listDistributor != null && listDistributor.Count > 0)
                //                {
                //                    foreach (var distributor in listDistributor)
                //                    {
                //                        listDistribtuorCode.Add(distributor.DistributorCode);
                //                    }
                //                }
                //            }
                //        }
                //        else
                //        {
                //            var resultListDistributor = await GetListDistributorByTerritoryValue(lastNodeCurrent.TerritoryValueKey, lastNodeCurrent.TerritoryStructureCode, lastNodeCurrent.SaleOrgCode, token);
                //            if (!resultListDistributor.IsSuccess)
                //            {
                //                return new BaseResultModel
                //                {
                //                    Code = resultListDistributor.Code,
                //                    IsSuccess = false,
                //                    Message = resultListDistributor.Message
                //                };
                //            }
                //            var listDistributor = resultListDistributor.Data;
                //            if (listDistributor != null && listDistributor.Count > 0)
                //            {
                //                foreach (var distributor in listDistributor)
                //                {
                //                    listDistribtuorCode.Add(distributor.DistributorCode);
                //                }
                //            }
                //        }
                //    }
                //    if (listDistribtuorCode?.Count > 0)
                //    {
                //        var listLastNodeDic = listLastNode.ToDictionary(d => (d.SaleOrgCode, d.TerritoryStructureCode, d.TerritoryValueKey), m => m);
                //        //string distributorCodes = string.Join(",", listDistribtuorCode.Distinct().ToList());
                //        string distributorCodes = !string.IsNullOrWhiteSpace(parameters.DistributorCode) ? parameters.DistributorCode : string.Join(",", listDistribtuorCode.Distinct().ToList());
                //        var responseFromFn = await _dataContext.FnProductivityByDayReportModels
                //        .FromSqlRaw($@"select * from ODProductivityByDayReport('{distributorCodes}','{parameters.FromDate.StartOfDate().ToString(FormatConstants.DATETIME_REQ_DB)}','{parameters.ToDate.EndOfDate().ToString(FormatConstants.DATETIME_REQ_DB)}', '{_schemaName}')")
                //        .AsNoTracking()
                //        .ToListAsync();

                //        listDataRespone = responseFromFn.GroupBy(d => (d.SalesOrgID, d.TerritoryStrID, d.TerritoryValueKey), (key, groupedData) => new ProductivityByDayReportTerritoryValueRespone
                //        {
                //            TerritoryLevelValues = key.TerritoryValueKey,
                //            TerritoryValeDescription = listLastNodeDic != null && listLastNodeDic.ContainsKey(key) ? listLastNodeDic[key].TerritoryValueDescription : string.Empty,
                //            ListData = groupedData.GroupBy(o => o.InventoryID, (inventoryKey, inventoryGrouped) => new ProductivityByDayReportDetailRespone
                //            {
                //                InventoryId = inventoryKey,
                //                TerritoryLevelValues = key.TerritoryValueKey,
                //                Description = inventoryGrouped.Select(d => d.ItemDescription).FirstOrDefault(),
                //                ListData = inventoryGrouped.Select(d => new LineProductivityByDayRes
                //                {
                //                    OrderDate = d.OrderDate,
                //                    ShippedSKUQty = d.ShippedBaseQuantities
                //                }).ToList()
                //            }).ToList()
                //        }).ToList();
                //    }

                //    _orderInformationsRepository.Dispose(_schemaName);
                //    return new BaseResultModel
                //    {
                //        Code = 200,
                //        Data = listDataRespone,
                //        IsSuccess = true,
                //        Message = "OK"
                //    };
                //}
                //else
                //{
                //    string routeZoneCodes = parameters.ListRouteZoneCode?.Count > 0 ? string.Join(",", parameters.ListRouteZoneCode) : string.Empty;
                //    //string distributorCodes = string.Join(",", listDistribtuorCode.Distinct().ToList());
                //    string distributorCodes = !string.IsNullOrWhiteSpace(parameters.DistributorCode) ? parameters.DistributorCode : string.Join(",", listDistribtuorCode.Distinct().ToList());
                //    var responseFromFn = await _dataContext.FnProductivityByDayReportWithRouteZoneModels
                //        .FromSqlRaw($@"select * from ODProductivityByDayReportWithRouteZone('{distributorCodes}','{routeZoneCodes}','{parameters.FromDate.StartOfDate().ToString(FormatConstants.DATETIME_REQ_DB)}','{parameters.ToDate.EndOfDate().ToString(FormatConstants.DATETIME_REQ_DB)}', '{_schemaName}')")
                //        .AsNoTracking()
                //        .ToListAsync();

                //    List<ProductivityByDayReportRouteZoneRespone> listDataRespone = responseFromFn.GroupBy(routeZone => routeZone.RouteZoneID,
                //       (key, routeZoneGroup) => new ProductivityByDayReportRouteZoneRespone
                //       {
                //           RouteZoneCode = key,
                //           ListData = routeZoneGroup.GroupBy(o => o.InventoryID,
                //               (inventoryKey, inventoryGroup) => new ProductivityByDayReportDetailRespone
                //               {
                //                   InventoryId = inventoryKey,
                //                   TerritoryLevelValues = "",
                //                   Description = inventoryGroup.Select(d => d.ItemDescription).FirstOrDefault(),
                //                   ListData = inventoryGroup
                //                   .Select(d => new LineProductivityByDayRes
                //                   {
                //                       OrderDate = d.OrderDate,
                //                       ShippedSKUQty = d.ShippedBaseQuantities
                //                   }).ToList()
                //               }
                //           ).ToList()
                //       }
                //   ).ToList();

                //    _orderInformationsRepository.Dispose(_schemaName);
                //    return new BaseResultModel
                //    {
                //        Code = 200,
                //        Data = listDataRespone,
                //        IsSuccess = true,
                //        Message = "OK"
                //    };

                //}

                //var listDataRespone = new List<ProductivityByDayReportTerritoryValueRespone>();
                // Check level territory structure
                //var resultTerritoryStructureDetail = await GetTerritoryStructureDetail(parameters.TerritoryStructureCode, token);
                //if (!resultTerritoryStructureDetail.IsSuccess)
                //{
                //    return new BaseResultModel
                //    {
                //        Code = resultTerritoryStructureDetail.Code,
                //        IsSuccess = false,
                //        Message = resultTerritoryStructureDetail.Message
                //    };
                //}

                //var level = resultTerritoryStructureDetail.Data.Count();

                //// Get territory mapping
                //var resultTerritoryMapping = await CommonGetListChildNodeByTerritoryValue(parameters.TerritoryValue, parameters.TerritoryStructureCode, parameters.SaleOrgCode, token);
                //if (!resultTerritoryMapping.IsSuccess)
                //{
                //    return new BaseResultModel
                //    {
                //        Code = resultTerritoryMapping.Code,
                //        IsSuccess = false,
                //        Message = resultTerritoryMapping.Message
                //    };
                //}

                //var listLastNode = resultTerritoryMapping.Data;
                //if (listLastNode != null && listLastNode.Count > 0)
                //{
                //    var lastNodeCurrent = listLastNode.FirstOrDefault();
                //    if (lastNodeCurrent.Level != level && string.IsNullOrWhiteSpace(parameters.DistributorCode))
                //    {
                //        foreach (var node in lastNodeCurrent.ListChildren)
                //        {
                //            var resultListDistributor = await GetListDistributorByTerritoryValue(node.TerritoryValueKey, node.TerritoryStructureCode, node.SaleOrgCode, token);
                //            if (!resultListDistributor.IsSuccess)
                //            {
                //                return new BaseResultModel
                //                {
                //                    Code = resultListDistributor.Code,
                //                    IsSuccess = false,
                //                    Message = resultListDistributor.Message
                //                };
                //            }
                //            var listDistributor = resultListDistributor.Data;
                //            if (listDistributor != null && listDistributor.Count > 0)
                //            {
                //                foreach (var distributor in listDistributor)
                //                {
                //                    listDistribtuorCode.Add(distributor.DistributorCode);
                //                }
                //            }
                //        }
                //    }
                //    else
                //    {
                //        var resultListDistributor = await GetListDistributorByTerritoryValue(lastNodeCurrent.TerritoryValueKey, lastNodeCurrent.TerritoryStructureCode, lastNodeCurrent.SaleOrgCode, token);
                //        if (!resultListDistributor.IsSuccess)
                //        {
                //            return new BaseResultModel
                //            {
                //                Code = resultListDistributor.Code,
                //                IsSuccess = false,
                //                Message = resultListDistributor.Message
                //            };
                //        }
                //        var listDistributor = resultListDistributor.Data;
                //        if (listDistributor != null && listDistributor.Count > 0)
                //        {
                //            foreach (var distributor in listDistributor)
                //            {
                //                listDistribtuorCode.Add(distributor.DistributorCode);
                //            }
                //        }
                //    }
                //}
                List<ProductivityByDayReportRouteZoneRespone> listDataRespone = new List<ProductivityByDayReportRouteZoneRespone>();
                if (listDistribtuorCode?.Count > 0)
                {
                    string routeZoneCodes = parameters.ListRouteZoneCode?.Count > 0 ? string.Join(",", parameters.ListRouteZoneCode) : string.Empty;
                    string distributorCodes = !string.IsNullOrWhiteSpace(parameters.DistributorCode) ? parameters.DistributorCode : string.Join(",", listDistribtuorCode.Distinct().ToList());
                    string queryStr = string.Empty;
                    if(string.IsNullOrEmpty(routeZoneCodes))
                    {
                        queryStr = $@"SELECT * FROM f_productivitybydayreport('{distributorCodes}',null, '{parameters.FromDate.StartOfDate()}', '{parameters.ToDate.EndOfDate()}', '{_schemaName}');";
                    }
                    else
                    {
                        queryStr = $@"SELECT * FROM f_productivitybydayreport('{distributorCodes}','{routeZoneCodes}', '{parameters.FromDate.StartOfDate()}', '{parameters.ToDate.EndOfDate()}', '{_schemaName}');";
                    }
                    var responseFromFn = (List<ProductivityByDayResponse>)_dapperRepositories.Query<ProductivityByDayResponse>(queryStr);

                    listDataRespone = responseFromFn.GroupBy(routeZone => routeZone.RouteZoneID,
                       (key, routeZoneGroup) => new ProductivityByDayReportRouteZoneRespone
                       {
                           RouteZoneCode = key,
                           ListData = routeZoneGroup.GroupBy(o => o.InventoryID,
                               (inventoryKey, inventoryGroup) => new ProductivityByDayReportDetailRespone
                               {
                                   InventoryId = inventoryKey,
                                   TerritoryLevelValues = "",
                                   Description = inventoryGroup.Select(d => d.ItemDescription).FirstOrDefault(),
                                   ListData = inventoryGroup
                                   .Select(d => new LineProductivityByDayResponse
                                   {
                                       OrderDate = d.OrderDate,
                                       QuantityThung = d.QuantityThung,
                                       QuantityLoc = d.QuantityLoc,
                                       QuantityChai = d.QuantityChai
                                   }).ToList()
                               }
                           ).ToList()
                       }
                   ).OrderByDescending(x => x.RouteZoneCode).ToList();
                }

                _orderInformationsRepository.Dispose(_schemaName);
                return new BaseResultModel
                {
                    Code = 200,
                    Data = listDataRespone,
                    IsSuccess = true,
                    Message = "OK"
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }

        public async Task<ResultModelWithObject<List<DistributorInfoModel>>> GetListDistributorByTerritoryValue(string territoryValue, string territoryStructureCode, string saleOrgCode, string token)
        {
            try
            {
                // Handle Token
                string tokenSplit = token.Split(" ").Last();

                var dataReq = new GetListDistributorModel()
                {
                    TerritoryStructureCode = territoryStructureCode,
                    TerritoryValue = territoryValue,
                    SaleOrgCode = saleOrgCode
                };

                _clientSalesConfig.Authenticator = new JwtAuthenticator($"Rdos {tokenSplit}");
                var requestSO = new RestRequest($"SalesOrganization/GetListDistributor", Method.POST, DataFormat.Json);
                requestSO.AddJsonBody(dataReq);
                var resultSO = _clientSalesConfig.Execute(requestSO);

                if (resultSO == null || resultSO.Content == String.Empty)
                {
                    return new ResultModelWithObject<List<DistributorInfoModel>>
                    {
                        IsSuccess = false,
                        Code = 400,
                        Message = $"Cannot GetListDistributorByTerritoryValue"
                    };
                }

                var resultData = JsonConvert.DeserializeObject<ResultModelWithObject<List<DistributorInfoModel>>>(JsonConvert.DeserializeObject(resultSO.Content).ToString());

                if (!resultData.IsSuccess)
                {
                    return new ResultModelWithObject<List<DistributorInfoModel>>
                    {
                        IsSuccess = false,
                        Code = resultData.Code,
                        Message = resultData.Message
                    };
                }

                return new ResultModelWithObject<List<DistributorInfoModel>>
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Success",
                    Data = resultData.Data
                };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message);
                return new ResultModelWithObject<List<DistributorInfoModel>>
                {
                    Code = 500,
                    IsSuccess = false,
                    Message = ex.InnerException?.Message ?? ex.Message
                };
            }
        }

        public async Task<ResultModelWithObject<List<TerritoryStructureDetailModel>>> GetTerritoryStructureDetail(string territoryStructureCode, string token)
        {
            try
            {
                // Handle Token
                string tokenSplit = token.Split(" ").Last();

                _clientSalesConfig.Authenticator = new JwtAuthenticator($"Rdos {tokenSplit}");
                var requestSO = new RestRequest($"TerritoryStructure/{territoryStructureCode}", Method.GET, DataFormat.Json);
                var resultSO = _clientSalesConfig.Execute(requestSO);

                if (resultSO == null || resultSO.Content == String.Empty)
                {
                    return new ResultModelWithObject<List<TerritoryStructureDetailModel>>
                    {
                        IsSuccess = false,
                        Code = 400,
                        Message = $"Cannot Get TerritoryStructureDetail"
                    };
                }

                var resultData = JsonConvert.DeserializeObject<ResultModelWithObject<TerritoryStructureModel>>(JsonConvert.DeserializeObject(resultSO.Content).ToString());

                if (!resultData.IsSuccess)
                {
                    return new ResultModelWithObject<List<TerritoryStructureDetailModel>>
                    {
                        IsSuccess = false,
                        Code = resultData.Code,
                        Message = resultData.Message
                    };
                }

                return new ResultModelWithObject<List<TerritoryStructureDetailModel>>
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Success",
                    Data = resultData.Data.TerritoryStructureDetails
                };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message);
                return new ResultModelWithObject<List<TerritoryStructureDetailModel>>
                {
                    Code = 500,
                    IsSuccess = false,
                    Message = ex.InnerException?.Message ?? ex.Message
                };
            }
        }

        // SO.RP07
        public async Task<BaseResultModel> ProductivityBySalesReport(ProductivityBySalesReportRequest parameters, string userName, string token)
        {
            if (parameters.IsOver3Month()) return parameters.IsOver3MonthResult();
            try
            {
                if (string.IsNullOrWhiteSpace(parameters.IntemHierarchyLevel))
                {
                    return new BaseResultModel
                    {
                        Code = 400,
                        Message = "IntemHierarchyLevel is Required",
                        IsSuccess = false
                    };
                }

                // Check HO
                var hODataModel = await HOGetDetail(userName, token);
                //bool isHO = false;

                // Khởi tạo return
                List<ProductivityBySalesReportResultV2> result = new();

                // Level
                int Level = (parameters.IntemHierarchyLevel == ItemSettingConst.Industry ? 1 :
                             parameters.IntemHierarchyLevel == ItemSettingConst.Category ? 2 :
                             parameters.IntemHierarchyLevel == ItemSettingConst.SubCategory ? 3 :
                             parameters.IntemHierarchyLevel == ItemSettingConst.Brand ? 4 :
                             parameters.IntemHierarchyLevel == ItemSettingConst.SubBrand ? 5 :
                             parameters.IntemHierarchyLevel == ItemSettingConst.PackSize ? 6 :
                             parameters.IntemHierarchyLevel == ItemSettingConst.PackType ? 7 :
                             parameters.IntemHierarchyLevel == ItemSettingConst.Packaging ? 8 :
                             parameters.IntemHierarchyLevel == ItemSettingConst.Weight ? 9 :
                             parameters.IntemHierarchyLevel == ItemSettingConst.Volume ? 10 : 0);

                // Status
                List<string> listStatusReport = new List<string>{
                            SO_SaleOrderStatusConst.OPEN.Trim(),
                            SO_SaleOrderStatusConst.SHIPPING.Trim(),
                            SO_SaleOrderStatusConst.WAITNGSHIPPING.Trim(),
                            SO_SaleOrderStatusConst.DELIVERED.Trim(),
                            SO_SaleOrderStatusConst.PARTIALDELIVERED.Trim(),
                            SO_SaleOrderStatusConst.FAILED.Trim(),
                            SO_SaleOrderStatusConst.CONFIRM.Trim(),
                            SO_SaleOrderStatusConst.COMPLETE_DRAFT.Trim()};

                // Chuyển list status thành đầu vào của func
                string orderStatus = string.Join(",", listStatusReport).Trim();

                if (!hODataModel.IsHO)
                //if (!isHO)
                {
                    string _query;
                    if (string.IsNullOrWhiteSpace(parameters.RouteZoneCode))
                    {
                        _query = $@"SELECT * from f_oddisproductivitybysalesreport_v2('{parameters.IntemHierarchyLevel}', '{parameters.DistributorCode}', null, '{orderStatus}', '{parameters.FromDate.Value.Date.StartOfDate().ToString(FormatConstants.DATETIME_REQ_DB)}', '{parameters.ToDate.Value.Date.EndOfDate().ToString(FormatConstants.DATETIME_REQ_DB)}', '{_schemaName}')";
                    }
                    else
                    {
                        _query = $@"SELECT * from f_oddisproductivitybysalesreport_v2('{parameters.IntemHierarchyLevel}', '{parameters.DistributorCode}', '{parameters.RouteZoneCode}', '{orderStatus}', '{parameters.FromDate.Value.Date.StartOfDate().ToString(FormatConstants.DATETIME_REQ_DB)}', '{parameters.ToDate.Value.Date.EndOfDate().ToString(FormatConstants.DATETIME_REQ_DB)}', '{_schemaName}')";
                    }
                    //var dataCollect = await _dataContext.DisProductivityBySalesReportV2.FromSqlRaw(_query).AsNoTracking().ToListAsync();
                    var dataCollect =(List<DisProductivityBySalesReportModelV2>) _dapperRepositories.Query<DisProductivityBySalesReportModelV2>(_query);
                    if (dataCollect.Count > 0)
                    {
                        // Group attribute value
                        List<DisProductivityBySalesReportModelV2> groupAttributes = dataCollect.GroupBy(x => x.InventoryAttibute).Select(x => x.First()).ToList();

                        foreach (var item in groupAttributes)
                        {
                            ProductivityBySalesReportResultV2 resultNew = new();
                            resultNew.Level = Level;
                            resultNew.HierarchyLevelValue = item.InventoryAttibute;
                            resultNew.GroupedRouteZone = new();
                            foreach (var itemDetail in dataCollect.Where(x => x.InventoryAttibute == item.InventoryAttibute).ToList())
                            {
                                PBSReportRouteZoneListV2 resultDetailNew = new();
                                resultDetailNew.RoutezoneCode = itemDetail.RouteZoneID;
                                resultDetailNew.ShippedSLThung = itemDetail.ShippedSLThung;
                                resultDetailNew.ShippedSLLoc = itemDetail.ShippedSLLoc;
                                resultDetailNew.ShippedSLChai = itemDetail.ShippedSLChai;
                                resultNew.GroupedRouteZone.Add(resultDetailNew);
                            }

                            result.Add(resultNew);
                        }
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(parameters.DistributorCode))
                    {
                        if (hODataModel.Distributors.Any(x => x != null && x == parameters.DistributorCode))
                        {
                            hODataModel.Distributors = new List<string>() { parameters.DistributorCode };
                        }
                    }

                    string _query;
                    // Chuyển list Distributors thành đầu vào của func
                    string _distributors = string.Join(",", hODataModel.Distributors).Trim();

                    //if (string.IsNullOrWhiteSpace(parameters.SalesTerritoryValue))
                    //{
                    //    _query = $@"SELECT * from ODHOProductivityBySalesReport('{parameters.IntemHierarchyLevel}', '{_distributors}',  '{orderStatus}', '{parameters.FromDate.Value.Date.StartOfDate().ToString(FormatConstants.DATETIME_REQ_DB)}', '{parameters.ToDate.Value.Date.EndOfDate().ToString(FormatConstants.DATETIME_REQ_DB)}', '{_schemaName}')";
                    //}
                    //else
                    //{
                    //    _query = $@"SELECT * from ODHOProductivityBySalesReport('{parameters.IntemHierarchyLevel}', '{_distributors}', '{orderStatus}', '{parameters.FromDate.Value.Date.StartOfDate().ToString(FormatConstants.DATETIME_REQ_DB)}', '{parameters.ToDate.Value.Date.EndOfDate().ToString(FormatConstants.DATETIME_REQ_DB)}', '{_schemaName}')";
                    //}

                    _query = $@"SELECT * from f_odhoproductivitybysalesreport_v2('{parameters.IntemHierarchyLevel}', '{_distributors}', '{orderStatus}', '{parameters.FromDate.Value.Date.StartOfDate().ToString(FormatConstants.DATETIME_REQ_DB)}', '{parameters.ToDate.Value.Date.EndOfDate().ToString(FormatConstants.DATETIME_REQ_DB)}', '{_schemaName}')";

                    var dataCollect = (List<HoProductivityBySalesReportModelV2>)_dapperRepositories.Query<DisProductivityBySalesReportModelV2>(_query);
                    if (dataCollect.Count > 0)
                    {
                        // Group attribute value
                        List<HoProductivityBySalesReportModelV2> groupAttributes = dataCollect.GroupBy(x => x.InventoryAttibute).Select(x => x.First()).ToList();

                        foreach (var item in groupAttributes)
                        {
                            ProductivityBySalesReportResultV2 resultNew = new();
                            resultNew.Level = Level;
                            resultNew.HierarchyLevelValue = item.InventoryAttibute;
                            resultNew.GoupedTerritoryLevel = new();
                            foreach (var itemDetail in dataCollect.Where(x => x.InventoryAttibute == item.InventoryAttibute).ToList())
                            {
                                PBSGoupedTerritoryLevelV2 resultDetailNew = new();
                                resultDetailNew.TerritoryLevelKey = itemDetail.TerritoryValueKey;
                                resultDetailNew.ShippedSLThung = itemDetail.ShippedSLThung;
                                resultDetailNew.ShippedSLLoc = itemDetail.ShippedSLLoc;
                                resultDetailNew.ShippedSLChai = itemDetail.ShippedSLChai;
                                resultNew.GoupedTerritoryLevel.Add(resultDetailNew);
                            }

                            result.Add(resultNew);
                        }
                    }
                }

                _orderItemsRepository.Dispose(_schemaName);
                _dataContext.Dispose();
                return new BaseResultModel
                {
                    Code = 200,
                    Data = result,
                    IsSuccess = true,
                    Message = "OK"
                };


            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message);
                return new BaseResultModel
                {
                    Code = 500,
                    IsSuccess = false,
                    Message = ex.InnerException?.Message ?? ex.Message
                };
            }
        }

        // SO.RP08
        public async Task<BaseResultModel> ProductivityByProductReport(PBPReportRequest parameters, string userName, string token)
        {
            if (parameters.IsOver3Month()) return parameters.IsOver3MonthResult();
            try
            {
                var hODataModel = await HOGetDetail(userName, token);
                List<string> listterritoryValueKey = new();
                List<string> listDistribtuorCode = new();
                List<string> listStatus = new List<string>
                {
                    SO_SaleOrderStatusConst.DELIVERED.Trim(),
                    SO_SaleOrderStatusConst.PARTIALDELIVERED.Trim(),
                    SO_SaleOrderStatusConst.CONFIRM.Trim(),
                    SO_SaleOrderStatusConst.COMPLETE_DRAFT.Trim(),
                };
                if (!string.IsNullOrEmpty(parameters.DistributorCode))
                    listDistribtuorCode.Add(parameters.DistributorCode);

                if (hODataModel?.IsHO == true && hODataModel?.Distributors?.Count() > 0)
                {
                    listDistribtuorCode.AddRange(hODataModel.Distributors);
                }

                List<PBPReportResultV2> result = new();
                if (!string.IsNullOrWhiteSpace(parameters.ItemHierarchyLevel))
                {
                    //string distributorCodes = string.Join(",", listDistribtuorCode.Distinct().ToList());
                    string distributorCodes = !string.IsNullOrWhiteSpace(parameters.DistributorCode) ? parameters.DistributorCode : string.Join(",", listDistribtuorCode.Distinct().ToList());
                    string statuses = string.Join(",", listStatus.Distinct().ToList());
                    if (parameters.ViewBy.ToLower().Trim() == PBPReportViewByConst.DSA.ToLower().Trim())
                    {
                        //var responseFromFn = await _dataContext.FnDsaProductivityByProductReportModels
                        //   .FromSqlRaw($@"select * from f_oddsaproductivitybyproductreport_v2('{parameters.ItemHierarchyLevel}','{distributorCodes}',null,'{statuses}','{parameters.FromDate.Value.StartOfDate().ToString(FormatConstants.DATETIME_REQ_DB)}','{parameters.ToDate.Value.EndOfDate().ToString(FormatConstants.DATETIME_REQ_DB)}', '{_schemaName}')")
                        //   .AsNoTracking()
                        //   .ToListAsync();
                        var query = $@"select * from f_oddsaproductivitybyproductreport_v2('{parameters.ItemHierarchyLevel}','{distributorCodes}',null,'{statuses}','{parameters.FromDate.Value.StartOfDate().ToString(FormatConstants.DATETIME_REQ_DB)}','{parameters.ToDate.Value.EndOfDate().ToString(FormatConstants.DATETIME_REQ_DB)}', '{_schemaName}')";
                        var responseFromFn =(List<FnDsaProductivityByProductReportModelV2>) _dapperRepositories.Query<FnDsaProductivityByProductReportModelV2>(query);
                        _ = responseFromFn.GroupBy(d => d.DSAID).Select(d =>
                        {
                            result.Add(new PBPReportResultV2
                            {
                                DSACode = d.Key,
                                RoutezoneCode = string.Empty,
                                DataValues = d.Select(l => new PBPReportHierarchyLevelValueV2
                                {
                                    HierarchyLevelValue = parameters.ItemHierarchyLevel,
                                    Level = 0,
                                    ShippedSLThung = l.ShippedSLThung,
                                    ShippedSLLoc = l.ShippedSLLoc,
                                    ShippedSLChai = l.ShippedSLChai,
                                    InventoryAttibute = l.InventoryAttibute
                                }).ToList()
                            });
                            return d;
                        }).ToList();
                    }
                    else
                    {
                        //var responseFromFn = await _dataContext.FnRouteZoneProductivityByProductReportModels
                        //  .FromSqlRaw($@"select * from f_oddsaproductivitybyproductreport_v2('{parameters.ItemHierarchyLevel}','{distributorCodes}','{parameters.RouteZoneCode}','{statuses}','{parameters.FromDate.Value.StartOfDate().ToString(FormatConstants.DATETIME_REQ_DB)}','{parameters.ToDate.Value.EndOfDate().ToString(FormatConstants.DATETIME_REQ_DB)}', '{_schemaName}')")
                        //  .AsNoTracking()
                        //  .ToListAsync();
                        var query = $@"select * from f_odroutezoneproductivitybyproductreport_v2('{parameters.ItemHierarchyLevel}','{distributorCodes}','{parameters.RouteZoneCode}','{statuses}','{parameters.FromDate.Value.StartOfDate().ToString(FormatConstants.DATETIME_REQ_DB)}','{parameters.ToDate.Value.EndOfDate().ToString(FormatConstants.DATETIME_REQ_DB)}', '{_schemaName}')";
                        var responseFromFn = (List<FnRouteZoneProductivityByProductReportModelV2>)_dapperRepositories.Query<FnRouteZoneProductivityByProductReportModelV2>(query);
                        _ = responseFromFn.GroupBy(d => d.RouteZoneID).Select(d =>
                        {
                            result.Add(new PBPReportResultV2
                            {
                                DSACode = string.Empty,
                                RoutezoneCode = d.Key,
                                DataValues = d.Select(l => new PBPReportHierarchyLevelValueV2
                                {
                                    HierarchyLevelValue = parameters.ItemHierarchyLevel,
                                    Level = 0,
                                    ShippedSLThung = l.ShippedSLThung,
                                    ShippedSLLoc = l.ShippedSLLoc,
                                    ShippedSLChai = l.ShippedSLChai,
                                    InventoryAttibute = l.InventoryAttibute
                                }).ToList()
                            });
                            return d;
                        }).ToList();
                    }
                }

                _orderItemsRepository.Dispose(_schemaName);
                _dataContext.Dispose();
                return new BaseResultModel
                {
                    Code = 200,
                    Data = result,
                    IsSuccess = true,
                    Message = "OK"
                };


            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message);
                return new BaseResultModel
                {
                    Code = 500,
                    IsSuccess = false,
                    Message = ex.InnerException?.Message ?? ex.Message
                };
            }
        }
        #endregion

        #region DeliveryNote
        public async Task<BaseResultModel> PrintDeliveryNote(List<string> refNumbers, string token, string username)
        {
            try
            {
                var saleOrderListIndb = await _orderInformationsRepository.GetAllQueryable(null, null, null, _schemaName)
                    .Where(x => !x.IsDeleted && refNumbers.Contains(x.OrderRefNumber)).AsNoTracking().ToListAsync();

                List<string> missingSaleOrder = new();
                if (saleOrderListIndb.Count != refNumbers.Count)
                {
                    foreach (var refNumber in refNumbers)
                    {
                        if (!saleOrderListIndb.Any(x => x.OrderRefNumber == refNumber))
                        {
                            missingSaleOrder.Add(refNumber);
                        }
                    }
                    string orderMissingMessage = string.Join(" | ", missingSaleOrder);
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Code = 400,
                        Message = $"SaleOrders: {orderMissingMessage} not found",
                    };
                }

                foreach (var saleOrderIndb in saleOrderListIndb)
                {
                     if (saleOrderIndb.Status == SO_SaleOrderStatusConst.OPEN && !saleOrderIndb.IsDirect)
                    {
                        saleOrderIndb.Status = SO_SaleOrderStatusConst.WAITNGSHIPPING;
                    }
                    saleOrderIndb.IsPrintedDeliveryNote = true;
                    saleOrderIndb.PrintedDeliveryNoteCount += 1;
                    saleOrderIndb.LastedDeliveryNotePrintDate = DateTime.Now;
                    saleOrderIndb.UpdatedBy = username;
                    saleOrderIndb.UpdatedDate = DateTime.Now;
                    _orderInformationsRepository.UpdateUnSaved(saleOrderIndb, _schemaName);

                    if (saleOrderIndb.Status != SO_SaleOrderStatusConst.DRAFT)
                    {
                        var _osStatus = new ODMappingOrderStatus();
                        if (!string.IsNullOrWhiteSpace(saleOrderIndb.External_OrdNBR)
                            && saleOrderIndb.Source == SO_SOURCE_CONST.ONESHOP)
                        {
                            _osStatus = await _orderStatusHisService.HandleOSMappingStatus(saleOrderIndb.Status);
                        }
                        else {
                            _osStatus = null;
                        }

                        saleOrderIndb.OSStatus = _osStatus?.OneShopOrderStatus;

                        OsorderStatusHistory hisStatusNew = new();
                        hisStatusNew.OrderRefNumber = saleOrderIndb.OrderRefNumber;
                        hisStatusNew.ExternalOrdNbr = saleOrderIndb.External_OrdNBR;
                        hisStatusNew.OrderDate = saleOrderIndb.OrderDate;
                        hisStatusNew.DistributorCode = _distributorCode;
                        hisStatusNew.Sostatus = saleOrderIndb.Status;
                        hisStatusNew.SOStatusName = _osStatus?.SaleOrderStatusName;
                        hisStatusNew.CreatedBy = username;
                        hisStatusNew.OutletCode = saleOrderIndb.OSOutletCode;
                        hisStatusNew.OneShopStatus = _osStatus?.OneShopOrderStatus;
                        hisStatusNew.OneShopStatusName = _osStatus?.OneShopOrderStatusName;
                        BaseResultModel resultSaveStatusHis = await _orderStatusHisService.SaveStatusHistory(hisStatusNew, false);
                        if (!resultSaveStatusHis.IsSuccess) return resultSaveStatusHis;
                    }
                }

                _orderInformationsRepository.Save(_schemaName);


                foreach (var saleOrderIndb in saleOrderListIndb)
                {
                    Serilog.Log.Information($"############ Chuẩn bị vào SendNotification : {saleOrderIndb.OSStatus} - {saleOrderIndb.Status}");
                    if (saleOrderIndb.OSStatus != null 
                        && saleOrderIndb.Status != SO_SaleOrderStatusConst.WAITNGSHIPPING)
                    {
                        // Send notification
                        OSNotificationModel reqNoti = new();
                        reqNoti.External_OrdNBR = saleOrderIndb.External_OrdNBR;
                        reqNoti.OrderRefNumber = saleOrderIndb.OrderRefNumber;
                        reqNoti.OSStatus = saleOrderIndb.OSStatus;
                        reqNoti.SOStatus = saleOrderIndb.Status;
                        reqNoti.DistributorCode = saleOrderIndb.DistributorCode;
                        reqNoti.DistributorName = saleOrderIndb.DistributorName;
                        reqNoti.OutletCode = saleOrderIndb.OSOutletCode;
                        reqNoti.Purpose = OSNotificationPurpose.GetPurpose(saleOrderIndb.Status);

                        await _osNotifiService.SendNotification(reqNoti, token);
                    }
                }

                _orderInformationsRepository.Dispose(_schemaName);
                return new BaseResultModel
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "OK",
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }
        #endregion

        #region FirstTimeCustomer
        public async Task<BaseResultModel> CreateFTC(SO_FirstTimeCustomer model, string username)
        {
            try
            {
                //if (IsODSiteConstant)
                //{
                    model.OwnerType = OwnerTypeConstant.DISTRIBUTOR;
                    model.OwnerCode = _distributorCode;
                //}

                //Generate RefNumber
                var prefix = StringsHelper.GetPrefixYYM();
                var orderRefNumberIndb = await _firstTimeCustomerRepository.GetAllQueryable(null, null, null, _schemaName)
                    .Where(x => x.CustomerCode.Contains(prefix)).AsNoTracking().Select(x => x.CustomerCode).OrderByDescending(x => x).FirstOrDefaultAsync();
                var generatedNumber = StringsHelper.GennerateCodeWithYearMonthFormat(prefix, orderRefNumberIndb != null ? orderRefNumberIndb : null);

                model.Id = Guid.NewGuid();
                model.CreatedBy = username;
                model.UpdatedBy = null;
                model.CustomerCode = generatedNumber;
                model.CreatedDate = DateTime.Now;
                _firstTimeCustomerRepository.Add(model, _schemaName);
                _firstTimeCustomerRepository.Save(_schemaName);
                return new BaseResultModel
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "OK",
                    Data = model
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }
        public async Task<ResultModelWithObject<ListFTCModel>> SearchFTC(FTCEcoparams parameters)
        {
            try
            {
                parameters.DistributorCode = _distributorCode;

                if (parameters.PageNumber <= 0) parameters.PageNumber = 1;
                var query = _firstTimeCustomerRepository.GetAllQueryable(null, null, null, _schemaName)
                    .Where(x => x.DistributorCode == parameters.DistributorCode);

                if (parameters.Filter != null && parameters.Filter.Trim() != string.Empty && parameters.Filter.Trim() != "NA_EMPTY")
                {
                    //var optionsAssembly = ScriptOptions.Default.AddReferences(typeof(SO_FirstTimeCustomer).Assembly);
                    //var filterExpressionTemp = CSharpScript.EvaluateAsync<Func<SO_FirstTimeCustomer, bool>>(($"s=> {parameters.Filter}"), optionsAssembly);
                    //Func<SO_FirstTimeCustomer, bool> filterExpression = filterExpressionTemp.Result;
                    //var checkCondition = query.Where(filterExpression);
                    //res = checkCondition.OrderBy(x => x.CustomerCode).ToList();

                    var parameter = Expression.Parameter(typeof(SO_FirstTimeCustomer), "s");
                    var lambda = DynamicExpressionParser.ParseLambda(new[] { parameter }, typeof(bool), parameters.Filter);
                    query = query.Where((Func<SO_FirstTimeCustomer, bool>)lambda.Compile()).AsQueryable();
                }

                query = query.OrderBy(x => x.CustomerCode).AsQueryable();

                if (!string.IsNullOrWhiteSpace(parameters.SearchValue))
                {
                    query = query.Where(x =>
                        !string.IsNullOrWhiteSpace(x.PhoneNumber) == x.PhoneNumber.Contains(parameters.SearchValue) ||
                        !string.IsNullOrWhiteSpace(x.FullName) == x.FullName.Contains(parameters.SearchValue) ||
                        !string.IsNullOrWhiteSpace(x.BusinessAddress) == x.BusinessAddress.Contains(parameters.SearchValue));
                }

                if (parameters.IsDropdown)
                {
                    var page1 = PagedList<SO_FirstTimeCustomer>.ToPagedList(query.ToList(), 0, query.Count());
                    var reponse = new ListFTCModel { Items = page1 };
                    return new ResultModelWithObject<ListFTCModel>
                    {
                        IsSuccess = true,
                        Code = 200,
                        Message = "Success",
                        Data = reponse
                    };
                }
                var poTempPagged = PagedList<SO_FirstTimeCustomer>.ToPagedListQueryAble(query, (parameters.PageNumber - 1) * parameters.PageSize, parameters.PageSize);
                var repsonse = new ListFTCModel { Items = poTempPagged, MetaData = poTempPagged.MetaData };
                //return metadata
                return new ResultModelWithObject<ListFTCModel>
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Success",
                    Data = repsonse
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new ResultModelWithObject<ListFTCModel>
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }
        #endregion

        #region ADS
        public async Task<BaseResultModel> AdsReport(SaleOrderAdsParams parameters, string token)
        {
            try
            {
                var SOList = await CommonGetAllQueryable(new SaleOrderSearchParamsModel
                {
                    DistributorCode = parameters.DistributorCode,
                    IsDropdown = true,
                    FromDate = parameters.StartDate.Date,
                    ToDate = parameters.EndDate.Date
                });

                var selectedItems = SOList.Where(x => x.OrderInformation.Status == SO_SaleOrderStatusConst.DELIVERED || x.OrderInformation.Status == SO_SaleOrderStatusConst.PARTIALDELIVERED).Select(x => x.OrderItem).Where(x => x.ItemGroupCode == parameters.ItemGroupCode).ToList();
                int totalBaseQty = 0;
                decimal totalAmt = 0;
                foreach (var item in selectedItems)
                {
                    totalBaseQty += item.ShippedBaseQuantities;
                    totalAmt += item.Ord_Line_Amt;
                }

                return new BaseResultModel
                {
                    Code = 200,
                    IsSuccess = true,
                    Message = "OK",
                    Data = new
                    {
                        TotalQty = totalBaseQty,
                        TotalAmt = totalAmt,
                    }
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }
        #endregion

        public async Task<BaseResultModel> GetListWareHouseByCustomerId(SaleOrderSearchParamsModel parameters)
        {
            try
            {
                var wareHouseList = await _orderInformationsRepository.GetAllQueryable(null, null, null, _schemaName)
                    .Where(x => !string.IsNullOrWhiteSpace(x.CustomerId) && x.CustomerId == parameters.CustomerCode &&
                    (parameters.ListDistributor != null && parameters.ListDistributor.Count > 0 ? parameters.ListDistributor.Contains(x.DistributorCode) : x.DistributorCode == parameters.DistributorCode)).Select(x => x.WareHouseID).ToArrayAsync();
                return new BaseResultModel
                {
                    Data = wareHouseList.Distinct().ToList(),
                    IsSuccess = true,
                    Message = "OK",
                    Code = 200
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }

        public async Task<BaseResultModel> UpdateDeliveryResult(List<SaleOrderModel> models, string status, string username, string token)
        {
            _token = token;
            try
            {
                OrderListQueryModel parameters = new()
                {
                    DistributorCode = models.Select(x => x.DistributorCode).First(),
                    OrderRefNumber = models.Select(x => x.OrderRefNumber).ToList(),
                    Status = status
                };
                foreach (var order in models)
                {
                    //if (IsODSiteConstant)
                    //{
                        order.OwnerType = OwnerTypeConstant.DISTRIBUTOR;
                        order.OwnerCode = _distributorCode;
                        order.DistributorCode = _distributorCode;
                    //}

                    var existedOrder = await _orderInformationsRepository
                        .GetAllQueryable(x => x.OrderRefNumber == order.OrderRefNumber, null, null, _schemaName)
                        .AsNoTracking()
                        .FirstOrDefaultAsync();

                    if (existedOrder == null)
                    {
                        return new BaseResultModel
                        {
                            Code = 400,
                            Data = null,
                            Message = "Order Not found",
                            IsSuccess = true
                        };
                    }

                    if (existedOrder.Status == SO_SaleOrderStatusConst.DELIVERED ||
                        existedOrder.Status == SO_SaleOrderStatusConst.PARTIALDELIVERED ||
                        existedOrder.Status == SO_SaleOrderStatusConst.FAILED)
                    {
                        return new BaseResultModel
                        {
                            Code = 400,
                            Data = null,
                            Message = $"Delivery process of Order {order.OrderRefNumber}  already updated",
                            IsSuccess = true
                        };
                    }

                    List<INV_TransactionModel> transactionDatas = new();
                    order.Shipped_Extend_Amt = 0;
                    order.Shipped_Amt = 0;
                    order.Shipped_Qty = 0;
                    order.Shipped_SKUs = 0;
                    order.Shipped_Promotion_Amt = 0;
                    order.TotalVAT = 0;
                    string transactionType = "";
                    if (parameters.Status == SO_SaleOrderStatusConst.FAILED)
                    {
                        if (order.Status == SO_SaleOrderStatusConst.SHIPPING)
                        {
                            transactionType = INV_TransactionType.SO_PICKING_FAILED;
                        }
                        else
                        {
                            transactionType = INV_TransactionType.SO_WAITING_FAILED;
                        }
                    }
                    else if (parameters.Status == SO_SaleOrderStatusConst.DELIVERED || parameters.Status == SO_SaleOrderStatusConst.PARTIALDELIVERED)
                    {
                        if (order.Status == SO_SaleOrderStatusConst.SHIPPING)
                        {
                            transactionType = INV_TransactionType.SO_SHIPPED;
                        }
                        else
                        {
                            transactionType = INV_TransactionType.SO_SHIPPED_NOPICKING;
                        }
                    }
                    foreach (var item in order.OrderItems.Where(x => !x.IsDeleted && (x.ItemCode != null || x.IsKit)).ToList())
                    {
                        //if (IsODSiteConstant)
                        //{
                            item.OwnerType = OwnerTypeConstant.DISTRIBUTOR;
                            item.OwnerCode = _distributorCode;
                        //}
                        /*Handle sửa thông tin item */
                        if (parameters.Status == SO_SaleOrderStatusConst.FAILED)
                        {
                            item.FailedQuantities = item.OrderQuantities;
                            item.FailedBaseQuantities = item.OrderBaseQuantities;
                            item.ShippedQuantities = 0;
                            item.ShippedBaseQuantities = 0;
                            item.Shipped_Line_Amt = 0;
                            item.Shipped_Line_Extend_Amt = 0;
                            item.Shipped_line_Disc_Amt = 0;
                            item.RemainQuantities = 0;
                            
                            // Trả ngân sách khi cập nhật đơn hàng failed
                            if (!string.IsNullOrEmpty(item.PromotionBudgetCode))
                            {
                                await HandleCancelBudgetSO(item, order, token);
                            }
                        }
                        else if (parameters.Status == SO_SaleOrderStatusConst.DELIVERED)
                        {
                            item.ShippedQuantities = item.OrderQuantities;
                            item.ShippedBaseQuantities = item.OrderBaseQuantities;
                            item.Shipped_Line_Amt = item.Ord_Line_Amt;
                            item.Shipped_Line_Extend_Amt = item.Ord_Line_Extend_Amt;
                            item.Shipped_line_Disc_Amt = item.Ord_line_Disc_Amt;
                            item.RemainQuantities = 0;

                            // Reuse logic theo hàm updateDeliveryResultV2
                            var salesPriceIncludeVaT = _caculateTaxService.GetSalesPriceIncludeVaT();
                            var shippedTax = new ShippedLineTax
                            {
                                disCountAmount = item.DisCountAmount,
                                shipped_Line_Amt = item.Shipped_Line_Amt,
                                vatValue = item.VatValue,
                                shipped_line_Disc_Amt = item.Shipped_line_Disc_Amt,
                                salespriceincludeVAT = salesPriceIncludeVaT
                            };
                            double Shipped_Line_TaxAfter_Amt, Shipped_Line_TaxBefore_Amt;
                            decimal Shipped_Line_Extend_Amt;
                            _caculateTaxService.CaculateShippingTax(shippedTax, out Shipped_Line_TaxAfter_Amt, out Shipped_Line_TaxBefore_Amt, out Shipped_Line_Extend_Amt);
                            item.Shipped_Line_TaxAfter_Amt = Shipped_Line_TaxAfter_Amt;
                            item.Shipped_Line_TaxBefore_Amt = Shipped_Line_TaxBefore_Amt;
                        }
                        else if (parameters.Status == SO_SaleOrderStatusConst.PARTIALDELIVERED)
                        {
                            item.ShippedBaseQuantities = item.ShippedQuantities * item.OrderBaseQuantities != 0 ? item.ShippedQuantities * item.OrderBaseQuantities / item.OrderQuantities : 0;
                            item.Shipped_Line_Amt = item.ShippedQuantities * item.Ord_Line_Amt != 0 ? item.ShippedQuantities * item.Ord_Line_Amt / item.OrderQuantities : 0;
                            item.Shipped_Line_Extend_Amt = item.ShippedQuantities * item.Ord_Line_Extend_Amt != 0 ? item.ShippedQuantities * item.Ord_Line_Extend_Amt / item.OrderQuantities : 0;
                            item.RemainQuantities = item.OrderBaseQuantities - item.ShippedBaseQuantities;

                            // Reuse logic theo hàm updateDeliveryResultV2
                            var salesPriceIncludeVaT = _caculateTaxService.GetSalesPriceIncludeVaT();
                            var shippedTax = new ShippedLineTax
                            {
                                disCountAmount = item.DisCountAmount,
                                shipped_Line_Amt = item.Shipped_Line_Amt,
                                vatValue = item.VatValue,
                                shipped_line_Disc_Amt = item.Shipped_line_Disc_Amt,
                                salespriceincludeVAT = salesPriceIncludeVaT
                            };
                            double Shipped_Line_TaxAfter_Amt, Shipped_Line_TaxBefore_Amt;
                            decimal Shipped_Line_Extend_Amt;
                            _caculateTaxService.CaculateShippingTax(shippedTax, out Shipped_Line_TaxAfter_Amt, out Shipped_Line_TaxBefore_Amt, out Shipped_Line_Extend_Amt);
                            item.Shipped_Line_TaxAfter_Amt = Shipped_Line_TaxAfter_Amt;
                            item.Shipped_Line_TaxBefore_Amt = Shipped_Line_TaxBefore_Amt;
                            item.Shipped_Line_Extend_Amt = Shipped_Line_Extend_Amt;
                        }

                        if (!item.IsDeleted && !(item.IsKit && item.ItemCode == null))
                        {
                            order.TotalVAT += item.VAT;
                            order.Shipped_Promotion_Amt += item.Shipped_line_Disc_Amt;
                            order.Shipped_Extend_Amt += item.Shipped_Line_Extend_Amt;
                            order.Shipped_Amt += item.Shipped_Line_Amt;
                            order.Shipped_Qty += item.ShippedBaseQuantities;
                            order.Shipped_SKUs += 1;
                            order.Shipped_TotalBeforeTax_Amt += item.Shipped_Line_TaxBefore_Amt;
                            order.Shipped_TotalAfterTax_Amt += item.Shipped_Line_TaxAfter_Amt;
                        }
                        item.ShippingBaseQuantities = 0;
                        item.ShippingQuantities = 0;
                        _orderItemsRepository.UpdateUnSaved(item, _schemaName);

                        //Xử lý transaction
                        if (!item.IsDeleted && !(item.IsKit && item.ItemCode == null) && !(!string.IsNullOrWhiteSpace(item.PromotionCode) && item.ItemCode == null))
                        {
                            transactionDatas.Add(new INV_TransactionModel
                            {
                                OrderCode = order.OrderRefNumber,
                                ItemId = item.ItemId,
                                ItemCode = item.ItemCode,
                                ItemDescription = item.ItemDescription,
                                Uom = item.UOM,
                                Quantity = parameters.Status == SO_SaleOrderStatusConst.FAILED ? item.FailedQuantities : item.ShippedQuantities, // số lượng cần đặt
                                BaseQuantity = parameters.Status == SO_SaleOrderStatusConst.FAILED ? item.FailedBaseQuantities : item.ShippedBaseQuantities, //base cua thằng tr
                                OrderBaseQuantity = parameters.Status == SO_SaleOrderStatusConst.FAILED ? item.FailedBaseQuantities : item.OrderBaseQuantities,
                                TransactionDate = DateTime.Now,
                                TransactionType = transactionType,
                                WareHouseCode = order.WareHouseID,
                                LocationCode = item.LocationID,
                                DistributorCode = order.DistributorCode,
                                DSACode = order.DSAID,
                                Description = order.Note
                            });
                        }
                    }

                    order.Status = status;
                    if (order.Ord_Qty == order.Shipped_Qty && status == SO_SaleOrderStatusConst.PARTIALDELIVERED)
                    {
                        order.Status = SO_SaleOrderStatusConst.DELIVERED;
                    }
                    order.CompleteDate = DateTime.Now;

                    var requestCusDisProgram = new
                    {
                        saleOrgCode = order.SalesOrgID,
                        sicCode = order.SIC_ID,
                        customerCode = order.CustomerId,
                        shiptoCode = order.CustomerShiptoID,
                        routeZoneCode = order.RouteZoneID,
                        dsaCode = order.DSAID,
                        branch = order.BranchId,
                        region = order.RegionId,
                        subRegion = order.SubRegionId,
                        area = order.AreaId,
                        subArea = order.SubAreaId,
                        distributorCode = order.DistributorCode
                    };
                    var cusDisprog = _clientService.CommonRequest<ResultModelWithObject<DiscountModel>>(CommonData.SystemUrlCode.ODTpAPI, $"/tpdiscount/getdiscountbycustomer", Method.POST, token, requestCusDisProgram);

                    if (cusDisprog.Data != null)
                    {
                        order.DiscountID = cusDisprog.Data.code;
                        var discountResult = new
                        {
                            discountCode = cusDisprog.Data.code,
                            discountLevelId = cusDisprog.Data.listDiscountStructureDetails.Select(x => x.id).FirstOrDefault(),
                            purchaseAmount = order.Shipped_Amt - order.Shipped_line_Disc_Amt
                        };
                        var discountamt = _clientService.CommonRequest<DiscountResultModel>(CommonData.SystemUrlCode.ODTpAPI, $"/tpdiscount/discountresult", Method.POST, token, discountResult);
                        order.Shipped_Disc_Amt = discountamt.discountAmount;
                    }
                    order.Shipped_Extend_Amt = order.Shipped_Extend_Amt - order.Shipped_Disc_Amt;

                    _orderInformationsRepository.UpdateUnSaved(order, _schemaName);

                    var transactionResult = await CommonInventransactionService(transactionDatas, _token);
                    if (!transactionResult.IsSuccess)
                    {
                        return transactionResult;
                    }
                    _orderInformationsRepository.Save(_schemaName);

                    if (order.Status != SO_SaleOrderStatusConst.DRAFT)
                    {
                        var _osStatus = new ODMappingOrderStatus();
                        if (!string.IsNullOrWhiteSpace(order.External_OrdNBR)
                            && order.Source == SO_SOURCE_CONST.ONESHOP)
                        {
                            _osStatus = await _orderStatusHisService.HandleOSMappingStatus(order.Status);
                        }
                        else {
                            _osStatus = null;
                        }

                        order.OSStatus = _osStatus?.OneShopOrderStatus;

                        OsorderStatusHistory hisStatusNew = new();
                        hisStatusNew.OrderRefNumber = order.OrderRefNumber;
                        hisStatusNew.ExternalOrdNbr = order.External_OrdNBR;
                        hisStatusNew.OrderDate = order.OrderDate;
                        hisStatusNew.DistributorCode = _distributorCode;
                        hisStatusNew.Sostatus = order.Status;
                        hisStatusNew.SOStatusName = _osStatus?.SaleOrderStatusName;
                        hisStatusNew.CreatedBy = username;
                        hisStatusNew.OutletCode = order.OSOutletCode;
                        hisStatusNew.OneShopStatus = _osStatus?.OneShopOrderStatus;
                        hisStatusNew.OneShopStatusName = _osStatus?.OneShopOrderStatusName;
                        BaseResultModel resultSaveStatusHis = await _orderStatusHisService.SaveStatusHistory(hisStatusNew);
                        if (!resultSaveStatusHis.IsSuccess) return resultSaveStatusHis;

                        Serilog.Log.Information($"############ Chuẩn bị vào SendNotification : {order.OSStatus} - {order.Status}");
                        if (order.OSStatus != null)
                        {
                            // Send notification
                            OSNotificationModel reqNoti = new();
                            reqNoti.External_OrdNBR = order.External_OrdNBR;
                            reqNoti.OrderRefNumber = order.OrderRefNumber;
                            reqNoti.OSStatus = order.OSStatus;
                            reqNoti.SOStatus = order.Status;
                            reqNoti.DistributorCode = order.DistributorCode;
                            reqNoti.DistributorName = order.DistributorName;
                            reqNoti.OutletCode = order.OSOutletCode;
                            reqNoti.Purpose = OSNotificationPurpose.GetPurpose(order.Status);

                            await _osNotifiService.SendNotification(reqNoti, token);
                        }
                    }
                }

                return new BaseResultModel
                {
                    Code = 200,
                    Data = null,
                    Message = "OK",
                    IsSuccess = true
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }

        //Khoa enhance test performance
        public async Task<BaseResultModel> UpdateDeliveryResultv2(List<string> OrderRefNumberList, string Status, string ReasonCode, string DistributorCode, string UserName, string Token)
        {
            try
            {
                _token = Token;

                OrderListQueryModel parameters = new()
                {
                    DistributorCode = DistributorCode,
                    OrderRefNumber = OrderRefNumberList,
                    Status = Status
                };

                foreach (var orderRefNumber in parameters.OrderRefNumber)
                {
                    var orderInfomation = await _orderInformationsRepository.GetAllQueryable(x => x.OrderRefNumber == orderRefNumber, null, null, _schemaName).AsNoTracking().FirstOrDefaultAsync();
                    if (orderInfomation == null)
                    {
                        return new BaseResultModel
                        {
                            Code = 400,
                            Data = null,
                            Message = "Order Not found",
                            IsSuccess = true
                        };
                    }
                    if (orderInfomation.Status == SO_SaleOrderStatusConst.DELIVERED || orderInfomation.Status == SO_SaleOrderStatusConst.PARTIALDELIVERED || orderInfomation.Status == SO_SaleOrderStatusConst.FAILED)
                    {
                        return new BaseResultModel
                        {
                            Code = 400,
                            Data = null,
                            Message = $"Delivery process of Order {orderInfomation.OrderRefNumber}  already updated",
                            IsSuccess = true
                        };
                    }

                    orderInfomation.OwnerType = OwnerTypeConstant.DISTRIBUTOR;
                    orderInfomation.OwnerCode = _distributorCode;
                    orderInfomation.DistributorCode = _distributorCode;
                    orderInfomation.Shipped_Extend_Amt = 0;
                    orderInfomation.Shipped_Amt = 0;
                    orderInfomation.Shipped_Qty = 0;
                    orderInfomation.Shipped_SKUs = 0;
                    orderInfomation.Shipped_Promotion_Amt = 0;
                    orderInfomation.TotalVAT = 0;
                    orderInfomation.CompleteDate = DateTime.Now;
                    //orderInfomation.Status = parameters.Status;
                    string transactionType = "";
                    if (parameters.Status == SO_SaleOrderStatusConst.FAILED)
                    {
                        orderInfomation.ReasonCode = !string.IsNullOrWhiteSpace(ReasonCode) ? ReasonCode : null;
                        transactionType = transactionType = orderInfomation.Status == SO_SaleOrderStatusConst.SHIPPING ? INV_TransactionType.SO_PICKING_FAILED : INV_TransactionType.SO_WAITING_FAILED;
                    }
                    else if (parameters.Status == SO_SaleOrderStatusConst.DELIVERED || parameters.Status == SO_SaleOrderStatusConst.PARTIALDELIVERED)
                    {
                        transactionType = orderInfomation.Status == SO_SaleOrderStatusConst.SHIPPING ? INV_TransactionType.SO_SHIPPED : INV_TransactionType.SO_SHIPPED_NOPICKING;
                    }
                    
                    if (orderInfomation.Ord_Qty == orderInfomation.Shipped_Qty && parameters.Status == SO_SaleOrderStatusConst.PARTIALDELIVERED)
                    {
                        orderInfomation.Status = SO_SaleOrderStatusConst.DELIVERED;
                    }
                    else
                    {
                        orderInfomation.Status = parameters.Status;
                    }

                    List<SO_OrderItems> orderItems = _orderItemsRepository.GetAllQueryable(x => x.OrderRefNumber.Equals(orderInfomation.OrderRefNumber) && !x.IsDeleted && (x.ItemCode != null || x.IsKit), null, null, _schemaName).AsNoTracking().ToList();
                    if (orderItems == null || orderItems.Count == 0)
                    {
                        return new BaseResultModel
                        {
                            Code = 400,
                            Data = null,
                            Message = $"Order {orderInfomation.OrderRefNumber} is not found items.",
                            IsSuccess = true
                        };
                    }
                    List<INV_TransactionModel> transactionDatas = new();

                    foreach (var item in orderItems)
                    {
                        item.OwnerType = OwnerTypeConstant.DISTRIBUTOR;
                        item.OwnerCode = _distributorCode;

                        /*Handle sửa thông tin item */
                        if (parameters.Status == SO_SaleOrderStatusConst.FAILED)
                        {
                            item.FailedQuantities = item.OrderQuantities;
                            item.FailedBaseQuantities = item.OrderBaseQuantities;
                            item.ShippedQuantities = 0;
                            item.ShippedBaseQuantities = 0;
                            item.Shipped_Line_Amt = 0;
                            item.Shipped_Line_Extend_Amt = 0;
                            item.Shipped_line_Disc_Amt = 0;
                            item.RemainQuantities = 0;
                            
                            // Trả ngân sách khi cập nhật đơn hàng failed
                            if (!string.IsNullOrEmpty(item.PromotionBudgetCode))
                            {
                                await HandleCancelBudgetSO(item, orderInfomation, Token);
                            }
                        }
                        else if (parameters.Status == SO_SaleOrderStatusConst.DELIVERED)
                        {
                            item.ShippedQuantities = item.OrderQuantities;
                            item.ShippedBaseQuantities = item.OrderBaseQuantities;
                            item.Shipped_Line_Amt = item.Ord_Line_Amt;
                            //item.Shipped_Line_Extend_Amt = item.Ord_Line_Extend_Amt;
                            item.Shipped_line_Disc_Amt = item.Ord_line_Disc_Amt;
                            item.RemainQuantities = 0;
                            var salesPriceIncludeVaT = _caculateTaxService.GetSalesPriceIncludeVaT();
                            var shippedTax = new ShippedLineTax
                            {
                                disCountAmount = item.DisCountAmount,
                                shipped_Line_Amt = item.Shipped_Line_Amt,
                                vatValue = item.VatValue,
                                shipped_line_Disc_Amt = item.Shipped_line_Disc_Amt,
                                salespriceincludeVAT = salesPriceIncludeVaT
                            };
                            double Shipped_Line_TaxAfter_Amt, Shipped_Line_TaxBefore_Amt;
                            decimal Shipped_Line_Extend_Amt;
                            _caculateTaxService.CaculateShippingTax(shippedTax, out Shipped_Line_TaxAfter_Amt, out Shipped_Line_TaxBefore_Amt, out Shipped_Line_Extend_Amt);
                            item.Shipped_Line_TaxAfter_Amt = Shipped_Line_TaxAfter_Amt;
                            item.Shipped_Line_TaxBefore_Amt = Shipped_Line_TaxBefore_Amt;
                            item.Shipped_Line_Extend_Amt = Shipped_Line_Extend_Amt;
                        }
                        else if (parameters.Status == SO_SaleOrderStatusConst.PARTIALDELIVERED)
                        {
                            item.ShippedBaseQuantities = item.ShippedQuantities * item.OrderBaseQuantities != 0 ? item.ShippedQuantities * item.OrderBaseQuantities / item.OrderQuantities : 0;
                            item.Shipped_Line_Amt = item.ShippedQuantities * item.Ord_Line_Amt != 0 ? item.ShippedQuantities * item.Ord_Line_Amt / item.OrderQuantities : 0;
                            item.Shipped_Line_Extend_Amt = item.ShippedQuantities * item.Ord_Line_Extend_Amt != 0 ? item.ShippedQuantities * item.Ord_Line_Extend_Amt / item.OrderQuantities : 0;
                            item.RemainQuantities = item.OrderBaseQuantities - item.ShippedBaseQuantities;
                            var salesPriceIncludeVaT = _caculateTaxService.GetSalesPriceIncludeVaT();
                            var shippedTax = new ShippedLineTax
                            {
                                disCountAmount = item.DisCountAmount,
                                shipped_Line_Amt = item.Shipped_Line_Amt,
                                vatValue = item.VatValue,
                                shipped_line_Disc_Amt = item.Shipped_line_Disc_Amt,
                                salespriceincludeVAT = salesPriceIncludeVaT
                            };
                            double Shipped_Line_TaxAfter_Amt, Shipped_Line_TaxBefore_Amt;
                            decimal Shipped_Line_Extend_Amt;
                            _caculateTaxService.CaculateShippingTax(shippedTax, out Shipped_Line_TaxAfter_Amt, out Shipped_Line_TaxBefore_Amt, out Shipped_Line_Extend_Amt);
                            item.Shipped_Line_TaxAfter_Amt = Shipped_Line_TaxAfter_Amt;
                            item.Shipped_Line_TaxBefore_Amt = Shipped_Line_TaxBefore_Amt;
                            item.Shipped_Line_Extend_Amt = Shipped_Line_Extend_Amt;
                        }

                        if (!item.IsDeleted && !(item.IsKit && item.ItemCode == null))
                        {
                            orderInfomation.TotalVAT += item.VAT;
                            orderInfomation.Shipped_Promotion_Amt += item.Shipped_line_Disc_Amt;
                            orderInfomation.Shipped_Extend_Amt += item.Shipped_Line_Extend_Amt;
                            orderInfomation.Shipped_Amt += item.Shipped_Line_Amt;
                            orderInfomation.Shipped_Qty += item.ShippedBaseQuantities;
                            orderInfomation.Shipped_SKUs += 1;
                            orderInfomation.Shipped_TotalAfterTax_Amt += item.Shipped_Line_TaxAfter_Amt;
                            orderInfomation.Shipped_TotalBeforeTax_Amt += item.Shipped_Line_TaxBefore_Amt;
                        }

                        item.ShippingBaseQuantities = 0;
                        item.ShippingQuantities = 0;
                        _orderItemsRepository.UpdateUnSaved(item, _schemaName);

                        //Xử lý transaction
                        if (!item.IsDeleted && !(item.IsKit && item.ItemCode == null) && !(!string.IsNullOrWhiteSpace(item.PromotionCode) && item.ItemCode == null))
                        {
                            transactionDatas.Add(new INV_TransactionModel
                            {
                                OrderCode = orderInfomation.OrderRefNumber,
                                ItemId = item.ItemId,
                                ItemCode = item.ItemCode,
                                ItemDescription = item.ItemDescription,
                                Uom = item.UOM,
                                Quantity = parameters.Status == SO_SaleOrderStatusConst.FAILED ? item.FailedQuantities : item.ShippedQuantities, // số lượng cần đặt
                                BaseQuantity = parameters.Status == SO_SaleOrderStatusConst.FAILED ? item.FailedBaseQuantities : item.ShippedBaseQuantities, //base cua thằng tr
                                OrderBaseQuantity = parameters.Status == SO_SaleOrderStatusConst.FAILED ? item.FailedBaseQuantities : item.OrderBaseQuantities,
                                TransactionDate = DateTime.Now,
                                TransactionType = transactionType,
                                WareHouseCode = orderInfomation.WareHouseID,
                                LocationCode = item.LocationID,
                                DistributorCode = orderInfomation.DistributorCode,
                                DSACode = orderInfomation.DSAID,
                                Description = orderInfomation.Note
                            });
                        }
                    }

                    var requestCusDisProgram = new
                    {
                        saleOrgCode = orderInfomation.SalesOrgID,
                        sicCode = orderInfomation.SIC_ID,
                        customerCode = orderInfomation.CustomerId,
                        shiptoCode = orderInfomation.CustomerShiptoID,
                        routeZoneCode = orderInfomation.RouteZoneID,
                        dsaCode = orderInfomation.DSAID,
                        branch = orderInfomation.BranchId,
                        region = orderInfomation.RegionId,
                        subRegion = orderInfomation.SubRegionId,
                        area = orderInfomation.AreaId,
                        subArea = orderInfomation.SubAreaId,
                        distributorCode = orderInfomation.DistributorCode
                    };
                    var cusDisprog = _clientService.CommonRequest<ResultModelWithObject<DiscountModel>>(CommonData.SystemUrlCode.ODTpAPI, $"/tpdiscount/getdiscountbycustomer", Method.POST, Token, requestCusDisProgram);

                    if (cusDisprog.Data != null)
                    {
                        orderInfomation.DiscountID = cusDisprog.Data.code;
                        var discountResult = new
                        {
                            discountCode = cusDisprog.Data.code,
                            discountLevelId = cusDisprog.Data.listDiscountStructureDetails.Select(x => x.id).FirstOrDefault(),
                            purchaseAmount = orderInfomation.Shipped_Amt - orderInfomation.Shipped_line_Disc_Amt
                        };
                        var discountamt = _clientService.CommonRequest<DiscountResultModel>(CommonData.SystemUrlCode.ODTpAPI, $"/tpdiscount/discountresult", Method.POST, Token, discountResult);
                        orderInfomation.Shipped_Disc_Amt = discountamt.discountAmount;
                    }
                    orderInfomation.Shipped_Extend_Amt = orderInfomation.Shipped_Extend_Amt - orderInfomation.Shipped_Disc_Amt;

                    _orderInformationsRepository.UpdateUnSaved(orderInfomation, _schemaName);

                    var transactionResult = await CommonInventransactionService(transactionDatas, _token);
                    if (!transactionResult.IsSuccess)
                    {
                        return transactionResult;
                    }
                    _orderInformationsRepository.Save(_schemaName);

                    if (orderInfomation.Status != SO_SaleOrderStatusConst.DRAFT)
                    {
                        var _osStatus = new ODMappingOrderStatus();
                        if (!string.IsNullOrWhiteSpace(orderInfomation.External_OrdNBR) && orderInfomation.Source == SO_SOURCE_CONST.ONESHOP)
                        {
                            _osStatus = await _orderStatusHisService.HandleOSMappingStatus(orderInfomation.Status);
                        }
                        else
                        {
                            _osStatus = null;
                        }

                        orderInfomation.OSStatus = _osStatus?.OneShopOrderStatus;

                        OsorderStatusHistory hisStatusNew = new();
                        hisStatusNew.OrderRefNumber = orderInfomation.OrderRefNumber;
                        hisStatusNew.ExternalOrdNbr = orderInfomation.External_OrdNBR;
                        hisStatusNew.OrderDate = orderInfomation.OrderDate;
                        hisStatusNew.DistributorCode = _distributorCode;
                        hisStatusNew.Sostatus = orderInfomation.Status;
                        hisStatusNew.SOStatusName = _osStatus?.SaleOrderStatusName;
                        hisStatusNew.CreatedBy = UserName;
                        hisStatusNew.OutletCode = orderInfomation.OSOutletCode;
                        hisStatusNew.OneShopStatus = _osStatus?.OneShopOrderStatus;
                        hisStatusNew.OneShopStatusName = _osStatus?.OneShopOrderStatusName;
                        BaseResultModel resultSaveStatusHis = await _orderStatusHisService.SaveStatusHistory(hisStatusNew);
                        if (!resultSaveStatusHis.IsSuccess) return resultSaveStatusHis;

                        //Serilog.Log.Information($"############ Chuẩn bị vào SendNotification : {order.OSStatus} - {order.Status}");
                        if (orderInfomation.OSStatus != null)
                        {
                            // Send notification
                            OSNotificationModel reqNoti = new();
                            reqNoti.External_OrdNBR = orderInfomation.External_OrdNBR;
                            reqNoti.OrderRefNumber = orderInfomation.OrderRefNumber;
                            reqNoti.OSStatus = orderInfomation.OSStatus;
                            reqNoti.SOStatus = orderInfomation.Status;
                            reqNoti.DistributorCode = orderInfomation.DistributorCode;
                            reqNoti.DistributorName = orderInfomation.DistributorName;
                            reqNoti.OutletCode = orderInfomation.OSOutletCode;
                            reqNoti.Purpose = OSNotificationPurpose.GetPurpose(orderInfomation.Status);

                            await _osNotifiService.SendNotification(reqNoti, Token);
                        }
                    }
                }

                return new BaseResultModel
                {
                    Code = 200,
                    Data = null,
                    Message = "OK",
                    IsSuccess = true
                };
            }
            catch (Exception ex)
            {
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }

        }

        public async Task<IEnumerable<SaleOrderBaseModel>> GetAllQueryableByUser(SaleOrderSearchParamsModel parameters, string username, string token)
        {
            var hODataModel = await HOGetDetail(username, token);
            IEnumerable<SaleOrderBaseModel> saleOrders;
            if (!hODataModel.IsHO)
            {
                hODataModel.Distributors = new List<string>() { username };
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(parameters.DistributorCode))
                {
                    if (hODataModel.Distributors.Any(x => x != null && x == parameters.DistributorCode))
                    {
                        hODataModel.Distributors = new List<string>() { parameters.DistributorCode };
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            saleOrders = await CommonGetAllQueryableWODistributor(parameters, hODataModel.Distributors);
            return saleOrders;
        }

        public async Task<BaseResultModel> GetSaleRepIdByDistributorCode(string DistributorCode)
        {
            try
            {
                var result = await _orderInformationsRepository.GetAllQueryable(x => !x.IsDeleted && !string.IsNullOrWhiteSpace(x.SalesRepID) &&
                    !string.IsNullOrWhiteSpace(x.DistributorCode) && x.DistributorCode == DistributorCode, null, null, _schemaName)
                        .Select(x => x.SalesRepID)
                    .AsNoTracking().Distinct().ToListAsync();

                _orderInformationsRepository.Dispose(_schemaName);
                return new BaseResultModel
                {
                    Code = 200,
                    Message = "OK",
                    IsSuccess = true,
                    Data = result
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }

        public async Task HandleCancelBudgetFFA(FfasoOrderItem item, FfasoOrderInformation order, string token)
        {
            try
            {
                int _budgetBooked = item.BudgetBooked.HasValue ? item.BudgetBooked.Value : 0;
                if (_budgetBooked > 0 && !string.IsNullOrEmpty(item.BudgetCode))
                {
                    // Book budget
                    var budgetRequest = new BudgetRequestModel();
                    budgetRequest.budgetCode = item.BudgetCode;
                    budgetRequest.budgetType = null;
                    budgetRequest.customerCode = order.CustomerId;
                    budgetRequest.customerShipTo = order.CustomerShiptoID;
                    budgetRequest.saleOrg = order.SalesOrgID;
                    budgetRequest.budgetAllocationLevel = null;
                    budgetRequest.budgetBook = -(_budgetBooked); // Trả số đã book
                    budgetRequest.promotionCode = item.PromotionCode;
                    budgetRequest.promotionLevel = item.PromotionLevelCode;
                    budgetRequest.routeZoneCode = order.RouteZoneID;
                    budgetRequest.dsaCode = order.DSAID;
                    budgetRequest.subAreaCode = order.SubAreaId;
                    budgetRequest.areaCode = order.AreaId;
                    budgetRequest.subRegionCode = order.SubRegionId;
                    budgetRequest.regionCode = order.RegionId;
                    budgetRequest.branchCode = order.BranchId;
                    budgetRequest.nationwideCode = "VN";
                    budgetRequest.salesOrgCode = order.SalesOrgID;
                    budgetRequest.referalCode = order.External_OrdNBR;
                    budgetRequest.distributorCode = _distributorCode;

                    _clientService.CommonRequest<ResultModelWithObject<BudgetResponseModel>>(
                        CommonData.SystemUrlCode.ODTpAPI, $"external_checkbudget/checkbudget", 
                        Method.POST, 
                        token, 
                        budgetRequest, 
                        true);
                    
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
            }
        }

        public async Task HandleCancelBudgetSO(SO_OrderItems item, SO_OrderInformations order, string token)
        {
            try
            {
                int _budgetBooked = item.PromotionBudgetQuantities.HasValue ? item.PromotionBudgetQuantities.Value : 0;
                if (_budgetBooked > 0 && !string.IsNullOrEmpty(item.PromotionBudgetCode))
                {
                    // Book budget
                    var budgetRequest = new BudgetRequestModel();
                    budgetRequest.budgetCode = item.PromotionBudgetCode;
                    budgetRequest.budgetType = null;
                    budgetRequest.customerCode = order.CustomerId;
                    budgetRequest.customerShipTo = order.CustomerShiptoID;
                    budgetRequest.saleOrg = order.SalesOrgID;
                    budgetRequest.budgetAllocationLevel = null;
                    budgetRequest.budgetBook = -(_budgetBooked); // Trả số đã book
                    budgetRequest.promotionCode = item.PromotionCode;
                    budgetRequest.promotionLevel = item.PromotionLevel;
                    budgetRequest.routeZoneCode = order.RouteZoneID;
                    budgetRequest.dsaCode = order.DSAID;
                    budgetRequest.subAreaCode = order.SubAreaId;
                    budgetRequest.areaCode = order.AreaId;
                    budgetRequest.subRegionCode = order.SubRegionId;
                    budgetRequest.regionCode = order.RegionId;
                    budgetRequest.branchCode = order.BranchId;
                    budgetRequest.nationwideCode = "VN";
                    budgetRequest.salesOrgCode = order.SalesOrgID;
                    budgetRequest.referalCode = order.OrderRefNumber;
                    budgetRequest.distributorCode = _distributorCode;

                    _clientService.CommonRequest<ResultModelWithObject<BudgetResponseModel>>(
                        CommonData.SystemUrlCode.ODTpAPI, $"external_checkbudget/checkbudget",
                        Method.POST,
                        token,
                        budgetRequest,
                        true);

                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
            }
        }

        public async Task<BaseResultModel> ProcessPendingDataTrans(OrderPendingTransModel model, string username, string token)
        {
            _token = token;
            try
            {
                var parameters = new SaleOrderSearchParamsModel
                {
                    StatusFilter = model.Detail.Select(x => x.FromStatus).ToList(),
                    OrderDate = model.BaselineDate
                };
                var searchResult = await (from header in _orderInformationsRepository.GetAllQueryable(null, null, null, _schemaName).AsNoTracking()
                                          join detail in _orderItemsRepository.GetAllQueryable(null, null, null, _schemaName).AsNoTracking() on header.OrderRefNumber equals detail.OrderRefNumber into data
                                          from detail in data.DefaultIfEmpty()
                                          where
                                          (parameters.StatusFilter != null && parameters.StatusFilter.Count > 0 ? parameters.StatusFilter.Contains(header.Status) : true) &&
                                          (parameters.OrderDate.HasValue ? parameters.OrderDate.Value.Date == header.OrderDate.Date : true) &&
                                          !header.IsDeleted
                                          select new SaleOrderBaseModel
                                          {
                                              OrderInformation = header,
                                              OrderItem = detail
                                          }).ToListAsync();

                //searchResult = searchResult.Where(x => x.OrderInformation != null && x.OrderInformation.OrderRefNumber == "25600056").ToList();
                if (searchResult != null && searchResult.Count > 0)
                {
                    IEnumerable<SaleOrderModel> searchList = groupSoOrderInformation(searchResult);
                    foreach (var item in model.Detail)
                    {
                        var listSO = searchList.Where(x => x.Status == item.FromStatus).ToList();

                        string fromStatus = item.FromStatus;
                        string toStatus = item.ToStatus;

                        foreach (var salesOrder in listSO)
                        {
                            string _statusClone = salesOrder.Status;
                            List<INV_TransactionModel> transactionReqData = new List<INV_TransactionModel>();
                            string invenTransactionType = null;
                            if (fromStatus == SO_SaleOrderStatusConst.SHIPPING && (toStatus == SO_SaleOrderStatusConst.DELIVERED || toStatus == SO_SaleOrderStatusConst.PARTIALDELIVERED))
                            {
                                //SO_SHIPPED
                                invenTransactionType = INV_TransactionType.SO_SHIPPED;
                            }
                            else if (fromStatus == SO_SaleOrderStatusConst.WAITNGSHIPPING && (toStatus == SO_SaleOrderStatusConst.DELIVERED || toStatus == SO_SaleOrderStatusConst.PARTIALDELIVERED))
                            {
                                //SO_SHIPPED_NOPICKING
                                invenTransactionType = INV_TransactionType.SO_SHIPPED_NOPICKING;
                            }
                            else if (fromStatus == SO_SaleOrderStatusConst.OPEN && toStatus == SO_SaleOrderStatusConst.DELIVERED)
                            {
                                //SO_SHIPPED_DIRECT
                                invenTransactionType = INV_TransactionType.SO_SHIPPED_DIRECT;
                            }
                            else if ((fromStatus == SO_SaleOrderStatusConst.DELIVERED || fromStatus == SO_SaleOrderStatusConst.PARTIALDELIVERED) && toStatus == SO_SaleOrderStatusConst.CANCEL)
                            {
                                //SO_CL
                                salesOrder.ReasonCode = BL_CANCEL_REASON_CODE;
                                invenTransactionType = INV_TransactionType.SO_CL;
                            }
                            else if (fromStatus == SO_SaleOrderStatusConst.OPEN && toStatus == SO_SaleOrderStatusConst.CANCEL)
                            {
                                //SO_BOOKED_CANCEL
                                salesOrder.ReasonCode = BL_CANCEL_REASON_CODE;
                                invenTransactionType = INV_TransactionType.SO_BOOKED_CANCEL;
                            }
                            else if (fromStatus == SO_SaleOrderStatusConst.WAITNGSHIPPING && toStatus == SO_SaleOrderStatusConst.FAILED)
                            {
                                //SO_WAITING_FAILED
                                invenTransactionType = INV_TransactionType.SO_WAITING_FAILED;
                            }
                            else if (fromStatus == SO_SaleOrderStatusConst.SHIPPING && toStatus == SO_SaleOrderStatusConst.FAILED)
                            {
                                //SO_PICKING_FAILED
                                invenTransactionType = INV_TransactionType.SO_PICKING_FAILED;
                            }

                            if (toStatus == SO_SaleOrderStatusConst.CANCEL)
                            {
                                salesOrder.CancelDate = DateTime.Now;
                                salesOrder.Status = SO_SaleOrderStatusConst.CANCEL;
                            }

                            foreach (var orderItem in salesOrder.OrderItems.Where(x => !x.IsDeleted && (x.ItemCode != null || x.IsKit)).ToList())
                            {
                                if (toStatus == SO_SaleOrderStatusConst.FAILED)
                                {
                                    orderItem.FailedQuantities = orderItem.OrderQuantities;
                                    orderItem.FailedBaseQuantities = orderItem.OrderBaseQuantities;
                                    orderItem.ShippedQuantities = 0;
                                    orderItem.ShippedBaseQuantities = 0;
                                    orderItem.Shipped_Line_Amt = 0;
                                    orderItem.Shipped_Line_Extend_Amt = 0;
                                    orderItem.Shipped_line_Disc_Amt = 0;
                                    orderItem.RemainQuantities = 0;
                                }
                                else if (toStatus == SO_SaleOrderStatusConst.DELIVERED)
                                {
                                    orderItem.ShippedQuantities = orderItem.OrderQuantities;
                                    orderItem.ShippedBaseQuantities = orderItem.OrderBaseQuantities;
                                    orderItem.Shipped_Line_Amt = orderItem.Ord_Line_Amt;
                                    orderItem.Shipped_Line_Extend_Amt = orderItem.Ord_Line_Extend_Amt;
                                    orderItem.Shipped_line_Disc_Amt = orderItem.Ord_line_Disc_Amt;
                                    orderItem.RemainQuantities = 0;
                                }
                                else if (toStatus == SO_SaleOrderStatusConst.PARTIALDELIVERED)
                                {
                                    orderItem.ShippedBaseQuantities = orderItem.ShippedQuantities * orderItem.OrderBaseQuantities != 0 ? orderItem.ShippedQuantities * orderItem.OrderBaseQuantities / orderItem.OrderQuantities : 0;
                                    orderItem.Shipped_Line_Amt = orderItem.ShippedQuantities * orderItem.Ord_Line_Amt != 0 ? orderItem.ShippedQuantities * orderItem.Ord_Line_Amt / orderItem.OrderQuantities : 0;
                                    orderItem.Shipped_Line_Extend_Amt = orderItem.ShippedQuantities * orderItem.Ord_Line_Extend_Amt != 0 ? orderItem.ShippedQuantities * orderItem.Ord_Line_Extend_Amt / orderItem.OrderQuantities : 0;
                                    orderItem.RemainQuantities = orderItem.OrderBaseQuantities - orderItem.ShippedBaseQuantities;
                                }

                                if (!(orderItem.IsKit && orderItem.ItemCode == null))
                                {
                                    salesOrder.Shipped_Promotion_Amt += orderItem.Shipped_line_Disc_Amt;
                                    salesOrder.Shipped_Extend_Amt += orderItem.Shipped_Line_Extend_Amt;
                                    salesOrder.Shipped_Amt += orderItem.Shipped_Line_Amt;
                                    salesOrder.Shipped_Qty += orderItem.ShippedBaseQuantities;
                                    salesOrder.Shipped_SKUs += 1;
                                }

                                // Trả budget
                                if (!string.IsNullOrEmpty(orderItem.PromotionBudgetCode)
                                    && (salesOrder.Status == SO_SaleOrderStatusConst.CANCEL
                                    || salesOrder.Status == SO_SaleOrderStatusConst.FAILED))
                                {
                                    await HandleCancelBudgetSO(orderItem, salesOrder, token);
                                }
                            }

                            // Handle transaction inventory
                            var transactionDataResult = await HandleTransactionPendingOrderData(salesOrder, invenTransactionType, _token);
                            var transactionResult = await CommonInventransactionService(transactionDataResult, _token);
                            if (!transactionResult.IsSuccess)
                            {
                                salesOrder.ErrorMessage += transactionResult.Message;
                                salesOrder.UpdatedDate = DateTime.Now;
                                salesOrder.CancelDate = null;
                                salesOrder.Status = _statusClone; // revert status
                                _orderInformationsRepository.UpdateUnSaved(salesOrder, _schemaName);
                                _orderInformationsRepository.Save(_schemaName);
                                continue;
                                //return new BaseResultModel
                                //{
                                //    Code = 400,
                                //    Message = transactionResult.Message,
                                //    IsSuccess = false,
                                //};
                            }

                            // Handle discount program
                            if (toStatus == SO_SaleOrderStatusConst.DELIVERED || toStatus == SO_SaleOrderStatusConst.PARTIALDELIVERED)
                            {
                                var requestCusDisProgram = new
                                {
                                    saleOrgCode = salesOrder.SalesOrgID,
                                    sicCode = salesOrder.SIC_ID,
                                    customerCode = salesOrder.CustomerId,
                                    shiptoCode = salesOrder.CustomerShiptoID,
                                    routeZoneCode = salesOrder.RouteZoneID,
                                    dsaCode = salesOrder.DSAID,
                                    branch = salesOrder.BranchId,
                                    region = salesOrder.RegionId,
                                    subRegion = salesOrder.SubRegionId,
                                    area = salesOrder.AreaId,
                                    subArea = salesOrder.SubAreaId,
                                    distributorCode = salesOrder.DistributorCode
                                };
                                var cusDisprog = _clientService.CommonRequest<ResultModelWithObject<DiscountModel>>(CommonData.SystemUrlCode.ODTpAPI, $"/tpdiscount/getdiscountbycustomer", Method.POST, token, requestCusDisProgram);

                                if (cusDisprog.Data != null)
                                {
                                    salesOrder.DiscountID = cusDisprog.Data.code;
                                    var discountResult = new
                                    {
                                        discountCode = cusDisprog.Data.code,
                                        discountLevelId = cusDisprog.Data.listDiscountStructureDetails.Select(x => x.id).FirstOrDefault(),
                                        purchaseAmount = salesOrder.Shipped_Amt - salesOrder.Shipped_line_Disc_Amt
                                    };
                                    var discountamt = _clientService.CommonRequest<DiscountResultModel>(CommonData.SystemUrlCode.ODTpAPI, $"/tpdiscount/discountresult", Method.POST, token, discountResult);
                                    salesOrder.Shipped_Disc_Amt = discountamt.discountAmount;

                                }
                                salesOrder.Shipped_Extend_Amt = salesOrder.Shipped_Extend_Amt - salesOrder.Shipped_Disc_Amt;
                            }

                            // Update status new
                            salesOrder.Status = item.ToStatus;
                            salesOrder.UpdatedBy = username;
                            salesOrder.UpdatedDate = DateTime.Now;

                            _orderInformationsRepository.UpdateUnSaved(salesOrder, _schemaName);
                            _orderItemsRepository.UpdateRange(salesOrder.OrderItems, _schemaName);
                            _orderInformationsRepository.Save(_schemaName);
                        }
                    }
                }


                //FFA flow
                var ffaOrder = await _ffasoOrderInformationRepository.GetAllQueryable(x => 
                    x.OrderDate.Value.Date <= model.BaselineDate.Date 
                    && x.Status != FFASOSTATUS.ImportSuccessfully 
                    && x.Status != FFASOSTATUS.CanCelImport
                    && x.OrderType != FFA_ORDER_TYPE.DirectOrder,
                    null, null, _schemaName)
                    .ToListAsync();

                foreach (var order in ffaOrder)
                {
                    bool _isCheckError = false;
                    // Get list transaction from INV
                    List<INV_InventoryTransaction> _listInvTransactionByVisitId = await _inventoryService.GetTransactionsByFfaVisitId(order.VisitID, order.OrderType);
                    if (_listInvTransactionByVisitId.Count > 0)
                    {
                        foreach (var item in _listInvTransactionByVisitId)
                        {
                            var resCancelBook = await _inventoryService.CancelBookedFFAOrder(item, username);
                            if (!resCancelBook.IsSuccess)
                            {
                                order.ErrorMessage = resCancelBook.Message;
                                _isCheckError = true;
                                break;
                                //return resCancelBook;
                            }
                        }
                    }

                    if (_isCheckError)
                    {
                        _ffasoOrderInformationRepository.UpdateUnSaved(order, _schemaName);
                        continue;
                    }

                    var ffaOrderItems = await _ffasoOrderItemRepository.GetAllQueryable(x => 
                        x.External_OrdNBR == order.External_OrdNBR 
                        && x.VisitId == order.VisitID 
                        && !string.IsNullOrEmpty(x.BudgetCode), null, null, _schemaName).ToListAsync();

                    foreach (var orderItem in ffaOrderItems)
                    {
                        await HandleCancelBudgetFFA(orderItem, order, token);
                    }
                    order.Status = FFASOSTATUS.CanCelImport;
                    order.ReasonCode = BL_CANCEL_REASON_CODE;
                    _ffasoOrderInformationRepository.UpdateUnSaved(order, _schemaName);
                    #region flow old
                    //var items = await _ffasoOrderItemRepository.GetAllQueryable(x => x.External_OrdNBR == order.External_OrdNBR && x.VisitId == order.VisitID, null, null, _schemaName).ToListAsync();
                    //List<BookingStockReqModel> models = new();
                    //var requestId = Guid.NewGuid();
                    //var modelMappingError = false;
                    //foreach (var item in items)
                    //{
                    //    try
                    //    {
                    //        if (item.OriginalOrderQtyBooked > 0)
                    //        {
                    //            if (item.LocationID == null)
                    //            {
                    //                _logger.LogError($"itemId {item.Id} has booked qty with null Location");
                    //                modelMappingError = true;
                    //                break;
                    //            }
                    //            models.Add(new BookingStockReqModel
                    //            {
                    //                LocationCode = item.LocationID,
                    //                DistributorCode = order.DistributorCode,
                    //                DistributorShiptoCode = order.WareHouseID,
                    //                AllocateType = item.AllocateType,
                    //                ItemCode = item.ItemCode,
                    //                Uom = item.BaseUnitCode,
                    //                OrderQuantities = -item.OriginalOrderQtyBooked.Value,
                    //                BaseUom = item.BaseUnitCode,
                    //                TransactionId = null,
                    //                VisitId = order.VisitID,
                    //                AllocationStock = false,
                    //                OrderBaseQuantities = -item.OriginalOrderQtyBooked.Value,
                    //                ForceConversion = false,
                    //                RequestDate = DateTime.Now,
                    //                RequestId = requestId,
                    //            });
                    //        }
                    //        continue;

                    //    }
                    //    catch (System.Exception ex)
                    //    {
                    //        _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                    //        modelMappingError = true;
                    //        break;
                    //    }

                    //}
                    //if (modelMappingError) continue;
                    //var bookingResult = await BookingStockItems(models, token);
                    //if (bookingResult == null || !bookingResult.IsSuccess)
                    //{
                    //    return new BaseResultModel
                    //    {
                    //        Code = 400,
                    //        Message = "return item of FFA order failed",
                    //        IsSuccess = false,
                    //    };
                    //}
                    //order.Status = FFASOSTATUS.CanCelImport;
                    //order.ReasonCode = BL_CANCEL_REASON_CODE;
                    //_ffasoOrderInformationRepository.UpdateUnSaved(order, _schemaName);
                    #endregion
                }
                // _ffasoOrderInformationRepository.UpdateRange(ffaOrder);
                _orderInformationsRepository.Save(_schemaName);
                return new BaseResultModel
                {
                    Code = 200,
                    Message = "OK",
                    IsSuccess = true,
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }
        //private List<SaleOrderModel> groupSoOrderInformation(List<SaleOrderBaseModel> searchResult)
        //{
        //    return searchResult.Where(x => x.OrderInformation != null && x.OrderItem != null)
        //        .GroupBy(x => x.OrderInformation.OrderRefNumber)
        //        .Select(x =>
        //        {
        //            var firstOrderInformation = x.First().OrderInformation;
        //            var listOrderItem = x.Select(d => d.OrderItem).ToList();
        //            return new SaleOrderModel
        //            {
        //                Id = firstOrderInformation.Id,
        //                OrderRefNumber = firstOrderInformation.OrderRefNumber,
        //                OrderDescription = firstOrderInformation.OrderDescription,
        //                ReferenceRefNbr = firstOrderInformation.ReferenceRefNbr,
        //                CancelNumber = firstOrderInformation.CancelNumber,
        //                ReasonCode = firstOrderInformation.ReasonCode,
        //                CancelDate = firstOrderInformation.CancelDate,
        //                NotInSubRoute = firstOrderInformation.NotInSubRoute,
        //                IsDirect = firstOrderInformation.IsDirect,
        //                OrderType = firstOrderInformation.OrderType,
        //                PeriodID = firstOrderInformation.PeriodID,
        //                WareHouseID = firstOrderInformation.WareHouseID,
        //                PrincipalID = firstOrderInformation.PrincipalID,
        //                DistributorCode = firstOrderInformation.DistributorCode,
        //                Disty_billtoID = firstOrderInformation.Disty_billtoID,
        //                DeliveredDate = firstOrderInformation.DeliveredDate,
        //                isReturn = firstOrderInformation.isReturn,
        //                Status = firstOrderInformation.Status,
        //                IsPrintedDeliveryNote = firstOrderInformation.IsPrintedDeliveryNote,
        //                PrintedDeliveryNoteCount = firstOrderInformation.PrintedDeliveryNoteCount,
        //                LastedDeliveryNotePrintDate = firstOrderInformation.LastedDeliveryNotePrintDate,
        //                SalesOrgID = firstOrderInformation.SalesOrgID,
        //                TerritoryStrID = firstOrderInformation.TerritoryStrID,
        //                TerritoryValueKey = firstOrderInformation.TerritoryValueKey,
        //                BranchId = firstOrderInformation.BranchId,
        //                RegionId = firstOrderInformation.RegionId,
        //                SubRegionId = firstOrderInformation.SubRegionId,
        //                AreaId = firstOrderInformation.AreaId,
        //                SubAreaId = firstOrderInformation.SubAreaId,
        //                DSAID = firstOrderInformation.DSAID,
        //                NSD_ID = firstOrderInformation.NSD_ID,
        //                Branch_Manager_ID = firstOrderInformation.Branch_Manager_ID,
        //                Region_Manager_ID = firstOrderInformation.Region_Manager_ID,
        //                Sub_Region_Manager_ID = firstOrderInformation.Sub_Region_Manager_ID,
        //                Area_Manager_ID = firstOrderInformation.Area_Manager_ID,
        //                Sub_Area_Manager_ID = firstOrderInformation.Sub_Area_Manager_ID,
        //                DSA_Manager_ID = firstOrderInformation.DSA_Manager_ID,
        //                RZ_Suppervisor_ID = firstOrderInformation.RZ_Suppervisor_ID,
        //                SIC_ID = firstOrderInformation.SIC_ID,
        //                SalesRepID = firstOrderInformation.SalesRepID,
        //                RouteZoneID = firstOrderInformation.RouteZoneID,
        //                RouteZOneType = firstOrderInformation.RouteZOneType,
        //                RouteZonelocation = firstOrderInformation.RouteZonelocation,
        //                CustomerId = firstOrderInformation.CustomerId,
        //                CustomerShiptoID = firstOrderInformation.CustomerShiptoID,
        //                CustomerPhone = firstOrderInformation.CustomerPhone,
        //                CustomerName = firstOrderInformation.CustomerName,
        //                CustomerAddress = firstOrderInformation.CustomerAddress,
        //                Shipto_Attribute1 = firstOrderInformation.Shipto_Attribute1,
        //                Shipto_Attribute2 = firstOrderInformation.Shipto_Attribute2,
        //                Shipto_Attribute3 = firstOrderInformation.Shipto_Attribute3,
        //                Shipto_Attribute4 = firstOrderInformation.Shipto_Attribute4,
        //                Shipto_Attribute5 = firstOrderInformation.Shipto_Attribute5,
        //                Shipto_Attribute6 = firstOrderInformation.Shipto_Attribute6,
        //                Shipto_Attribute7 = firstOrderInformation.Shipto_Attribute7,
        //                Shipto_Attribute8 = firstOrderInformation.Shipto_Attribute8,
        //                Shipto_Attribute9 = firstOrderInformation.Shipto_Attribute9,
        //                Shipto_Attribute10 = firstOrderInformation.Shipto_Attribute10,
        //                ExpectShippedDate = firstOrderInformation.ExpectShippedDate,
        //                OrderDate = firstOrderInformation.OrderDate,
        //                VisitDate = firstOrderInformation.VisitDate,
        //                VisitID = firstOrderInformation.VisitID,
        //                External_OrdNBR = firstOrderInformation.External_OrdNBR,
        //                Owner_ID = firstOrderInformation.Owner_ID,
        //                Source = firstOrderInformation.Source,
        //                Orig_Ord_SKUs = firstOrderInformation.Orig_Ord_SKUs,
        //                Ord_SKUs = firstOrderInformation.Ord_SKUs,
        //                Shipped_SKUs = firstOrderInformation.Shipped_SKUs,
        //                Orig_Ord_Qty = firstOrderInformation.Orig_Ord_Qty,
        //                Ord_Qty = firstOrderInformation.Ord_Qty,
        //                Shipped_Qty = firstOrderInformation.Shipped_Qty,
        //                Orig_Promotion_Qty = firstOrderInformation.Orig_Promotion_Qty,
        //                Promotion_Qty = firstOrderInformation.Promotion_Qty,
        //                Shipped_Promotion_Qty = firstOrderInformation.Shipped_Promotion_Qty,
        //                Orig_Ord_Amt = firstOrderInformation.Orig_Ord_Amt,
        //                Ord_Amt = firstOrderInformation.Ord_Amt,
        //                Shipped_Amt = firstOrderInformation.Shipped_Amt,
        //                Promotion_Amt = firstOrderInformation.Promotion_Amt,
        //                Shipped_Promotion_Amt = firstOrderInformation.Shipped_Promotion_Amt,
        //                Orig_Ord_Disc_Amt = firstOrderInformation.Orig_Ord_Disc_Amt,
        //                Ord_Disc_Amt = firstOrderInformation.Ord_Disc_Amt,
        //                Shipped_Disc_Amt = firstOrderInformation.Shipped_Disc_Amt,
        //                Orig_Ordline_Disc_Amt = firstOrderInformation.Orig_Ordline_Disc_Amt,
        //                Ordline_Disc_Amt = firstOrderInformation.Ordline_Disc_Amt,
        //                Shipped_line_Disc_Amt = firstOrderInformation.Shipped_line_Disc_Amt,
        //                Orig_Ord_Extend_Amt = firstOrderInformation.Orig_Ord_Extend_Amt,
        //                Ord_Extend_Amt = firstOrderInformation.Ord_Extend_Amt,
        //                Shipped_Extend_Amt = firstOrderInformation.Shipped_Extend_Amt,
        //                TotalVAT = firstOrderInformation.TotalVAT,
        //                ConfirmCount = firstOrderInformation.ConfirmCount,
        //                PromotionRefNumber = firstOrderInformation.PromotionRefNumber,
        //                MenuType = firstOrderInformation.MenuType,
        //                ExpectDeliveryNote = firstOrderInformation.ExpectDeliveryNote,
        //                Note = firstOrderInformation.Note,
        //                TotalLine = firstOrderInformation.TotalLine,
        //                CustomerShiptoName = firstOrderInformation.CustomerShiptoName,
        //                SalesRepName = firstOrderInformation.SalesRepName,
        //                OrderItems = listOrderItem
        //            };
        //        }).ToList();
        //}

        private List<SaleOrderModel> groupSoOrderInformation(List<SaleOrderBaseModel> searchResult)
        {
            return searchResult.Where(x => x.OrderInformation != null && x.OrderItem != null)
                .GroupBy(x => x.OrderInformation.OrderRefNumber)
                .Select(x =>
                {
                    var firstOrderInformation = x.First().OrderInformation;
                    var saleOrder = _mapper.Map<SaleOrderModel>(firstOrderInformation);
                    saleOrder.OrderItems = x.Select(d => d.OrderItem).ToList();
                    return saleOrder;
                })
                .ToList();
        }

        public async Task<IQueryable<SaleOrderBaseModel>> MappingQuerySO(IQueryable<SaleOrderBaseModel> query, List<GenericFilter> filters)
        {
            foreach (var filter in filters)
            {
                switch (filter.Property)
                {
                    case "RouteZoneID":
                        {
                            query = filter.Values.Any(a => a == "" || a == null)
                            ? query.Where(x => string.IsNullOrEmpty(x.OrderInformation.RouteZoneID) || filter.Values.Contains(x.OrderInformation.RouteZoneID.ToLower().Trim()))
                            : query.Where(x => !string.IsNullOrEmpty(x.OrderInformation.RouteZoneID) && filter.Values.Contains(x.OrderInformation.RouteZoneID.ToLower().Trim()));
                            break;
                        }
                    case "Id":
                        {
                            List<Guid> ids = new();
                            foreach (var value in filter.Values)
                            {
                                if (value != "" || value != null)
                                {
                                    ids.Add(Guid.Parse(value));
                                }
                            }
                            query = query.Where(x => ids.Contains(x.OrderInformation.Id));
                            break;
                        }
                    case "OrderType":
                        {
                            query = filter.Values.Any(a => a == "" || a == null)
                            ? query.Where(x => string.IsNullOrEmpty(x.OrderInformation.OrderType) || filter.Values.Contains(x.OrderInformation.OrderType.ToLower().Trim()))
                            : query.Where(x => !string.IsNullOrEmpty(x.OrderInformation.OrderType) && filter.Values.Contains(x.OrderInformation.OrderType.ToLower().Trim()));
                            break;
                        }
                    case "ReasonCode":
                        {
                            query = filter.Values.Any(a => a == "" || a == null)
                            ? query.Where(x => string.IsNullOrEmpty(x.OrderInformation.ReasonCode) || filter.Values.Contains(x.OrderInformation.ReasonCode.ToLower().Trim()))
                            : query.Where(x => !string.IsNullOrEmpty(x.OrderInformation.ReasonCode) && filter.Values.Contains(x.OrderInformation.ReasonCode.ToLower().Trim()));
                            break;
                        }
                    case "Status":
                        {
                            query = filter.Values.Any(a => a == "" || a == null)
                            ? query.Where(x => string.IsNullOrEmpty(x.OrderInformation.Status) || filter.Values.Contains(x.OrderInformation.Status.ToLower().Trim()))
                            : query.Where(x => !string.IsNullOrEmpty(x.OrderInformation.Status) && filter.Values.Contains(x.OrderInformation.Status.ToLower().Trim()));
                            break;
                        }
                    case "ReferenceRefNbr":
                        {
                            query = filter.Values.Any(a => a == "" || a == null)
                            ? query.Where(x => string.IsNullOrEmpty(x.OrderInformation.ReferenceRefNbr) || filter.Values.Contains(x.OrderInformation.ReferenceRefNbr.ToLower().Trim()))
                            : query.Where(x => !string.IsNullOrEmpty(x.OrderInformation.ReferenceRefNbr) && filter.Values.Contains(x.OrderInformation.ReferenceRefNbr.ToLower().Trim()));
                            break;
                        }
                    case "OrderRefNumber":
                        {
                            query = filter.Values.Any(a => a == "" || a == null)
                            ? query.Where(x => string.IsNullOrEmpty(x.OrderInformation.OrderRefNumber) || filter.Values.Contains(x.OrderInformation.OrderRefNumber.ToLower().Trim()))
                            : query.Where(x => !string.IsNullOrEmpty(x.OrderInformation.OrderRefNumber) && filter.Values.Contains(x.OrderInformation.OrderRefNumber.ToLower().Trim()));
                            break;
                        }
                    case "CustomerId":
                        {
                            query = filter.Values.Any(a => a == "" || a == null)
                            ? query.Where(x => string.IsNullOrEmpty(x.OrderInformation.CustomerId) || filter.Values.Contains(x.OrderInformation.CustomerId.ToLower().Trim()))
                            : query.Where(x => !string.IsNullOrEmpty(x.OrderInformation.CustomerId) && filter.Values.Contains(x.OrderInformation.CustomerId.ToLower().Trim()));
                            break;
                        }
                    case "CustomerName":
                        {
                            query = filter.Values.Any(a => a == "" || a == null)
                            ? query.Where(x => string.IsNullOrEmpty(x.OrderInformation.CustomerName) || filter.Values.Contains(x.OrderInformation.CustomerName.ToLower().Trim()))
                            : query.Where(x => !string.IsNullOrEmpty(x.OrderInformation.CustomerName) && filter.Values.Contains(x.OrderInformation.CustomerName.ToLower().Trim()));
                            break;
                        }
                    case "SalesRepID":
                        {
                            query = filter.Values.Any(a => a == "" || a == null)
                            ? query.Where(x => string.IsNullOrEmpty(x.OrderInformation.SalesRepID) || filter.Values.Contains(x.OrderInformation.SalesRepID.ToLower().Trim()))
                            : query.Where(x => !string.IsNullOrEmpty(x.OrderInformation.SalesRepID) && filter.Values.Contains(x.OrderInformation.SalesRepID.ToLower().Trim()));
                            break;
                        }
                    case "IsDirect":
                        {
                            List<bool> values = new();
                            foreach (var value in filter.Values)
                            {
                                if (value != "" || value != null)
                                {
                                    values.Add(Boolean.Parse(value));
                                }

                            }
                            query = query.Where(x => values.Contains(x.OrderInformation.IsDirect));
                            break;
                        }
                    case "IsPrintedDeliveryNote":
                        {
                            List<bool> values = new();
                            foreach (var value in filter.Values)
                            {
                                if (value != "" || value != null)
                                {
                                    values.Add(Boolean.Parse(value));
                                }

                            }
                            query = query.Where(x => values.Contains(x.OrderInformation.IsPrintedDeliveryNote));
                            break;
                        }
                    case "TotalLine":
                        {
                            List<int> values = new();
                            foreach (var value in filter.Values)
                            {
                                if (value != "" || value != null)
                                {
                                    values.Add(Int32.Parse(value));
                                }

                            }
                            query = filter.Values.Any(a => a == "" || a == null)
                            ? query.Where(x => x.OrderInformation.TotalLine == null || values.Contains(x.OrderInformation.TotalLine))
                            : query.Where(x => x.OrderInformation.TotalLine != null || values.Contains(x.OrderInformation.TotalLine));
                            break;
                        }
                    case "CustomerCode":
                        {
                            query = filter.Values.Any(a => a == "" || a == null)
                            ? query.Where(x => string.IsNullOrEmpty(x.OrderInformation.CustomerId) || filter.Values.Contains(x.OrderInformation.CustomerId.ToLower().Trim()))
                            : query.Where(x => !string.IsNullOrEmpty(x.OrderInformation.CustomerId) && filter.Values.Contains(x.OrderInformation.CustomerId.ToLower().Trim()));
                            break;
                        }
                    default:
                        break;
                }
            }

            return query;
        }

        public async Task<List<CommonSoOrderModel>> MappingQuerySOV2(List<CommonSoOrderModel> query, List<GenericFilter> filters)
        {
            foreach (var filter in filters)
            {
                switch (filter.Property)
                {
                    case "RouteZoneID":
                        {
                            query = filter.Values.Any(a => a == "" || a == null)
                            ? query.Where(x => string.IsNullOrEmpty(x.RouteZoneID) || filter.Values.Contains(x.RouteZoneID.ToLower().Trim())).ToList()
                            : query.Where(x => !string.IsNullOrEmpty(x.RouteZoneID) && filter.Values.Contains(x.RouteZoneID.ToLower().Trim())).ToList();
                            break;
                        }
                    case "Id":
                        {
                            List<Guid> ids = new();
                            foreach (var value in filter.Values)
                            {
                                if (value != "" || value != null)
                                {
                                    ids.Add(Guid.Parse(value));
                                }
                            }
                            query = query.Where(x => ids.Contains(x.Id)).ToList();
                            break;
                        }
                    case "OrderType":
                        {
                            query = filter.Values.Any(a => a == "" || a == null)
                            ? query.Where(x => string.IsNullOrEmpty(x.OrderType) || filter.Values.Contains(x.OrderType.ToLower().Trim())).ToList()
                            : query.Where(x => !string.IsNullOrEmpty(x.OrderType) && filter.Values.Contains(x.OrderType.ToLower().Trim())).ToList();
                            break;
                        }
                    case "ReasonCode":
                        {
                            query = filter.Values.Any(a => a == "" || a == null)
                            ? query.Where(x => string.IsNullOrEmpty(x.ReasonCode) || filter.Values.Contains(x.ReasonCode.ToLower().Trim())).ToList()
                            : query.Where(x => !string.IsNullOrEmpty(x.ReasonCode) && filter.Values.Contains(x.ReasonCode.ToLower().Trim())).ToList();
                            break;
                        }
                    case "Status":
                        {
                            query = filter.Values.Any(a => a == "" || a == null)
                            ? query.Where(x => string.IsNullOrEmpty(x.Status) || filter.Values.Contains(x.Status.ToLower().Trim())).ToList()
                            : query.Where(x => !string.IsNullOrEmpty(x.Status) && filter.Values.Contains(x.Status.ToLower().Trim())).ToList();
                            break;
                        }
                    case "ReferenceRefNbr":
                        {
                            query = filter.Values.Any(a => a == "" || a == null)
                            ? query.Where(x => string.IsNullOrEmpty(x.ReferenceRefNbr) || filter.Values.Contains(x.ReferenceRefNbr.ToLower().Trim())).ToList()
                            : query.Where(x => !string.IsNullOrEmpty(x.ReferenceRefNbr) && filter.Values.Contains(x.ReferenceRefNbr.ToLower().Trim())).ToList();
                            break;
                        }
                    case "OrderRefNumber":
                        {
                            query = filter.Values.Any(a => a == "" || a == null)
                            ? query.Where(x => string.IsNullOrEmpty(x.OrderRefNumber) || filter.Values.Contains(x.OrderRefNumber.ToLower().Trim())).ToList()
                            : query.Where(x => !string.IsNullOrEmpty(x.OrderRefNumber) && filter.Values.Contains(x.OrderRefNumber.ToLower().Trim())).ToList();
                            break;
                        }
                    case "CustomerId":
                        {
                            query = filter.Values.Any(a => a == "" || a == null)
                            ? query.Where(x => string.IsNullOrEmpty(x.CustomerId) || filter.Values.Contains(x.CustomerId.ToLower().Trim())).ToList()
                            : query.Where(x => !string.IsNullOrEmpty(x.CustomerId) && filter.Values.Contains(x.CustomerId.ToLower().Trim())).ToList();
                            break;
                        }
                    case "CustomerName":
                        {
                            query = filter.Values.Any(a => a == "" || a == null)
                            ? query.Where(x => string.IsNullOrEmpty(x.CustomerName) || filter.Values.Contains(x.CustomerName.ToLower().Trim())).ToList()
                            : query.Where(x => !string.IsNullOrEmpty(x.CustomerName) && filter.Values.Contains(x.CustomerName.ToLower().Trim())).ToList();
                            break;
                        }
                    case "SalesRepID":
                        {
                            query = filter.Values.Any(a => a == "" || a == null)
                            ? query.Where(x => string.IsNullOrEmpty(x.SalesRepID) || filter.Values.Contains(x.SalesRepID.ToLower().Trim())).ToList()
                            : query.Where(x => !string.IsNullOrEmpty(x.SalesRepID) && filter.Values.Contains(x.SalesRepID.ToLower().Trim())).ToList();
                            break;
                        }
                    case "IsDirect":
                        {
                            List<bool> values = new();
                            foreach (var value in filter.Values)
                            {
                                if (value != "" || value != null)
                                {
                                    values.Add(Boolean.Parse(value));
                                }

                            }
                            query = query.Where(x => values.Contains(x.IsDirect)).ToList();
                            break;
                        }
                    case "IsPrintedDeliveryNote":
                        {
                            List<bool> values = new();
                            foreach (var value in filter.Values)
                            {
                                if (value != "" || value != null)
                                {
                                    values.Add(Boolean.Parse(value));
                                }

                            }
                            query = query.Where(x => values.Contains(x.IsPrintedDeliveryNote)).ToList();
                            break;
                        }
                    case "TotalLine":
                        {
                            List<int> values = new();
                            foreach (var value in filter.Values)
                            {
                                if (value != "" || value != null)
                                {
                                    values.Add(Int32.Parse(value));
                                }

                            }
                            query = filter.Values.Any(a => a == "" || a == null)
                            ? query.Where(x => x.TotalLine == null || values.Contains(x.TotalLine)).ToList()
                            : query.Where(x => x.TotalLine != null || values.Contains(x.TotalLine)).ToList();
                            break;
                        }
                    case "CustomerCode":
                        {
                            query = filter.Values.Any(a => a == "" || a == null)
                            ? query.Where(x => string.IsNullOrEmpty(x.CustomerId) || filter.Values.Contains(x.CustomerId.ToLower().Trim())).ToList()
                            : query.Where(x => !string.IsNullOrEmpty(x.CustomerId) && filter.Values.Contains(x.CustomerId.ToLower().Trim())).ToList();
                            break;
                        }
                    default:
                        break;
                }
            }

            return query;
        }

        public async Task<IQueryable<SO_OrderInformations>> MappingQuerySOv2(IQueryable<SO_OrderInformations> query, List<GenericFilter> filters)
        {
            foreach (var filter in filters)
            {
                switch (filter.Property)
                {
                    case "RouteZoneID":
                        {
                            query = filter.Values.Any(a => a == "" || a == null)
                            ? query.Where(x => string.IsNullOrEmpty(x.RouteZoneID) || filter.Values.Contains(x.RouteZoneID.ToLower().Trim()))
                            : query.Where(x => !string.IsNullOrEmpty(x.RouteZoneID) && filter.Values.Contains(x.RouteZoneID.ToLower().Trim()));
                            break;
                        }
                    case "Id":
                        {
                            List<Guid> ids = new();
                            foreach (var value in filter.Values)
                            {
                                if (value != "" || value != null)
                                {
                                    ids.Add(Guid.Parse(value));
                                }
                            }
                            query = query.Where(x => ids.Contains(x.Id));
                            break;
                        }
                    case "OrderType":
                        {
                            //query = filter.Values.Any(a => a == "" || a == null)
                            //? query.Where(x => string.IsNullOrEmpty(x.OrderType) || filter.Values.Contains(x.OrderType.ToLower().Trim()))
                            //: query.Where(x => !string.IsNullOrEmpty(x.OrderType) && filter.Values.Contains(x.OrderType.ToLower().Trim()));
                            //break;
                            query = filter.Values.Any(a => a == "" || a == null)
                            ? query.Where(x => string.IsNullOrEmpty(x.OrderType) || filter.Values.Contains(x.OrderType))
                            : query.Where(x => !string.IsNullOrEmpty(x.OrderType) && filter.Values.Contains(x.OrderType));
                            break;
                        }
                    case "ReasonCode":
                        {
                            query = filter.Values.Any(a => a == "" || a == null)
                            ? query.Where(x => string.IsNullOrEmpty(x.ReasonCode) || filter.Values.Contains(x.ReasonCode.ToLower().Trim()))
                            : query.Where(x => !string.IsNullOrEmpty(x.ReasonCode) && filter.Values.Contains(x.ReasonCode.ToLower().Trim()));
                            break;
                        }
                    case "Status":
                        {
                            //query = filter.Values.Any(a => a == "" || a == null)
                            //? query.Where(x => string.IsNullOrEmpty(x.Status) || filter.Values.Contains(x.Status.ToLower().Trim()))
                            //: query.Where(x => !string.IsNullOrEmpty(x.Status) && filter.Values.Contains(x.Status.ToLower().Trim()));
                            //break;
                            query = filter.Values.Any(a => a == "" || a == null)
                            ? query.Where(x => string.IsNullOrEmpty(x.Status) || filter.Values.Contains(x.Status))
                            : query.Where(x => !string.IsNullOrEmpty(x.Status) && filter.Values.Contains(x.Status));
                            break;
                        }
                    case "ReferenceRefNbr":
                        {
                            query = filter.Values.Any(a => a == "" || a == null)
                            ? query.Where(x => string.IsNullOrEmpty(x.ReferenceRefNbr) || filter.Values.Contains(x.ReferenceRefNbr.ToLower().Trim()))
                            : query.Where(x => !string.IsNullOrEmpty(x.ReferenceRefNbr) && filter.Values.Contains(x.ReferenceRefNbr.ToLower().Trim()));
                            break;
                        }
                    case "OrderRefNumber":
                        {
                            query = filter.Values.Any(a => a == "" || a == null)
                            ? query.Where(x => string.IsNullOrEmpty(x.OrderRefNumber) || filter.Values.Contains(x.OrderRefNumber.ToLower().Trim()))
                            : query.Where(x => !string.IsNullOrEmpty(x.OrderRefNumber) && filter.Values.Contains(x.OrderRefNumber.ToLower().Trim()));
                            break;
                        }
                    case "CustomerId":
                        {
                            //query = filter.Values.Any(a => a == "" || a == null)
                            //? query.Where(x => string.IsNullOrEmpty(x.CustomerId) || filter.Values.Contains(x.CustomerId.ToLower().Trim()))
                            //: query.Where(x => !string.IsNullOrEmpty(x.CustomerId) && filter.Values.Contains(x.CustomerId.ToLower().Trim()));
                            //break;
                            query = filter.Values.Any(a => a == "" || a == null)
                            ? query.Where(x => string.IsNullOrEmpty(x.CustomerId) || filter.Values.Any(y => EF.Functions.ILike(x.CustomerId, y)))
                            : query.Where(x => !string.IsNullOrEmpty(x.CustomerId) && filter.Values.Any(y => EF.Functions.ILike(x.CustomerId, y)));
                            break;
                        }
                    case "CustomerName":
                        {
                            //query = filter.Values.Any(a => a == "" || a == null)
                            //? query.Where(x => string.IsNullOrEmpty(x.CustomerName) || filter.Values.Contains(x.CustomerName.ToLower().Trim()))
                            //: query.Where(x => !string.IsNullOrEmpty(x.CustomerName) && filter.Values.Contains(x.CustomerName.ToLower().Trim()));
                            //break;
                            query = filter.Values.Any(a => a == "" || a == null)
                            ? query.Where(x => string.IsNullOrEmpty(x.CustomerName) || filter.Values.Any(y => EF.Functions.ILike(x.CustomerName, y)))
                            : query.Where(x => !string.IsNullOrEmpty(x.CustomerName) && filter.Values.Any(y => EF.Functions.ILike(x.CustomerName, y)));
                            break;
                        }
                    case "CustomerShiptoName":
                        {
                            var wildcardValues = filter.Values.Where(y => !string.IsNullOrEmpty(y)).Select(y => $"%{y}%").ToList();
                            query = filter.Values.Any(a => string.IsNullOrEmpty(a))
                                ? query.Where(x => string.IsNullOrEmpty(x.CustomerName) || wildcardValues.Any(y => EF.Functions.ILike(x.CustomerName, y)))
                                : query.Where(x => !string.IsNullOrEmpty(x.CustomerName) && wildcardValues.Any(y => EF.Functions.ILike(x.CustomerName, y)));
                            break;
                        }
                    case "isReturn":
                        {
                            List<bool> values = new();
                            foreach (var value in filter.Values)
                            {
                                if (value != "" || value != null)
                                {
                                    values.Add(Boolean.Parse(value));
                                }

                            }
                            query = query.Where(x => values.Contains(x.isReturn));
                            break;
                        }
                    case "SalesRepID":
                        {
                            //query = filter.Values.Any(a => a == "" || a == null)
                            //? query.Where(x => string.IsNullOrEmpty(x.SalesRepID) || filter.Values.Contains(x.SalesRepID.ToLower().Trim()))
                            //: query.Where(x => !string.IsNullOrEmpty(x.SalesRepID) && filter.Values.Contains(x.SalesRepID.ToLower().Trim(), ));
                            //break;
                            query = filter.Values.Any(a => a == "" || a == null)
                            ? query.Where(x => string.IsNullOrEmpty(x.SalesRepID) || filter.Values.Any(y => EF.Functions.ILike(x.SalesRepID, y)))
                            : query.Where(x => !string.IsNullOrEmpty(x.SalesRepID) && filter.Values.Any(y => EF.Functions.ILike(x.SalesRepID, y)));
                            break;
                        }
                    case "IsDirect":
                        {
                            List<bool> values = new();
                            foreach (var value in filter.Values)
                            {
                                if (value != "" || value != null)
                                {
                                    values.Add(Boolean.Parse(value));
                                }

                            }
                            query = query.Where(x => values.Contains(x.IsDirect));
                            break;
                        }
                    case "IsPrintedDeliveryNote":
                        {
                            List<bool> values = new();
                            foreach (var value in filter.Values)
                            {
                                if (value != "" || value != null)
                                {
                                    values.Add(Boolean.Parse(value));
                                }

                            }
                            query = query.Where(x => values.Contains(x.IsPrintedDeliveryNote));
                            break;
                        }
                    case "TotalLine":
                        {
                            List<int> values = new();
                            foreach (var value in filter.Values)
                            {
                                if (value != "" || value != null)
                                {
                                    values.Add(Int32.Parse(value));
                                }

                            }
                            query = filter.Values.Any(a => a == "" || a == null)
                            ? query.Where(x => x.TotalLine == null || values.Contains(x.TotalLine))
                            : query.Where(x => x.TotalLine != null || values.Contains(x.TotalLine));
                            break;
                        }
                    case "CustomerCode":
                        {
                            query = filter.Values.Any(a => a == "" || a == null)
                            ? query.Where(x => string.IsNullOrEmpty(x.CustomerId) || filter.Values.Contains(x.CustomerId.ToLower().Trim()))
                            : query.Where(x => !string.IsNullOrEmpty(x.CustomerId) && filter.Values.Contains(x.CustomerId.ToLower().Trim()));
                            break;
                        }
                    default:
                        break;
                }
            }

            return query;
        }

        #region bookingStockReuse

        public async Task<BaseResultModel> BookingStockItems(List<BookingStockReqModel> models, string token)
        {
            ReqModel = models;
            if (models.Select(x => x.DistributorCode).Distinct().ToList().Count > 1)
            {
                return new BaseResultModel
                {
                    Code = 400,
                    IsSuccess = false,
                    Message = "Ones Distributor per Request"
                };
            }

            var distributorInfor = models.FirstOrDefault();
            var reqId = Guid.NewGuid();
            foreach (var model in models)
            {
                model.RequestId = reqId;
                var itemBaseQty = model.OrderBaseQuantities;
                switch (model.AllocateType.ToUpper())
                {
                    case AllocateType.SKU:
                        {
                            var res = await handleBookingStockItem(model, token);
                            break;
                        }
                    case AllocateType.GROUP:
                        {
                            //call api transaction
                            var standardSkus = _clientService.CommonRequest<List<StandardItemModel>>(CommonData.SystemUrlCode.SystemAdminAPI, $"Standard/GetInventoryItemStdByItemGroupByQuantity/{model.ItemCode}/{itemBaseQty}", Method.GET, $"Rdos {token.Split(" ").Last()}", null);
                            if (standardSkus != null && standardSkus.Count > 0)
                            {
                                foreach (var item in standardSkus)
                                {
                                    var bookingData = new BookingStockReqModel
                                    {
                                        DistributorCode = model.DistributorCode,
                                        DistributorShiptoCode = model.DistributorShiptoCode,
                                        AllocateType = model.AllocateType,
                                        ItemCode = item.InventoryCode,
                                        Uom = model.Uom,
                                        OrderQuantities = model.OrderQuantities,
                                        OrderBaseQuantities = item.Avaiable,
                                        BaseUom = model.BaseUom,
                                        TransactionId = model.TransactionId,
                                        VisitId = model.VisitId,
                                        ForceConversion = true,
                                        RequestDate = model.RequestDate,
                                        RequestId = model.RequestId
                                    };
                                    var res = await handleBookingStockItem(bookingData, token);
                                }
                            }
                            break;
                        }
                    default:
                        break;
                }
            }

            // _allocationDetailRepo.Save();

            return new BaseResultModel
            {
                Code = 200,
                IsSuccess = true,
                // Data = response
            };
        }

        public async Task<ItemBookedModel> handleBookingStockItem(BookingStockReqModel model, string token)
        {
            try
            {
                var bookedItem = new ItemBookedModel()
                {
                    ItemCode = model.ItemCode,
                    Uom = model.Uom,
                    Quantity = model.OrderQuantities,
                    BaseUom = model.BaseUom,
                    BaseQty = model.OrderBaseQuantities,
                };

                var allocationDetail = await _allocationDetailRepo.GetAllQueryable(null, null, null, _schemaName)
                    .FirstOrDefaultAsync(x => x.DistributorCode == model.DistributorCode &&
                                            x.WareHouseCode == model.DistributorShiptoCode &&
                                            x.LocationCode == model.LocationCode &&
                                            x.ItemCode == model.ItemCode);


                var trakingLog = new InvAllocationTracking
                {
                    Id = Guid.NewGuid(),
                    ItemKey = allocationDetail.ItemKey,
                    ItemId = allocationDetail.ItemId,
                    ItemCode = allocationDetail.ItemCode,
                    BaseUom = allocationDetail.BaseUom,
                    ItemDescription = allocationDetail.ItemDescription,
                    WareHouseCode = allocationDetail.WareHouseCode,
                    LocationCode = allocationDetail.LocationCode,
                    DistributorCode = allocationDetail.DistributorCode,
                    OnHandBeforChanged = allocationDetail.OnHand,
                    OnHandToChanged = 0,
                    OnHandChanged = allocationDetail.OnHand,
                    OnSoShippingBeforChanged = allocationDetail.OnSoShipping,
                    OnSoShippingToChanged = 0,
                    OnSoShippingChanged = allocationDetail.OnSoShipping,
                    OnSoBookedBeforChanged = allocationDetail.OnSoBooked,
                    OnSoBookedToChanged = 0,
                    OnSoBookedChanged = 0,
                    AvailableBeforChanged = allocationDetail.Available,
                    AvailableToChanged = 0,
                    AvailableChanged = 0,
                    ItemGroupCode = allocationDetail.ItemGroupCode,
                    DSACode = allocationDetail.DSACode,
                    FromFeature = "Baseline",
                    RequestDate = model.RequestDate,
                    RequestId = model.RequestId,
                    IsSuccess = true,
                    RequestBody = Newtonsoft.Json.JsonConvert.SerializeObject(ReqModel)
                };

                //Nếu UOM mua = baseUOM thì k cần quy đổi

                bookedItem.BaseQty = model.OrderBaseQuantities;
                allocationDetail.OnSoBooked += bookedItem.BaseQty;
                allocationDetail.Available -= bookedItem.BaseQty;

                trakingLog.OnSoBookedToChanged = bookedItem.BaseQty;
                trakingLog.AvailableToChanged = -bookedItem.BaseQty;
                trakingLog.OnSoBookedChanged = allocationDetail.OnSoBooked;
                trakingLog.AvailableChanged = allocationDetail.Available;

                if (allocationDetail.OnSoBooked < 0 || allocationDetail.Available < 0)
                {
                    trakingLog.IsSuccess = false;
                    _alocationtrackinglogRepo.Add(trakingLog, _schemaName);
                    return null;
                }
                _alocationtrackinglogRepo.Add(trakingLog, _schemaName);
                _allocationDetailRepo.UpdateUnSaved(allocationDetail, _schemaName);

                return bookedItem;
            }
            catch (System.Exception ex)
            {
                return null;
            }
        }

        #endregion

        #region Khoa enhance 
        public BaseResultModel SaleOrderDetail(string ExternalOrdNBR, string OrderType)
        {
            try
            {
                var ffa = new FfaOrderModel();
                ffa.FfasoOrderInformation = _ffasoOrderInformationRepository.GetAllQueryable(x => !string.IsNullOrEmpty(x.External_OrdNBR) && x.External_OrdNBR == ExternalOrdNBR && x.OrderType.Equals(OrderType), null, null, _schemaName)?.FirstOrDefault();
                
                if (ffa.FfasoOrderInformation is not null)
                {
                    if(ffa.FfasoOrderInformation.Status.Equals("FFA_SO_01") && !string.IsNullOrWhiteSpace(ffa.FfasoOrderInformation.OrderRefNumber))
                    {
                        var soOrder = _orderInformationsRepository.GetAllQueryable(x => !string.IsNullOrEmpty(x.External_OrdNBR) && x.External_OrdNBR == ffa.FfasoOrderInformation.OrderRefNumber, null, null, _schemaName)?.FirstOrDefault();
                        if (soOrder is not null) 
                        {
                            ffa.FfasoOrderInformation.Status = soOrder.Status;
                        }
                    }

                    ffa.FfasoOrderItem = _ffasoOrderItemRepository.GetAllQueryable(x => !string.IsNullOrEmpty(x.External_OrdNBR) && x.External_OrdNBR == ffa.FfasoOrderInformation.External_OrdNBR && x.OrderType.Equals(OrderType), null, null, _schemaName)?.ToList();

                    ffa.FfadsSoLot = _ffadsSoLotRepo.GetAllQueryable(l => !string.IsNullOrEmpty(l.External_OrdNBR) && l.External_OrdNBR == ffa.FfasoOrderInformation.External_OrdNBR, null, null, _schemaName)?.ToList();

                    ffa.FfadsSoPayment = _ffadsSoPaymentRepo.GetAllQueryable(p => !string.IsNullOrEmpty(p.External_OrdNBR) && p.External_OrdNBR == ffa.FfasoOrderInformation.External_OrdNBR && p.OrderType.Equals(OrderType), null, null, _schemaName)?.ToList();
                }
             
                //_ffasoOrderInformationRepository.Dispose(_schemaName);
                //_ffasoOrderItemRepository.Dispose(_schemaName);
                //_ffadsSoLotRepo.Dispose(_schemaName);
                //_ffadsSoPaymentRepo.Dispose(_schemaName);

                var result = new SaleOrderDetail()
                {
                    FFA = ffa,
                    SO = null
                };

                return new BaseResultModel
                {
                    Code = 200,
                    Message = "OK",
                    IsSuccess = true,
                    Data = result
                };
            }
            catch (System.Exception ex)
            {
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }




            #endregion

        }
    }
}
