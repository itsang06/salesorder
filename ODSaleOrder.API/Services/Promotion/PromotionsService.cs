using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sys.Common.Models;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using ODSaleOrder.API.Services.SaleOrder.Interface;
using ODSaleOrder.API.Services.Base;
using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Models;
using static SysAdmin.API.Constants.Constant;
using RestSharp;
using SysAdmin.Models.StaticValue;
using ODSaleOrder.API.Infrastructure.SOInfrastructure;
using DynamicSchema.Helper.Services.Interface;
using DynamicSchema.Helper.Services;
using Microsoft.AspNetCore.Http;

namespace ODSaleOrder.API.Services.SaleOrder
{
    public class PromotionsService : IPromotionsService
    {
        // Service
        private readonly ILogger<PromotionsService> _logger;
        private readonly IMapper _mapper;
        public IRestClient _client;
        public readonly IClientService _clientService;

        // Private
        //private readonly IBaseRepository<Temp_Programs> _programsRepo;
        //private readonly IBaseRepository<Temp_ProgramsDetails> _programsDetailsRepo;
        //private readonly IBaseRepository<Temp_ProgramDetailsItemsGroup> _programsItemgroupRepo;
        //private readonly IBaseRepository<Temp_ProgramDetailReward> _ProgramDetailRewardRepo;
        //private readonly IBaseRepository<ProgramCustomers> _customerProgramRepo;
        //private readonly IBaseRepository<ProgramCustomersDetail> _customerProgramDetailRepo;
        //private readonly IBaseRepository<ProgramCustomerItemsGroup> _customerProgramItemsGroupRepo;
        //private readonly IBaseRepository<ProgramCustomerDetailsItems> _customerProgramDetailsItemsRepo;

        private readonly IDynamicBaseRepository<Temp_Programs> _programsRepo;
        private readonly IDynamicBaseRepository<Temp_ProgramsDetails> _programsDetailsRepo;
        private readonly IDynamicBaseRepository<Temp_ProgramDetailsItemsGroup> _programsItemgroupRepo;
        private readonly IDynamicBaseRepository<Temp_ProgramDetailReward> _ProgramDetailRewardRepo;
        private readonly IDynamicBaseRepository<ProgramCustomers> _customerProgramRepo;
        private readonly IDynamicBaseRepository<ProgramCustomersDetail> _customerProgramDetailRepo;
        private readonly IDynamicBaseRepository<ProgramCustomerItemsGroup> _customerProgramItemsGroupRepo;
        private readonly IDynamicBaseRepository<ProgramCustomerDetailsItems> _customerProgramDetailsItemsRepo;

        // Public
        private readonly IDynamicBaseRepository<Principal> _principalRepo;

        // Other
        private string UserToken;
        private List<PromotionCustomerModel> _items = new List<PromotionCustomerModel>();
        private bool _includeSaved = true;

        private readonly IHttpContextAccessor _httpContextAccessor;
        private string _schemaName = OD_Constant.DEFAULT_SCHEMA;
        private string _distributorCode = null;


        public PromotionsService(ILogger<PromotionsService> logger,
            //IBaseRepository<Temp_Programs> programsRepo,
            //IBaseRepository<Temp_ProgramsDetails> programsDetailsRepo,
            //IBaseRepository<Temp_ProgramDetailsItemsGroup> programsItemgroupRepo,
            //IBaseRepository<Temp_ProgramDetailReward> programsItemgroupRewardRepo,
            //IBaseRepository<ProgramCustomers> programCustomersRepo,
            //IBaseRepository<ProgramCustomersDetail> programCustomersDetailRepo,
            //IBaseRepository<ProgramCustomerItemsGroup> programCustomerItemsGroupRepo,
            //IBaseRepository<ProgramCustomerDetailsItems> programCustomerDetailsItemsRepo,
            IMapper mapper,
            IClientService clientService,
            RDOSContext dataContext,
            IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _mapper = mapper;
            _programsRepo = new DynamicBaseRepository<Temp_Programs>(dataContext);
            _programsDetailsRepo = new DynamicBaseRepository<Temp_ProgramsDetails>(dataContext);
            _programsItemgroupRepo = new DynamicBaseRepository<Temp_ProgramDetailsItemsGroup>(dataContext);
            _ProgramDetailRewardRepo = new DynamicBaseRepository<Temp_ProgramDetailReward>(dataContext);
            _customerProgramRepo = new DynamicBaseRepository<ProgramCustomers>(dataContext);
            _customerProgramDetailRepo = new DynamicBaseRepository<ProgramCustomersDetail>(dataContext);
            _customerProgramItemsGroupRepo = new DynamicBaseRepository<ProgramCustomerItemsGroup>(dataContext);
            _customerProgramDetailsItemsRepo = new DynamicBaseRepository<ProgramCustomerDetailsItems>(dataContext);
            _clientService = clientService;
            _principalRepo = new DynamicBaseRepository<Principal>(dataContext);
            _httpContextAccessor = httpContextAccessor;
            _schemaName = _httpContextAccessor.HttpContext?.Items["SchemaName"] as string ?? _schemaName;
            _distributorCode = _httpContextAccessor.HttpContext?.Items["DistributorCode"] as string ?? _distributorCode;
        }

        public async Task<BaseResultModel> UpsertMutipleCustomerProgram(List<PromotionCustomerModel> models, bool includeSaved, string username, string token)
        {
            try
            {
                UserToken = token;
                _items = new List<PromotionCustomerModel>();
                _includeSaved = includeSaved;
                foreach (var model in models)
                {
                    var upsertResult = await UpsertCustomerProgram(model, username);
                    if (!upsertResult.IsSuccess)
                    {
                        return new BaseResultModel
                        {
                            IsSuccess = false,
                            Code = 400,
                            Message = upsertResult.Message,
                        };
                    }
                    else
                    {
                        if (upsertResult.Data != null)
                        {
                            _items.Add(upsertResult.Data);
                        }
                    }
                }
                if (includeSaved)
                {
                    _programsDetailsRepo.Save(_schemaName);
                }
                return new BaseResultModel
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Ok",
                    Data = _items
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

        public async Task<ResultModelWithObject<PromotionCustomerModel>> UpsertCustomerProgram(PromotionCustomerModel model, string username)
        {
            try
            {
                if (_includeSaved)
                {
                    if (model.Id == Guid.Empty)
                    {
                        _customerProgramRepo.Add(model, _schemaName);
                    }
                    else
                    {
                        _customerProgramRepo.UpdateUnSaved(model, _schemaName);
                    }
                }

                var customerHierachy = (_clientService.CommonRequest<ResultModelWithObject<CustomerSettingHierarchyGetAllResultModel>>(CommonData.SystemUrlCode.ODCustomerAPI, $"CustomerSettingHierarchy/List", Method.GET, UserToken, null)).Data;

                CustomerSettingHierarchyModel highestHiearchy = customerHierachy.CustomerHierarchies.OrderByDescending(x => x.HierarchyLevel).FirstOrDefault();
                string customerAttribute = null;
                switch (highestHiearchy.CustomerSetting.AttributeID)
                {
                    case "CUS01":
                        {
                            customerAttribute = model.Shipto_Attribute1;
                            break;
                        }
                    case "CUS02":
                        {
                            customerAttribute = model.Shipto_Attribute2;
                            break;
                        }
                    case "CUS03":
                        {
                            customerAttribute = model.Shipto_Attribute3;
                            break;
                        }
                    case "CUS04":
                        {
                            customerAttribute = model.Shipto_Attribute4;
                            break;
                        }
                    case "CUS05":
                        {
                            customerAttribute = model.Shipto_Attribute5;
                            break;
                        }
                    case "CUS06":
                        {
                            customerAttribute = model.Shipto_Attribute6;
                            break;
                        }
                    case "CUS07":
                        {
                            customerAttribute = model.Shipto_Attribute7;
                            break;
                        }
                    case "CUS08":
                        {
                            customerAttribute = model.Shipto_Attribute8;
                            break;
                        }
                    case "CUS09":
                        {
                            customerAttribute = model.Shipto_Attribute9;
                            break;
                        }
                    case "CUS10":
                        {
                            customerAttribute = model.Shipto_Attribute10;
                            break;
                        }
                    default:
                        break;
                }

                //call api Get distributor infomation
                var distributorInfo = (_clientService.CommonRequest<ResultModelWithObject<DistributorInfoModel>>(CommonData.SystemUrlCode.SalesOrgAPI, $"DistributorSellingArea/GetInformationDistributor/{username}", Method.GET, UserToken, null)).Data;

                // List<ProgramCustomerDetailsItems> result = new();
                foreach (var item in model.ProgramsCustomerDetails)
                {
                    float budgetbook = 0;
                    foreach (var itemgroup in item.ProgramCustomerItemsGroup)
                    {
                        if (model.ProgramsItemScope != "BUNDLE")
                        {
                            if (item.ProgramsBuyType == "QUANTITY")
                            {
                                itemgroup.Amount = 0;
                                if (model.ProgramsItemScope == "LINE")
                                {
                                    itemgroup.ItemGroupQuantities = item.DetailQuantities * itemgroup.Quantities;
                                }
                                else
                                {
                                    itemgroup.ItemGroupQuantities = itemgroup.Quantities;
                                }
                            }
                            else
                            {
                                if (model.ProgramsItemScope == "LINE" && item.ProgramsBuyType == "AMOUNT")
                                {
                                    itemgroup.ItemGroupQuantities = 0;
                                    itemgroup.Amount = item.DetailAmount * itemgroup.Quantities;
                                }
                                else
                                {
                                    itemgroup.ItemGroupQuantities = 0;
                                }
                            }

                        }
                        else
                        {
                            itemgroup.ItemGroupQuantities = itemgroup.FixedQuantities * item.ActualQantities;
                        }

                        //Budgetbook calculate
                        if (model.ProgramsType == PROMO_PROMOTIONTYPECONST.Promotion && !string.IsNullOrEmpty(item.BudgetCode))
                        {
                            switch (item.BudgetType)
                            {
                                case "01":
                                    {
                                        if (model.ProgramsItemScope != "BUNDLE")
                                        {
                                            budgetbook += itemgroup.Quantities;
                                        }
                                        else
                                        {
                                            budgetbook += item.ActualQantities;
                                        }
                                        break;
                                    }
                                case "02":
                                    {
                                        if (model.ProgramsItemScope == "LINE" && item.ProgramsBuyType == "AMOUNT")
                                        {
                                            budgetbook += (float)itemgroup.Amount;
                                        }
                                        else
                                        {
                                            if (itemgroup.ProductTypeForSale == TP_SALE_TYPE.SKU)
                                            {
                                                var detailItem = _clientService.CommonRequest<ItemMng_IventoryItemModel>(CommonData.SystemUrlCode.ODItemAPI, $"InventoryItem/GetInventoryItemByCode/{itemgroup.InventoryItemCode}", Method.GET, $"Rdos {UserToken.Split(" ").Last()}", null);
                                                var price = await GetPriceByItemsGroupCode(detailItem.InventoryItem.GroupId, itemgroup.UOMCode, distributorInfo, null);
                                                budgetbook += (float)price * itemgroup.ItemGroupQuantities;
                                            }
                                            else
                                            {
                                                var price = await GetPriceByItemsGroupCode(itemgroup.ItemGroupCode, itemgroup.UOMCode, distributorInfo, null);
                                                budgetbook += (float)price * itemgroup.ItemGroupQuantities;
                                            }
                                        }
                                        break;
                                    }
                                default:
                                    break;
                            }
                        }
                    }



                    if (model.ProgramsType != PROMO_PROMOTIONTYPECONST.Promotion)
                    {
                        item.ActualQantities = item.ProgramCustomerItemsGroup.Sum(x => x.ItemGroupQuantities);
                        item.ActualAmount = item.ProgramCustomerItemsGroup.Sum(x => x.Amount);
                        item.RemainQuantities = item.DetailQuantities - item.ActualQantities;
                        if (item.RemainQuantities < 0) item.RemainQuantities = 0;

                        item.RemainAmount = item.DetailAmount - item.ActualAmount;
                        if (item.RemainAmount < 0) item.RemainAmount = 0;
                    }



                    #region //Xử lý Ngân sách
                    var principal = await _principalRepo.GetAllQueryable().FirstOrDefaultAsync();
                    var budgetDataReq = new BudgetReqModel
                    {
                        budgetCode = item.BudgetCode,
                        budgetType = item.BudgetType,
                        customerCode = model.CustomerCode,
                        customerShipTo = model.ShiptoCode,
                        saleOrg = model.SalesOrgCode,
                        budgetAllocationLevel = item.BudgetAllocationLevel,
                        budgetBook = budgetbook,
                        salesTerritoryValueCode = null,
                        promotionCode = model.ProgramCode,
                        promotionLevel = item.DetailLevel,
                        routeZoneCode = model.RouteZoneCode,
                        dsaCode = model.DsaCode,
                        subAreaCode = model.SubArea,
                        areaCode = model.Area,
                        subRegionCode = model.SubRegion,
                        regionCode = model.Region,
                        branchCode = model.Branch,
                        nationwideCode = principal.Country,
                        salesOrgCode = model.SalesOrgCode,
                        referalCode = null, //?
                        distributorCode = _distributorCode
                    };

                    switch (item.BudgetAllocationLevel)
                    {
                        case "DSA":
                            {
                                budgetDataReq.salesTerritoryValueCode = model.DsaCode;
                                break;
                            }
                        case "TL01":
                            {
                                budgetDataReq.salesTerritoryValueCode = model.Branch;
                                break;
                            }
                        case "TL02":
                            {
                                budgetDataReq.salesTerritoryValueCode = model.Region;
                                break;
                            }
                        case "TL03":
                            {
                                budgetDataReq.salesTerritoryValueCode = model.SubRegion;
                                break;
                            }
                        case "TL04":
                            {
                                budgetDataReq.salesTerritoryValueCode = model.Area;
                                break;
                            }
                        case "TL05":
                            {
                                budgetDataReq.salesTerritoryValueCode = model.SubArea;
                                break;
                            }
                        default:
                            break;
                    }
                    if (model.ProgramsType == PROMO_PROMOTIONTYPECONST.Promotion && !string.IsNullOrEmpty(item.BudgetCode))
                    {
                        var budgetChecked = (await _clientService.CommonRequestAsync<ResultModelWithObject<BudgetResModel>>(CommonData.SystemUrlCode.ODTpAPI, $"external_checkbudget/checkbudget", Method.POST, UserToken, budgetDataReq)).Data;

                        if (budgetChecked != null)
                        {
                            item.BudgetBookOver = budgetChecked.budgetBookOver;
                            item.BudgetBook = budgetChecked.budgetBook;
                            item.BudgetBooked = budgetChecked.budgetBooked;
                            if (item.BudgetBook > item.BudgetBooked && !item.BudgetBookOver)
                            {
                                return new ResultModelWithObject<PromotionCustomerModel>
                                {
                                    Code = 400,
                                    IsSuccess = false,
                                    Message = $"{item.ProgramCustomersDetailCode}#{budgetChecked.message}",
                                    Data = model
                                };
                            }
                        }

                        if (!_includeSaved)
                        {
                            //Trả lại booked khi chưa save
                            budgetDataReq.budgetBook = -budgetDataReq.budgetBook;
                            var budgetReturned = (await _clientService.CommonRequestAsync<ResultModelWithObject<BudgetResModel>>(CommonData.SystemUrlCode.ODTpAPI, $"external_checkbudget/checkbudget", Method.POST, UserToken, budgetDataReq)).Data;
                        }

                    }
                    #endregion

                    if (item.Id == Guid.Empty)
                    {
                        if (_includeSaved)
                        {
                            _customerProgramDetailRepo.Add(item, _schemaName);
                            _customerProgramItemsGroupRepo.AddRange(item.ProgramCustomerItemsGroup, _schemaName);
                        }

                        // Handle ItemGroup theo Promotion Detail Type
                        //đi tìm Items từ ItemGroup r mới add vào list của item
                        if (item.ProgramCustomerItemsGroup.Any(x => x.ItemGroupQuantities > 0 || x.Amount > 0))
                        {
                            if (item.ProgramCustomerDetailsItems == null || item.ProgramCustomerDetailsItems.Count < 1)
                            {
                                item.ProgramCustomerDetailsItems = await GetInventoryItemStdByItemGroupByQuantity(customerAttribute, item, item.ProgramCustomersDetailCode, false, username, UserToken, distributorInfo, model.ProgramsType, item.PromotionRefNumber, model.ProgramCode, item.ProgramsBuyType, model.ProgramsItemScope, item.ProgramsGivingType);
                            }

                            if (item.ProgramCustomerItemsGroup != null && item.ProgramCustomerItemsGroup.Count > 0)
                            {
                                if (_includeSaved)
                                {
                                    _customerProgramDetailsItemsRepo.AddRange(item.ProgramCustomerDetailsItems, _schemaName);
                                }
                                // result.AddRange(item.ProgramCustomerDetailsItems);
                            }
                        }
                    }
                    else
                    {
                        if (_includeSaved)
                        {
                            //Nhả Budget
                            var cusProgDetail = await _customerProgramDetailRepo.GetAllQueryable(x => x.Id == item.Id, null, null, _schemaName).FirstOrDefaultAsync();
                            if (cusProgDetail != null && cusProgDetail.BudgetBooked > 0)
                            {
                                var returnBudgetReq = new BudgetReqModel
                                {
                                    budgetCode = budgetDataReq.budgetCode,
                                    budgetType = budgetDataReq.budgetType,
                                    customerCode = budgetDataReq.customerCode,
                                    customerShipTo = budgetDataReq.customerShipTo,
                                    saleOrg = budgetDataReq.saleOrg,
                                    budgetAllocationLevel = budgetDataReq.budgetAllocationLevel,
                                    budgetBook = -cusProgDetail.BudgetBooked,
                                    salesTerritoryValueCode = budgetDataReq.salesTerritoryValueCode,
                                    promotionCode = budgetDataReq.promotionCode,
                                    promotionLevel = budgetDataReq.promotionLevel,
                                    routeZoneCode = budgetDataReq.routeZoneCode,
                                    dsaCode = budgetDataReq.dsaCode,
                                    subAreaCode = budgetDataReq.subAreaCode,
                                    areaCode = budgetDataReq.areaCode,
                                    subRegionCode = budgetDataReq.subRegionCode,
                                    regionCode = budgetDataReq.regionCode,
                                    branchCode = budgetDataReq.branchCode,
                                    nationwideCode = budgetDataReq.nationwideCode,
                                    salesOrgCode = budgetDataReq.salesOrgCode,
                                    referalCode = budgetDataReq.referalCode,
                                    distributorCode = _distributorCode
                                };
                                var budgetReturned = (await _clientService.CommonRequestAsync<ResultModelWithObject<BudgetResModel>>(CommonData.SystemUrlCode.ODTpAPI, $"external_checkbudget/checkbudget", Method.POST, UserToken, returnBudgetReq)).Data;
                            }

                            _customerProgramDetailRepo.UpdateUnSaved(item, _schemaName);

                            //Handle ItemGroup theo Promotion Detail Type
                            //Later
                            //
                            _customerProgramItemsGroupRepo.UpdateRange(item.ProgramCustomerItemsGroup, _schemaName);
                        }

                        #region Handle ItemGroup theo Promotion Detail Type
                        //Delete list Item cũ
                        // var detailItemIds = item.Temp_ProgramCustomerItemsGroup.Where(x => x.Id != Guid.Empty).Select(x => x.Id).ToList();

                        //
                        //đi tìm Items từ ItemGroup r mới add vào list của item
                        if (item.ProgramCustomerDetailsItems != null && item.ProgramCustomerDetailsItems.Count > 0)
                        {
                            var itemList = await _customerProgramDetailsItemsRepo.GetAllQueryable(null, null, null, _schemaName).Where(x => x.ProgramCustomersDetailCode == item.ProgramCustomersDetailCode && x.PromotionRefNumber == item.PromotionRefNumber).AsNoTracking().ToListAsync();
                            var selectedIds = item.ProgramCustomerDetailsItems.Select(x => x.Id).ToList();
                            var oldItemList = itemList.Where(x => !selectedIds.Contains(x.Id)).ToList();
                            if (_includeSaved)
                            {
                                _customerProgramDetailsItemsRepo.RemoveRange(oldItemList, _schemaName);
                            }

                            foreach (var detailItem in item.ProgramCustomerDetailsItems)
                            {
                                if (_includeSaved)
                                {
                                    if (detailItem.Id != Guid.Empty)
                                    {
                                        if (itemList.Any(x => x.Id == detailItem.Id))
                                        {

                                            _customerProgramDetailsItemsRepo.UpdateUnSaved(detailItem, _schemaName);
                                        }
                                        else
                                        {
                                            _customerProgramDetailsItemsRepo.Add(detailItem, _schemaName);
                                        }
                                    }
                                    else
                                    {
                                        _customerProgramDetailsItemsRepo.Add(detailItem, _schemaName);
                                    }
                                }

                            }
                            // result.AddRange(item.ProgramCustomerDetailsItems);
                        }
                        else
                        {
                            var oldItemList = await _customerProgramDetailsItemsRepo.GetAllQueryable(null, null, null, _schemaName).Where(x => x.ProgramCustomersDetailCode == item.ProgramCustomersDetailCode).ToListAsync();
                            if (_includeSaved)
                            {

                                _customerProgramDetailsItemsRepo.RemoveRange(oldItemList, _schemaName);
                            }
                            if (item.ProgramCustomerItemsGroup.Any(x => x.ItemGroupQuantities > 0 || x.Amount > 0))
                            {
                                item.ProgramCustomerDetailsItems = await GetInventoryItemStdByItemGroupByQuantity(customerAttribute, item, item.ProgramCustomersDetailCode, false, username, UserToken, distributorInfo, model.ProgramsType, item.PromotionRefNumber, model.ProgramCode, item.ProgramsBuyType, model.ProgramsItemScope, item.ProgramsGivingType);

                                if (item.ProgramCustomerItemsGroup != null && item.ProgramCustomerItemsGroup.Count > 0)
                                {
                                    if (_includeSaved)
                                    {
                                        _customerProgramDetailsItemsRepo.AddRange(item.ProgramCustomerDetailsItems, _schemaName);
                                    }
                                    // result.AddRange(item.ProgramCustomerDetailsItems);
                                }
                            }
                        }
                        #endregion
                    }
                }

                return new ResultModelWithObject<PromotionCustomerModel>
                {
                    Code = 200,
                    IsSuccess = true,
                    Message = "OK",
                    Data = model,
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);

                return new ResultModelWithObject<PromotionCustomerModel>
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }

        public async Task<decimal> GetPriceByItemsGroupCode(string ItemGroupCode, string UomCode, DistributorInfoModel distributorInfo, string cusAttribute)
        {
            try
            {
                var requestData = new
                {
                    TerritoryStructureCode = distributorInfo.TerritoryStructureCode,
                    AttributeValue = cusAttribute,
                    DSAId = distributorInfo.DSAId,
                    DSACode = distributorInfo.DSACode,
                    ItemGroupCode = ItemGroupCode,
                    Uom = UomCode,
                    DistributorCode = distributorInfo.DistributorCode
                };
                //call api Detail Items
                var price = _clientService.CommonRequest<ResultModelWithObject<decimal>>(CommonData.SystemUrlCode.ODPriceAPI, $"SalesBasePrice/GetSalesPriceCurrent", Method.POST, $"Rdos {UserToken.Split(" ").Last()}", requestData).Data;
                return price;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return 0;
            }
        }

        public async Task<BaseResultModel> GenRefPromotionNumber(PromoRefRequestModel model)
        {
            try
            {
                var time = DateTime.Now;
                string code = model.Username + time.ToString("yyyyMMddHHmmssffff");
                var a = code.Length;
                return new BaseResultModel
                {
                    Code = 200,
                    Data = code,
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

        public async Task<BaseResultModel> GetListProgramsByCustomerID(CustomerPromotionRequestModel cusInfo, string PromotionRefNumber, string ProgramType, string token)
        {

            try
            {
                cusInfo.DistributorCode = _distributorCode;
                UserToken = token;
                List<PromotionCustomerModel> pgCustomers = new();

                #region Getcurrent pgCus
                var datas = _customerProgramRepo.Find(x => x.PromotionRefNumber == PromotionRefNumber &&
                            x.ProgramsType.Trim().ToLower() == ProgramType.Trim().ToLower(), _schemaName).ToList();
                if (datas.Count > 0)
                {
                    pgCustomers = _mapper.Map(datas, pgCustomers);
                    foreach (var pgCustomer in pgCustomers)
                    {
                        var details = _customerProgramDetailRepo.GetAllQueryable(x => x.ProgramCustomersKey == pgCustomer.ProgramCustomersKey, null, null, _schemaName).ToList();
                        if (details.Count > 0)
                        {
                            pgCustomer.ProgramsCustomerDetails = _mapper.Map<List<PromotionCustomerDetailsModel>>(details);
                            foreach (var pgCusDetail in pgCustomer.ProgramsCustomerDetails)
                            {
                                pgCusDetail.ProgramCustomerItemsGroup = _customerProgramItemsGroupRepo.GetAllQueryable(x => x.ProgramCustomersDetailCode == pgCusDetail.ProgramCustomersDetailCode && x.PromotionRefNumber == PromotionRefNumber, null, null, _schemaName).ToList();
                                pgCusDetail.ProgramCustomerDetailsItems = _customerProgramDetailsItemsRepo.GetAllQueryable(x => x.ProgramCustomersDetailCode == pgCusDetail.ProgramCustomersDetailCode, null, null, _schemaName).ToList();
                            }
                        }
                    }
                }
                #endregion

                if (ProgramType == "Promotion")
                {
                    var TpPromotionResult = _clientService.CommonRequest<ResultModelWithObject<List<TpCustomerPromotionModel>>>(CommonData.SystemUrlCode.ODTpAPI, $"tppromotion/getlistpromotionbycustomer", Method.POST, UserToken, cusInfo, true);


                    if (TpPromotionResult.IsSuccess && TpPromotionResult.Data != null && TpPromotionResult.Data.Count > 0)
                    {

                        List<TpCustomerPromotionModel> tpPromotions = TpPromotionResult.Data;
                        if (pgCustomers.Count > 0)
                        {
                            var pgCodes = pgCustomers.Select(x => x.ProgramCode).ToList();
                            tpPromotions = tpPromotions.Where(x => !pgCodes.Contains(x.code)).ToList();
                        }
                        foreach (var promotion in tpPromotions)
                        {
                            // Temp_PromotionCustomerModel pgCustomer = new();
                            //Get  Promotion Detail
                            var promotionDetailResult = _clientService.CommonRequest<ResultModelWithObject<TpPromotionDetailModel>>(CommonData.SystemUrlCode.ODTpAPI, $"tppromotion/Getdetailpromotionexternalbycode/{promotion.code}", Method.GET, UserToken, null, true);
                            if (!promotionDetailResult.IsSuccess || promotionDetailResult.Data == null)
                            {
                                continue;
                            }
                            TpPromotionDetailModel promotionDetail = promotionDetailResult.Data;

                            var promoItemScope = promotionDetail.promotionType == "01" ? PROMO_ITEMSCOPECONST.LINE : promotionDetail.promotionType == "02" ? PROMO_ITEMSCOPECONST.GROUP : promotionDetail.promotionType == "03" ? PROMO_ITEMSCOPECONST.BUNDLE : "Undefined";
                            var pgCustomer = new PromotionCustomerModel
                            {
                                Id = Guid.Empty,
                                PromotionRefNumber = PromotionRefNumber,
                                ProgramCustomersKey = PromotionRefNumber + "-" + promotionDetail.code,
                                ProgramCode = promotionDetail.code,
                                ProgramsType = PROMO_PROMOTIONTYPECONST.Promotion,
                                ProgramsDescription = promotionDetail.fullName,
                                ProgramsItemScope = promoItemScope,
                                ShiptoCode = cusInfo.ShiptoCode,
                                CustomerCode = cusInfo.CustomerCode,
                                IsDeleted = false,
                                EffectiveDate = promotionDetail.effectiveDateFrom,
                                ValidUntil = promotionDetail.validUntil,
                                Shipto_Attribute1 = cusInfo.Shipto_Attribute1,
                                Shipto_Attribute2 = cusInfo.Shipto_Attribute2,
                                Shipto_Attribute3 = cusInfo.Shipto_Attribute3,
                                Shipto_Attribute4 = cusInfo.Shipto_Attribute4,
                                Shipto_Attribute5 = cusInfo.Shipto_Attribute5,
                                Shipto_Attribute6 = cusInfo.Shipto_Attribute6,
                                Shipto_Attribute7 = cusInfo.Shipto_Attribute7,
                                Shipto_Attribute8 = cusInfo.Shipto_Attribute8,
                                Shipto_Attribute9 = cusInfo.Shipto_Attribute9,
                                Shipto_Attribute10 = cusInfo.Shipto_Attribute10,
                                SalesOrgCode = cusInfo.SaleOrgCode,
                                SicCode = cusInfo.SicCode,
                                RouteZoneCode = cusInfo.RouteZoneCode,
                                DsaCode = cusInfo.DsaCode,
                                Branch = cusInfo.Branch,
                                Region = cusInfo.Region,
                                SubRegion = cusInfo.SubRegion,
                                Area = cusInfo.Area,
                                SubArea = cusInfo.SubArea,
                                ProgramsCustomerDetails = new List<PromotionCustomerDetailsModel>()
                            };

                            //definitionStructure
                            foreach (var definitionStructure in promotionDetail.listDefinitionStructure)
                            {
                                var buyType = definitionStructure.quantityPurchased > 0 ? PROMO_BYBREAKDOWNCONST.QUANTITY : PROMO_BYBREAKDOWNCONST.AMOUNT;
                                var givingType = definitionStructure.listProductForGifts.Count > 0 ? PROMO_GIVINGTYPECONST.FREEITEM : definitionStructure.amountOfDonation > 0 ? PROMO_GIVINGTYPECONST.AMOUNT : definitionStructure.percentageOfAmount > 0 ? PROMO_GIVINGTYPECONST.PERCENTED : null;
                                var detailType = definitionStructure.ruleOfGiving && (definitionStructure.onEachValue > 0 || definitionStructure.onEachQuantity > 0) ? PROMO_RULEOFGIVING.BOX : PROMO_RULEOFGIVING.PASSLEVEL;
                                var detailQty = detailType == PROMO_RULEOFGIVING.PASSLEVEL ? definitionStructure.quantityPurchased : definitionStructure.onEachQuantity;
                                var detailAmt = detailType == PROMO_RULEOFGIVING.PASSLEVEL ? definitionStructure.valuePurchased : definitionStructure.onEachValue;

                                #region Flow Budget
                                var budgetCode = definitionStructure.budgetCodeForGift != null ? definitionStructure.budgetCodeForGift : definitionStructure.budgetCodeForDonate != null ? definitionStructure.budgetCodeForDonate : null;
                                var budgetType = definitionStructure.budgetTypeOfGift != null ? definitionStructure.budgetTypeOfGift : definitionStructure.budgetTypeOfDonate != null ? definitionStructure.budgetTypeOfDonate : null;
                                var budgetAllocationLevel = definitionStructure.budgetAllocationLevelOfGift != null ? definitionStructure.budgetAllocationLevelOfGift : definitionStructure.budgetAllocationLevelOfDonate != null ? definitionStructure.budgetAllocationLevelOfDonate : null;
                                #endregion
                                PromotionCustomerDetailsModel pgCusDetail = new PromotionCustomerDetailsModel
                                {
                                    Id = Guid.Empty,
                                    ProgramCustomersDetailCode = pgCustomer.ProgramCustomersKey + "-" + definitionStructure.levelCode,
                                    ProgramCustomersKey = pgCustomer.ProgramCustomersKey,
                                    ProgramDetailsKey = definitionStructure.levelCode,
                                    PromotionRefNumber = PromotionRefNumber,
                                    ActualQantities = 0,
                                    ActualAmount = 0,
                                    RemainAmount = 0,
                                    RemainQuantities = 0,
                                    SuggestQantities = 0,
                                    DetailLevel = definitionStructure.levelCode,
                                    DetailDescription = definitionStructure.levelName,
                                    DetailType = detailType,
                                    DetailQuantities = detailQty,
                                    DetailAmount = detailAmt,
                                    EffectiveDate = DateTime.Now,
                                    ValidUntil = null,
                                    IsDeleted = false,
                                    ProgramsBuyType = buyType,
                                    ProgramsGivingType = givingType,
                                    ProductTypeForGift = definitionStructure.productTypeForGift,
                                    ProductTypeForSale = definitionStructure.productTypeForSale,
                                    ItemHierarchyLevelForSale = definitionStructure.itemHierarchyLevelForSale,
                                    ItemHierarchyLevelForGift = definitionStructure.itemHierarchyLevelForGift,
                                    QuantityPurchased = definitionStructure.quantityPurchased,
                                    OnEachQuantity = definitionStructure.onEachQuantity,
                                    ValuePurchased = definitionStructure.valuePurchased,
                                    OnEachValue = definitionStructure.onEachValue,
                                    BudgetCode = budgetCode,
                                    BudgetType = budgetType,
                                    BudgetAllocationLevel = budgetAllocationLevel,
                                    BudgetBook = 0,
                                    BudgetBooked = 0,
                                    BudgetBookOver = false,
                                    Allowance = definitionStructure.Allowance,
                                    ProgramCustomerItemsGroup = new List<ProgramCustomerItemsGroup>()
                                };

                                //Detail ItemGroup
                                foreach (var productForSales in definitionStructure.listProductForSales)
                                {
                                    var saleType = definitionStructure.productTypeForSale;

                                    if (saleType == TP_SALE_TYPE.ITEMHIERARCHY) //phải tìm danh sách ItemGroup từ ItemHiearchy để add vào pgItemGroup 
                                    {
                                        var hierarchyLevel = definitionStructure.itemHierarchyLevelForSale;
                                        var request = new
                                        {
                                            IT01Code = hierarchyLevel == ItemSettingConst.Industry ? productForSales.productCode : null,
                                            IT02Code = hierarchyLevel == ItemSettingConst.Category ? productForSales.productCode : null,
                                            IT03Code = hierarchyLevel == ItemSettingConst.SubCategory ? productForSales.productCode : null,
                                            IT04Code = hierarchyLevel == ItemSettingConst.Brand ? productForSales.productCode : null,
                                            IT05Code = hierarchyLevel == ItemSettingConst.SubBrand ? productForSales.productCode : null,
                                            IT06Code = hierarchyLevel == ItemSettingConst.PackSize ? productForSales.productCode : null,
                                            IT07Code = hierarchyLevel == ItemSettingConst.PackType ? productForSales.productCode : null,
                                            IT08Code = hierarchyLevel == ItemSettingConst.Packaging ? productForSales.productCode : null,
                                            IT09Code = hierarchyLevel == ItemSettingConst.Weight ? productForSales.productCode : null,
                                            IT10Code = hierarchyLevel == ItemSettingConst.Volume ? productForSales.productCode : null
                                        };
                                        var result = _clientService.CommonRequest<ResultModelWithObject<List<ItemGroupFromHierarchyResult>>>(CommonData.SystemUrlCode.ODItemAPI, $"ItemGroup/GetItemGroupByHierarchy", Method.POST, UserToken, request);
                                        if (result.IsSuccess && result.Data != null)
                                        {
                                            var listItemGroup = result.Data;
                                            foreach (var itemGroup in listItemGroup)
                                            {
                                                ProgramCustomerItemsGroup pgCusItemGroup = new ProgramCustomerItemsGroup
                                                {
                                                    Id = Guid.Empty,
                                                    ProgramCustomerItemsGroupCode = pgCusDetail.ProgramCustomersDetailCode + "-" + productForSales.productCode,
                                                    ProgramCustomersDetailCode = pgCusDetail.ProgramCustomersDetailCode,
                                                    PromotionRefNumber = PromotionRefNumber,
                                                    Quantities = 0,
                                                    ItemGroupQuantities = 0,
                                                    Amount = 0,
                                                    Description = itemGroup.Description,
                                                    ItemGroupCode = itemGroup.ItemgroupCode,
                                                    UOMCode = productForSales.packing,
                                                    FixedQuantities = productForSales.sellNumber,
                                                    IsDeleted = false,
                                                    MinQty = 0,
                                                    MinAmt = 0,
                                                    ProductTypeForSale = definitionStructure.productTypeForSale,
                                                    InventoryItemCode = null,
                                                    ItemHierarchyValueForSale = productForSales.productCode

                                                };
                                                pgCusDetail.ProgramCustomerItemsGroup.Add(pgCusItemGroup);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        ProgramCustomerItemsGroup pgCusItemGroup = new ProgramCustomerItemsGroup
                                        {
                                            Id = Guid.Empty,
                                            ProgramCustomerItemsGroupCode = pgCusDetail.ProgramCustomersDetailCode + "-" + productForSales.productCode,
                                            ProgramCustomersDetailCode = pgCusDetail.ProgramCustomersDetailCode,
                                            PromotionRefNumber = PromotionRefNumber,
                                            Quantities = 0,
                                            ItemGroupQuantities = 0,
                                            Amount = 0,
                                            Description = productForSales.productDescription,
                                            ItemGroupCode = saleType == TP_SALE_TYPE.ITEMGROUP ? productForSales.productCode : null,
                                            UOMCode = productForSales.packing,
                                            FixedQuantities = productForSales.sellNumber,
                                            IsDeleted = false,
                                            MinQty = 0,
                                            MinAmt = 0,
                                            ProductTypeForSale = definitionStructure.productTypeForSale,
                                            InventoryItemCode = saleType == TP_SALE_TYPE.SKU ? productForSales.productCode : null,
                                            ItemHierarchyValueForSale = null
                                        };
                                        pgCusDetail.ProgramCustomerItemsGroup.Add(pgCusItemGroup);
                                    }

                                }

                                pgCustomer.ProgramsCustomerDetails.Add(pgCusDetail);
                            }

                            pgCustomers.Add(pgCustomer);
                        }
                    }
                }
                else
                {

                    var listpg = await _programsRepo.GetAllQueryable(x => x.ProgramsType == ProgramType, null, null, _schemaName).ToListAsync();
                    if (pgCustomers.Count > 0)
                    {
                        var pgCodes = pgCustomers.Select(x => x.ProgramCode).ToList();
                        listpg = listpg.Where(x => !pgCodes.Contains(x.ProgramCode)).ToList();
                    }

                    foreach (var pg in listpg)
                    {
                        var pgCustomer = new PromotionCustomerModel
                        {
                            Id = Guid.Empty,
                            PromotionRefNumber = PromotionRefNumber,
                            ProgramCustomersKey = PromotionRefNumber + "-" + pg.ProgramCode,
                            ProgramCode = pg.ProgramCode,
                            ProgramsType = ProgramType,
                            ProgramsDescription = pg.Description,
                            ProgramsItemScope = pg.ItemScope,
                            ShiptoCode = cusInfo.ShiptoCode,
                            CustomerCode = cusInfo.CustomerCode,
                            IsDeleted = false,
                            EffectiveDate = DateTime.Now,
                            ValidUntil = null,
                            Shipto_Attribute1 = cusInfo.Shipto_Attribute1,
                            Shipto_Attribute2 = cusInfo.Shipto_Attribute2,
                            Shipto_Attribute3 = cusInfo.Shipto_Attribute3,
                            Shipto_Attribute4 = cusInfo.Shipto_Attribute4,
                            Shipto_Attribute5 = cusInfo.Shipto_Attribute5,
                            Shipto_Attribute6 = cusInfo.Shipto_Attribute6,
                            Shipto_Attribute7 = cusInfo.Shipto_Attribute7,
                            Shipto_Attribute8 = cusInfo.Shipto_Attribute8,
                            Shipto_Attribute9 = cusInfo.Shipto_Attribute9,
                            Shipto_Attribute10 = cusInfo.Shipto_Attribute10,
                            ProgramsCustomerDetails = new List<PromotionCustomerDetailsModel>()
                        };

                        var listpgDetail = await _programsDetailsRepo.GetAllQueryable(x => x.ProgramsCode == pg.ProgramCode, null, null, _schemaName).ToListAsync();
                        foreach (var pgDetail in listpgDetail)
                        {
                            var pgCusDetail = new PromotionCustomerDetailsModel
                            {
                                Id = Guid.Empty,
                                ProgramCustomersDetailCode = pgCustomer.ProgramCustomersKey + "-" + pgDetail.ProgramDetailsKey,
                                ProgramDetailsKey = pgDetail.ProgramDetailsKey,
                                ProgramCustomersKey = pgCustomer.ProgramCustomersKey,
                                PromotionRefNumber = PromotionRefNumber,
                                ActualQantities = 0,
                                ActualAmount = 0,
                                RemainAmount = pgDetail.RequiredAmount,
                                RemainQuantities = pgDetail.RequiredQuantities,
                                SuggestQantities = pgDetail.RequiredQuantities,
                                DetailLevel = pgDetail.Level,
                                DetailDescription = pgDetail.Description,
                                DetailType = pgDetail.Type,
                                DetailQuantities = pgDetail.RequiredQuantities,
                                DetailAmount = pgDetail.RequiredAmount,
                                EffectiveDate = DateTime.Now,
                                ValidUntil = null,
                                IsDeleted = false,
                                ProgramsBuyType = pg.BuyType,
                                ProgramsGivingType = pg.GivingType,
                                ProductTypeForGift = TP_GIFT_TYPE.ITEMGROUP,
                                ProductTypeForSale = TP_SALE_TYPE.ITEMGROUP,
                                ProgramCustomerItemsGroup = new List<ProgramCustomerItemsGroup>()
                            };

                            var listDetailItemGroup = await _programsItemgroupRepo.GetAllQueryable(x => x.ProgramDetailsKey == pgDetail.ProgramDetailsKey, null, null, _schemaName).ToListAsync();

                            foreach (var detailItemGroup in listDetailItemGroup)
                            {
                                var cusDetailItemGroup = new ProgramCustomerItemsGroup
                                {
                                    Id = Guid.Empty,
                                    ProgramCustomerItemsGroupCode = pgCusDetail.ProgramCustomersDetailCode + "-" + detailItemGroup.ItemGroupCode,
                                    ProgramCustomersDetailCode = pgCusDetail.ProgramCustomersDetailCode,
                                    PromotionRefNumber = PromotionRefNumber,
                                    Quantities = 0,
                                    Amount = 0,
                                    ItemGroupCode = detailItemGroup.ItemGroupCode,
                                    Description = detailItemGroup.Description,
                                    UOMCode = detailItemGroup.UOMCode,
                                    MinQty = detailItemGroup.MinQty,
                                    MinAmt = detailItemGroup.MinAmt,
                                    FixedQuantities = detailItemGroup.FixedQuantities,
                                    IsDeleted = detailItemGroup.IsDeleted,
                                };

                                pgCusDetail.ProgramCustomerItemsGroup.Add(cusDetailItemGroup);
                            }
                            pgCustomer.ProgramsCustomerDetails.Add(pgCusDetail);
                        }

                        pgCustomers.Add(pgCustomer);

                    }
                }

                return new BaseResultModel
                {
                    Code = 200,
                    IsSuccess = true,
                    Message = "OK",
                    Data = pgCustomers
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

        public async Task<List<ProgramCustomerDetailsItems>> GetInventoryItemStdByItemGroupByQuantity(
            string customerattribute,
            PromotionCustomerDetailsModel programCustomerDetail,
            string programCustomersDetailCode,
            bool isDiscount,
            string username,
            string UserToken,
            DistributorInfoModel distributorInfo,
            string promotionType,
            string promotionRefNumber,
            string promotionCode,
            string programsBuyType,
            string itemScope,
            string givingType)
        {
            try
            {
                List<StandardItemModel> itemList = new();
                List<ProgramCustomerDetailsItems> programCustomerDetailsItems = new();
                int rewardReachedQty = 0;
                decimal rewardReachedAmt = 0;
                bool IsQualifiedReward = true;
                int levelBaseQty = 0;
                // foreach (var itemGroup in programCustomerDetail.Temp_ProgramCustomerItemsGroup.Where(x => x.ItemGroupQuantities > 0 || x.Amount > 0).ToList())
                foreach (var salesProduct in programCustomerDetail.ProgramCustomerItemsGroup)
                {
                    int totalSalesProductBaseQty = 0;
                    List<string> selectedInvItem = new();
                    if (salesProduct.ItemGroupQuantities == 0 && salesProduct.Amount == 0)
                    {
                        if ((salesProduct.MinAmt > 0 && salesProduct.Amount < salesProduct.MinAmt) || (salesProduct.MinQty > 0 && salesProduct.ItemGroupQuantities < salesProduct.MinQty))
                        {
                            IsQualifiedReward = false;
                        }
                        continue;
                    }

                    rewardReachedQty += salesProduct.ItemGroupQuantities;
                    rewardReachedAmt += salesProduct.Amount;

                    if ((programsBuyType == PROMO_BYBREAKDOWNCONST.AMOUNT && salesProduct.Amount < salesProduct.MinAmt) || (programsBuyType == PROMO_BYBREAKDOWNCONST.QUANTITY && salesProduct.ItemGroupQuantities < salesProduct.MinQty))
                    {
                        IsQualifiedReward = false;
                    }

                    if (salesProduct.ProductTypeForSale == TP_SALE_TYPE.SKU)
                    {

                        //call api Detail Items
                        var detailSelectedItem = _clientService.CommonRequest<ItemMng_InventoryItemAttribute>(CommonData.SystemUrlCode.ODItemAPI, $"InventoryItem/GetInventoryAttributeByCode/{salesProduct.InventoryItemCode}", Method.GET, $"Rdos {UserToken.Split(" ").Last()}", null);
                        //call api Detail Items
                        var detailItem = _clientService.CommonRequest<ItemMng_IventoryItemModel>(CommonData.SystemUrlCode.ODItemAPI, $"InventoryItem/GetInventoryItemByCode/{salesProduct.InventoryItemCode}", Method.GET, $"Rdos {UserToken.Split(" ").Last()}", null);
                        salesProduct.ItemGroupCode = detailItem.InventoryItem.GroupId;

                        decimal price = 0;
                        if (programsBuyType == PROMO_BYBREAKDOWNCONST.AMOUNT)
                        {
                            price = await GetPriceByItemsGroupCode(detailItem.InventoryItem.GroupId, detailSelectedItem.BaseUOMCode, distributorInfo, null);
                            salesProduct.UOMCode = detailSelectedItem.BaseUOMCode;
                            salesProduct.ItemGroupQuantities = Convert.ToInt32((salesProduct.Amount / price) + (decimal)0.5);
                        }
                        else
                        {
                            price = await GetPriceByItemsGroupCode(detailItem.InventoryItem.GroupId, salesProduct.UOMCode, distributorInfo, null);
                        }

                        //call api Detail UOM
                        var detailBaseUom = _clientService.CommonRequest<UomModel>(CommonData.SystemUrlCode.ODItemAPI, $"Uom/GetUomById/{detailItem.InventoryItem.BaseUnit}", Method.GET, $"Rdos {UserToken.Split(" ").Last()}", null);

                        int? baseQuantities = await commonGetBaseQtyFromSKU(detailItem.UomConversion, detailSelectedItem, salesProduct.UOMCode, salesProduct.ItemGroupQuantities, detailSelectedItem.BaseUOMCode);
                        var itemQuantities = baseQuantities.Value;
                        levelBaseQty += baseQuantities.Value;
                        totalSalesProductBaseQty += baseQuantities.Value;

                        // Lấy thông tin SalesUnit
                        var salesQty = await commonGetBaseQtyFromSKU(detailItem.UomConversion, detailSelectedItem, detailBaseUom.UomId, baseQuantities.Value, detailSelectedItem.SalesUOMCode);
                        bool isSalesBaseValid = false;
                        if (salesQty.HasValue)
                        {
                            var salesPrice = await GetPriceByItemsGroupCode(salesProduct.ItemGroupCode, detailSelectedItem.SalesUOMCode, distributorInfo, null);
                            if (salesPrice > 0)
                            {
                                itemQuantities = salesQty.Value;
                                isSalesBaseValid = true;
                                price = salesPrice;
                            }
                        }

                        //VatDetail
                        var vatDetail = _clientService.CommonRequest<VATDetail>(CommonData.SystemUrlCode.ODItemAPI, $"Vat/GetVatById/{detailItem.InventoryItem.Vat}", Method.GET, $"Rdos {UserToken.Split(" ").Last()}", null);

                        ProgramCustomerDetailsItems pcDetailItem = new()
                        {
                            Id = _includeSaved ? Guid.NewGuid() : Guid.Empty,
                            ProgramCustomerItemsGroupCode = salesProduct.ProgramCustomerItemsGroupCode,
                            ProgramCustomersDetailCode = programCustomersDetailCode,
                            InventoryId = detailSelectedItem.ItemCode, //Setup hàm Gen InventoryId
                            ItemCode = detailSelectedItem.ItemCode,
                            ItemId = detailSelectedItem.Id,
                            ItemDescription = detailItem.InventoryItem.Description,
                            PromotionRefNumber = salesProduct.PromotionRefNumber,
                            UOMCode = isSalesBaseValid ? detailSelectedItem.SalesUOMCode : detailSelectedItem.BaseUOMCode,
                            IsDisCountLine = false,
                            DiscountLineCode = null,
                            OrderQuantites = itemQuantities,
                            DiscountPercented = 0,
                            DisCountAmount = 0,
                            IsDeleted = false,
                            IsPromotion = true,
                            PromotionType = promotionType,
                            VatId = detailItem.InventoryItem.Vat,
                            VATCode = vatDetail.VatId,
                            VatValue = vatDetail.VatValues,
                            BaseOrderQuantities = baseQuantities.Value,
                            PromotionCode = promotionCode,
                            ItemShortName = detailItem.InventoryItem.ShortName,
                            BaseUnit = detailItem.InventoryItem.BaseUnit,
                            SalesUnit = detailItem.InventoryItem.SalesUnit,
                            PurchaseUnit = detailItem.InventoryItem.PurchaseUnit,
                            OriginalQty = salesProduct.ItemGroupQuantities,
                            OriginalAmt = salesProduct.Amount,
                            UnitPrice = price,
                            Amount = price * itemQuantities,
                            ProgramDetailDesc = programCustomerDetail.DetailDescription
                        };
                        programCustomerDetailsItems.Add(pcDetailItem);
                        selectedInvItem.Add(pcDetailItem.InventoryId);


                    }
                    else
                    {
                        #region Type ItemGroup
                        //call api GetDetailItemGroup
                        var itemGroupDetail = _clientService.CommonRequest<ItemGroupDetailModel>(CommonData.SystemUrlCode.ODItemAPI, $"ItemGroup/GetItemGroupWithUOMByCode/{salesProduct.ItemGroupCode}", Method.GET, $"Rdos {UserToken.Split(" ").Last()}", null);

                        var selectedItem = itemGroupDetail.ListInventory.Where(x => x != null).FirstOrDefault();

                        //call api Detail Items
                        var detailSelectedItem = _clientService.CommonRequest<ItemMng_InventoryItemAttribute>(CommonData.SystemUrlCode.ODItemAPI, $"InventoryItem/GetInventoryAttributeByCode/{selectedItem.InventoryItemId}", Method.GET, $"Rdos {UserToken.Split(" ").Last()}", null);

                        //Get Price Mockup
                        decimal price = 0;
                        if (programsBuyType == PROMO_BYBREAKDOWNCONST.AMOUNT)
                        {

                            // price = await GetPriceByItemsGroupCode(salesProduct.ItemGroupCode, detailSelectedItem.SalesUOMCode, distributorInfo, null);
                            // if (price > 0)
                            // {
                            //     salesProduct.UOMCode = detailSelectedItem.SalesUOMCode;
                            // }
                            // else
                            // {

                            // }

                            price = await GetPriceByItemsGroupCode(salesProduct.ItemGroupCode, detailSelectedItem.BaseUOMCode, distributorInfo, null);
                            salesProduct.UOMCode = detailSelectedItem.BaseUOMCode;
                            salesProduct.ItemGroupQuantities = Convert.ToInt32((salesProduct.Amount / price) + (decimal)0.5);
                        }
                        else
                        {
                            price = await GetPriceByItemsGroupCode(salesProduct.ItemGroupCode, salesProduct.UOMCode, distributorInfo, null);
                        }

                        int itemGroupbaseQuantites = salesProduct.ItemGroupQuantities;

                        //call api transaction
                        var resultData = _clientService.CommonRequest<List<StandardItemModel>>(CommonData.SystemUrlCode.ODItemAPI, $"Standard/GetInventoryItemStdByItemGroupByQuantity/{salesProduct.ItemGroupCode}/{itemGroupbaseQuantites}", Method.GET, $"Rdos {UserToken.Split(" ").Last()}", null);

                        if (resultData != null && resultData.Count > 0)
                        {

                            // if (promotionType == PROMO_PROMOTIONTYPECONST.Promotion)
                            // {
                            //     // Mua theo Line tặng Item LINE - AMT - FREE ITEM / LINE - QTY - FREEITEM
                            //     if (itemScope == PROMO_ITEMSCOPECONST.LINE) { }
                            // }

                            foreach (var standardItem in resultData)
                            {
                                if (standardItem.Avaiable <= 0) continue;
                                //call api Detail Items
                                var detailItem = _clientService.CommonRequest<ItemMng_IventoryItemModel>(CommonData.SystemUrlCode.ODItemAPI, $"InventoryItem/GetInventoryItemById/{standardItem.InventoryId}", Method.GET, $"Rdos {UserToken.Split(" ").Last()}", null);

                                //call api Detail UOM
                                var detailUOM = _clientService.CommonRequest<UomModel>(CommonData.SystemUrlCode.ODItemAPI, $"Uom/GetUomById/{detailItem.InventoryItem.BaseUnit}", Method.GET, $"Rdos {UserToken.Split(" ").Last()}", null);

                                int baseQuantities = standardItem.Avaiable;
                                levelBaseQty += baseQuantities;
                                totalSalesProductBaseQty += baseQuantities;
                                // Lấy thông tin SalesUnit
                                var salestoBaseUnitConverson = detailItem.UomConversion.Where(x => x.ConversionFactor != 0 && x != null && x.FromUnit == detailItem.InventoryItem.SalesUnit && x.ToUnitName == detailUOM.UomId).FirstOrDefault();
                                int saletoBasevalue = 1;
                                bool isSalesBaseValid = false;
                                if (salestoBaseUnitConverson != null)
                                {
                                    saletoBasevalue = (int)salestoBaseUnitConverson.ConversionFactor;
                                    if ((float)standardItem.Avaiable / saletoBasevalue == (int)standardItem.Avaiable / saletoBasevalue)
                                    {
                                        isSalesBaseValid = true;
                                        standardItem.Avaiable = standardItem.Avaiable / saletoBasevalue;
                                        price = await GetPriceByItemsGroupCode(salesProduct.ItemGroupCode, salestoBaseUnitConverson.FromUnitName, distributorInfo, null);
                                    }
                                    else
                                    {
                                        price = await GetPriceByItemsGroupCode(salesProduct.ItemGroupCode, detailUOM.UomId, distributorInfo, null);
                                    }
                                }
                                //VatDetail
                                var vatDetail = _clientService.CommonRequest<VATDetail>(CommonData.SystemUrlCode.ODItemAPI, $"Vat/GetVatById/{detailItem.InventoryItem.Vat}", Method.GET, $"Rdos {UserToken.Split(" ").Last()}", null);

                                ProgramCustomerDetailsItems pcDetailItem = new()
                                {
                                    Id = _includeSaved ? Guid.NewGuid() : Guid.Empty,
                                    ProgramCustomerItemsGroupCode = salesProduct.ProgramCustomerItemsGroupCode,
                                    ProgramCustomersDetailCode = programCustomersDetailCode,
                                    InventoryId = standardItem.InventoryCode, //Setup hàm Gen InventoryId
                                    ItemCode = standardItem.InventoryCode,
                                    ItemId = standardItem.InventoryId,
                                    ItemDescription = standardItem.InventoryDescription,
                                    PromotionRefNumber = salesProduct.PromotionRefNumber,
                                    UOMCode = isSalesBaseValid ? salestoBaseUnitConverson.FromUnitName : detailUOM.UomId,
                                    IsDisCountLine = false,
                                    DiscountLineCode = null,
                                    OrderQuantites = standardItem.Avaiable,
                                    DiscountPercented = 0,
                                    DisCountAmount = 0,
                                    IsDeleted = false,
                                    IsPromotion = true,
                                    PromotionType = promotionType,
                                    VatId = detailItem.InventoryItem.Vat,
                                    VATCode = vatDetail.VatId,
                                    VatValue = vatDetail.VatValues,
                                    BaseOrderQuantities = baseQuantities,
                                    PromotionCode = promotionCode,
                                    ItemShortName = detailItem.InventoryItem.ShortName,
                                    BaseUnit = detailItem.InventoryItem.BaseUnit,
                                    SalesUnit = detailItem.InventoryItem.SalesUnit,
                                    PurchaseUnit = detailItem.InventoryItem.PurchaseUnit,
                                    OriginalQty = salesProduct.ItemGroupQuantities,
                                    OriginalAmt = salesProduct.Amount,
                                    UnitPrice = price,
                                    Amount = price * standardItem.Avaiable,
                                    ProgramDetailDesc = programCustomerDetail.DetailDescription
                                };
                                programCustomerDetailsItems.Add(pcDetailItem);
                                selectedInvItem.Add(pcDetailItem.InventoryId);
                            }
                        }
                        #endregion //Endregion ItemGroup

                    }

                    if (promotionType == PROMO_PROMOTIONTYPECONST.Promotion && itemScope == PROMO_ITEMSCOPECONST.LINE && givingType != PROMO_GIVINGTYPECONST.FREEITEM && programCustomerDetail.Allowance)
                    {
                        //Get  Promotion Detail
                        var promotionDetailResult = _clientService.CommonRequest<ResultModelWithObject<TpPromotionDetailModel>>(CommonData.SystemUrlCode.ODTpAPI, $"tppromotion/Getdetailpromotionexternalbycode/{promotionCode}", Method.GET, UserToken, null, true);
                        if (promotionDetailResult.IsSuccess && promotionDetailResult.Data != null)
                        {
                            TpPromotionDetailModel promotionDetail = promotionDetailResult.Data;
                            DefinitionStructureModel definitionStructure = promotionDetail.listDefinitionStructure.Where(x => x.levelCode == programCustomerDetail.ProgramDetailsKey).FirstOrDefault();

                            foreach (var item in programCustomerDetailsItems.Where(x => selectedInvItem.Contains(x.InventoryId) && x.ProgramCustomersDetailCode == programCustomerDetail.ProgramCustomersDetailCode))
                            {
                                if (item.IsDisCountLine) continue;
                                bool isValidRew = true;

                                if (programCustomerDetail.DetailType == PROMO_RULEOFGIVING.BOX)
                                {
                                    if ((programsBuyType == PROMO_BYBREAKDOWNCONST.QUANTITY && item.OriginalQty < programCustomerDetail.QuantityPurchased) ||
                                        (programsBuyType == PROMO_BYBREAKDOWNCONST.AMOUNT && item.OriginalAmt < programCustomerDetail.ValuePurchased))
                                    {
                                        isValidRew = false;
                                    }
                                }
                                if (!isValidRew) continue;

                                int multiplyRw = programsBuyType == PROMO_BYBREAKDOWNCONST.QUANTITY ? item.OriginalQty / programCustomerDetail.DetailQuantities : (int)(item.OriginalAmt / programCustomerDetail.DetailAmount + (decimal)0.5);
                                if (givingType == PROMO_GIVINGTYPECONST.AMOUNT)
                                {
                                    decimal proportionByQuantity = (decimal)item.BaseOrderQuantities / totalSalesProductBaseQty;
                                    item.DisCountAmount = definitionStructure.amountOfDonation * multiplyRw * proportionByQuantity;
                                    // item.DisCountAmount = definitionStructure.amountOfDonation * multiplyRw;
                                }
                                if (givingType == PROMO_GIVINGTYPECONST.PERCENTED)

                                {
                                    item.DiscountPercented = (double)(definitionStructure.percentageOfAmount / 100);
                                    item.DisCountAmount = (item.Amount * (decimal)item.DiscountPercented);
                                }
                            }
                        }

                    }


                }



                if (promotionType == PROMO_PROMOTIONTYPECONST.Promotion)
                {
                    // Mua theo Line tặng Item LINE - AMT - FREE ITEM / LINE - QTY - FREEITEM
                    if (itemScope == PROMO_ITEMSCOPECONST.LINE)
                    {
                        if (givingType == PROMO_GIVINGTYPECONST.FREEITEM)
                        {
                            var rs = await CommonHandleRewardItemPromotion(
                                programCustomerDetail,
                                new List<ProgramCustomerDetailsItems>(),
                                promotionCode,
                                // multiplyRw,
                                itemScope,
                                programsBuyType,
                                promotionRefNumber,
                                UserToken);
                            if (rs != null && rs.Count > 0)
                            {
                                programCustomerDetailsItems.AddRange(rs);
                            }
                        }
                        else
                        {
                            foreach (var salesProduct in programCustomerDetail.ProgramCustomerItemsGroup)
                            {
                                if (programCustomerDetail.DetailType == PROMO_RULEOFGIVING.BOX)
                                {
                                    if (programCustomerDetail.ProgramsBuyType == PROMO_BYBREAKDOWNCONST.QUANTITY)
                                    {
                                        if (salesProduct.ItemGroupQuantities < programCustomerDetail.QuantityPurchased)
                                        {
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        if (salesProduct.Amount < programCustomerDetail.ValuePurchased)
                                        {
                                            continue;
                                        }
                                    }
                                }
                                else
                                {
                                    if (salesProduct.ItemGroupQuantities < programCustomerDetail.DetailQuantities || salesProduct.Amount < programCustomerDetail.DetailAmount)
                                    {
                                        continue;
                                    }
                                }

                                int multiplyRw = programsBuyType == PROMO_BYBREAKDOWNCONST.QUANTITY ? salesProduct.ItemGroupQuantities / programCustomerDetail.DetailQuantities : (int)(salesProduct.Amount / programCustomerDetail.DetailAmount + (decimal)0.5);

                                //Get  Promotion Detail
                                var promotionDetailResult = _clientService.CommonRequest<ResultModelWithObject<TpPromotionDetailModel>>(CommonData.SystemUrlCode.ODTpAPI, $"tppromotion/Getdetailpromotionexternalbycode/{promotionCode}", Method.GET, UserToken, null, true);
                                if (promotionDetailResult.IsSuccess && promotionDetailResult.Data != null && promotionDetailResult.Data.promotionCheckBy == PROMOTIONCHECKBY.AMOUNT)
                                {
                                    TpPromotionDetailModel promotionDetail = promotionDetailResult.Data;
                                    DefinitionStructureModel definitionStructure = promotionDetail.listDefinitionStructure.Where(x => x.levelCode == programCustomerDetail.ProgramDetailsKey).FirstOrDefault();

                                    ProgramCustomerDetailsItems pcDetailItem = new()
                                    {
                                        Id = _includeSaved ? Guid.NewGuid() : Guid.Empty,
                                        ProgramCustomersDetailCode = programCustomerDetail.ProgramCustomersDetailCode,
                                        InventoryId = "KMT",
                                        ItemCode = null,
                                        ItemDescription = null,
                                        PromotionRefNumber = promotionRefNumber,
                                        Description = "Khuyến mãi tiền",
                                        UOMCode = null,
                                        IsDisCountLine = true,
                                        DiscountLineCode = null,
                                        // OrderQuantites = (int)(giftLine.numberOfGift * multiplyRw),
                                        OrderQuantites = 1,
                                        UnitPrice = 0,
                                        Amount = 0,
                                        DiscountPercented = 0,
                                        DisCountAmount = definitionStructure.amountOfDonation * multiplyRw,
                                        IsDeleted = false,
                                        IsPromotion = true,
                                        PromotionType = PROMO_PROMOTIONTYPECONST.Promotion,
                                        PromotionCode = promotionCode,
                                        ItemShortName = null,
                                        BaseUnit = null,
                                        SalesUnit = null,
                                        PurchaseUnit = null,
                                        ItemId = null,
                                        BaseOrderQuantities = 0,
                                        VatId = null,
                                        VATCode = null,
                                        VatValue = 0,
                                        ProgramDetailDesc = programCustomerDetail.DetailDescription,
                                    };
                                }

                            }
                        }

                    }
                    else //Reward Group , Bundle
                    {
                        bool isValidRew = true;
                        int sumqty = programCustomerDetail.ProgramCustomerItemsGroup.Sum(x => x.ItemGroupQuantities);
                        decimal sumAmt = programCustomerDetail.ProgramCustomerItemsGroup.Sum(x => x.Amount);
                        if (programCustomerDetail.DetailType == PROMO_RULEOFGIVING.BOX)
                        {
                            if ((programsBuyType == PROMO_BYBREAKDOWNCONST.QUANTITY && sumqty < programCustomerDetail.QuantityPurchased) ||
                                (programsBuyType == PROMO_BYBREAKDOWNCONST.AMOUNT && sumAmt < programCustomerDetail.ValuePurchased))
                            {
                                isValidRew = false;
                            }
                        }
                        if (isValidRew)
                        {
                            if (givingType == PROMO_GIVINGTYPECONST.FREEITEM)
                            {
                                // int multiplyRw = programsBuyType == PROMO_BYBREAKDOWNCONST.QUANTITY ? itemGroup.ItemGroupQuantities / programCustomerDetail.DetailQuantities : (int)(itemGroup.Amount / programCustomerDetail.DetailAmount + (decimal)0.5);
                                var rs = await CommonHandleRewardItemPromotion(
                                    programCustomerDetail,
                                    new List<ProgramCustomerDetailsItems>(),
                                    promotionCode,
                                    itemScope,
                                    programsBuyType,
                                    promotionRefNumber,
                                    UserToken);

                                if (rs != null && rs.Count > 0)
                                {
                                    programCustomerDetailsItems.AddRange(rs);
                                }
                            }
                            else
                            {
                                //Get  Promotion Detail
                                var promotionDetailResult = _clientService.CommonRequest<ResultModelWithObject<TpPromotionDetailModel>>(CommonData.SystemUrlCode.ODTpAPI, $"tppromotion/Getdetailpromotionexternalbycode/{promotionCode}", Method.GET, UserToken, null, true);
                                if (promotionDetailResult.IsSuccess && promotionDetailResult.Data != null)
                                {
                                    TpPromotionDetailModel promotionDetail = promotionDetailResult.Data;
                                    DefinitionStructureModel definitionStructure = promotionDetail.listDefinitionStructure.Where(x => x.levelCode == programCustomerDetail.ProgramDetailsKey).FirstOrDefault();
                                    if (definitionStructure.Allowance)
                                    {
                                        foreach (var item in programCustomerDetailsItems)
                                        {
                                            if (item.IsDisCountLine) continue;
                                            int multiplyRw = programCustomerDetail.ActualQantities;
                                            if (givingType == PROMO_GIVINGTYPECONST.AMOUNT)
                                            {
                                                decimal proportionByQuantity = (decimal)item.BaseOrderQuantities / levelBaseQty;
                                                item.DisCountAmount = definitionStructure.amountOfDonation * multiplyRw * proportionByQuantity;
                                            }
                                            if (givingType == PROMO_GIVINGTYPECONST.PERCENTED)

                                            {
                                                item.DiscountPercented = (double)(definitionStructure.percentageOfAmount / 100);
                                                item.DisCountAmount = (item.Amount * (decimal)item.DiscountPercented);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (promotionDetailResult.Data.promotionCheckBy == PROMOTIONCHECKBY.AMOUNT)
                                        {
                                            ProgramCustomerDetailsItems pcDetailItem = new()
                                            {
                                                Id = _includeSaved ? Guid.NewGuid() : Guid.Empty,
                                                ProgramCustomersDetailCode = programCustomerDetail.ProgramCustomersDetailCode,
                                                InventoryId = "KMT",
                                                ItemCode = null,
                                                ItemDescription = null,
                                                PromotionRefNumber = promotionRefNumber,
                                                Description = "Khuyến mãi tiền",
                                                UOMCode = null,
                                                IsDisCountLine = true,
                                                DiscountLineCode = null,
                                                // OrderQuantites = (int)(giftLine.numberOfGift * multiplyRw),
                                                OrderQuantites = 1,
                                                UnitPrice = 0,
                                                Amount = 0,
                                                DiscountPercented = 0,
                                                DisCountAmount = definitionStructure.amountOfDonation * programCustomerDetail.ActualQantities,
                                                IsDeleted = false,
                                                IsPromotion = true,
                                                PromotionType = PROMO_PROMOTIONTYPECONST.Promotion,
                                                PromotionCode = promotionCode,
                                                ItemShortName = null,
                                                BaseUnit = null,
                                                SalesUnit = null,
                                                PurchaseUnit = null,
                                                ItemId = null,
                                                BaseOrderQuantities = 0,
                                                VatId = null,
                                                VATCode = null,
                                                VatValue = 0,
                                                ProgramDetailDesc = programCustomerDetail.DetailDescription,
                                            };
                                            programCustomerDetailsItems.Add(pcDetailItem);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    #region old reward flow - use for accumulate & Display
                    if (IsQualifiedReward)
                    {
                        var promotionDetail = await _programsDetailsRepo.GetAllQueryable(x => x.ProgramDetailsKey == programCustomerDetail.ProgramDetailsKey, null, null, _schemaName).FirstOrDefaultAsync();
                        var promotionData = await _programsRepo.GetAllQueryable(x => x.ProgramCode == promotionDetail.ProgramsCode, null, null, _schemaName).FirstOrDefaultAsync();
                        List<Temp_ProgramDetailReward> reward = null;
                        if (promotionData.ItemScope != "BUNDLE")
                        {
                            reward = await _ProgramDetailRewardRepo.GetAllQueryable(null, null, null, _schemaName).Where(x => x.ProgramDetailsKey == programCustomerDetail.ProgramDetailsKey && (promotionData.BuyType == "AMOUNT" ? rewardReachedAmt >= promotionDetail.RequiredAmount : rewardReachedQty >= promotionDetail.RequiredQuantities)).ToListAsync();
                        }
                        else
                        {
                            reward = await _ProgramDetailRewardRepo.GetAllQueryable(null, null, null, _schemaName).Where(x => x.ProgramDetailsKey == programCustomerDetail.ProgramDetailsKey && programCustomerDetail.ActualQantities >= promotionDetail.RequiredQuantities).ToListAsync();
                        }

                        int mutipleRewar;
                        if (promotionData.BuyType == "AMOUNT")
                        {
                            mutipleRewar = (int)(rewardReachedAmt / promotionDetail.RequiredAmount + (decimal)0.5);
                        }
                        else
                        {
                            if (promotionData.ItemScope != "BUNDLE")
                            {
                                mutipleRewar = (int)(rewardReachedQty / promotionDetail.RequiredQuantities + (decimal)0.5);
                            }
                            else
                            {
                                mutipleRewar = (int)(programCustomerDetail.ActualQantities / programCustomerDetail.DetailQuantities + (decimal)0.5);
                            }
                        }

                        if (reward != null)
                        {
                            foreach (var item in reward)
                            {
                                if (string.IsNullOrWhiteSpace(item.Type))
                                {
                                    continue;
                                }
                                if (item.Type == "PERCENT")
                                {
                                    foreach (var detailsItem in programCustomerDetailsItems)
                                    {
                                        if (!detailsItem.IsDisCountLine)
                                        {
                                            detailsItem.DiscountPercented += item.DiscountPercented;
                                            detailsItem.DisCountAmount = detailsItem.Amount * (decimal)detailsItem.DiscountPercented;
                                        }
                                    }
                                }
                                else if (item.Type == "AMOUNT")
                                {
                                    decimal rewardAmt = item.Amount * (decimal)mutipleRewar;
                                    foreach (var detailsItem in programCustomerDetailsItems)
                                    {
                                        decimal proportionByQuantity = (decimal)detailsItem.BaseOrderQuantities / levelBaseQty;
                                        if (!detailsItem.IsDisCountLine)
                                        {
                                            detailsItem.DisCountAmount = rewardAmt * proportionByQuantity;
                                        }
                                    }
                                }
                                else
                                {
                                    //call api Detail Items
                                    var detailItem = _clientService.CommonRequest<ItemMng_IventoryItemModel>(CommonData.SystemUrlCode.ODItemAPI, $"InventoryItem/GetInventoryItemById/{item.ItemId}", Method.GET, $"Rdos {UserToken.Split(" ").Last()}", null);

                                    VATDetail vatDetail = new VATDetail();
                                    if (detailItem != null)
                                    {
                                        //call api Detail VAT
                                        vatDetail = _clientService.CommonRequest<VATDetail>(CommonData.SystemUrlCode.ODItemAPI, $"Vat/GetVatById/{detailItem.InventoryItem.Vat}", Method.GET, $"Rdos {UserToken.Split(" ").Last()}", null);
                                    }

                                    ProgramCustomerDetailsItems pcDetailItem = new()
                                    {
                                        Id = _includeSaved ? Guid.NewGuid() : Guid.Empty,
                                        ProgramCustomersDetailCode = programCustomersDetailCode,
                                        InventoryId = item.ItemCode,
                                        ItemCode = item.ItemCode != null ? item.ItemCode : null,
                                        ItemDescription = detailItem != null ? detailItem.InventoryItem.Description : null,
                                        PromotionRefNumber = promotionRefNumber,
                                        Description = item.Description,
                                        UOMCode = item.UOMCode,
                                        IsDisCountLine = true,
                                        DiscountLineCode = null,
                                        OrderQuantites = (int)(item.Quantities * mutipleRewar),
                                        UnitPrice = 0,
                                        Amount = 0,
                                        DiscountPercented = 0,
                                        DisCountAmount = 0,
                                        IsDeleted = false,
                                        IsPromotion = true,
                                        PromotionType = promotionType,
                                        PromotionCode = promotionCode,
                                        ItemShortName = detailItem.InventoryItem.ShortName,
                                        BaseUnit = detailItem.InventoryItem.BaseUnit,
                                        SalesUnit = detailItem.InventoryItem.SalesUnit,
                                        PurchaseUnit = detailItem.InventoryItem.PurchaseUnit,
                                        ItemId = detailItem.InventoryItem.Id,
                                        BaseOrderQuantities = (int)(item.BaseQuantities * mutipleRewar),
                                        VatId = detailItem.InventoryItem.Vat,
                                        VATCode = vatDetail.VatId,
                                        VatValue = vatDetail.VatValues,
                                        ProgramDetailDesc = programCustomerDetail.DetailDescription,
                                    };
                                    programCustomerDetailsItems.Add(pcDetailItem);
                                }

                            }
                        }
                    }

                    #endregion

                }

                return programCustomerDetailsItems;
            }
            catch (System.Exception ex)
            {
                return new List<ProgramCustomerDetailsItems>();
            }
        }

        // #region Get reward Infor

        // public async Task<object> GetReward(Temp_PromotionCustomerDetailsModel programCustomerDetail , string promotionCode , string itemScope)
        // {
        //     try
        //     {

        //     }
        //     catch (System.Exception ex)
        //     {
        //         return null;
        //     }
        // }
        // #endregion
        #region Common

        public async Task<int> commonGetBaseQtyWithSKU(ItemMng_IventoryItemModel detailSKU, ProductForGiftModel giftLine)
        {
            var detaiAttlSKU = _clientService.CommonRequest<ItemMng_InventoryItemAttribute>(CommonData.SystemUrlCode.SystemAdminAPI, $"InventoryItem/GetInventoryAttributeByCode/{giftLine.productCode}", Method.GET, $"Rdos {UserToken.Split(" ").Last()}", null);
            //call api Detail VAT
            int baseQty = 0;
            if (detaiAttlSKU.BaseUOMCode == giftLine.packing)
            {
                baseQty = giftLine.numberOfGift;
            }
            else
            {
                var detailFromConversion = detailSKU.UomConversion.Where(x => x.FromUnitName == giftLine.packing && x.ToUnitName == detaiAttlSKU.BaseUOMCode).FirstOrDefault();
                if (detailFromConversion != null)
                {
                    var dm = detailFromConversion.DM;
                    var unitRate = (int)detailFromConversion.ConversionFactor;
                    baseQty = dm == 1 ? giftLine.numberOfGift * unitRate : (int)(giftLine.numberOfGift / unitRate);
                }
                else
                {
                    var detailToConversion = detailSKU.UomConversion.Where(x => x.ToUnitName == giftLine.packing && x.FromUnitName == detaiAttlSKU.BaseUOMCode).FirstOrDefault();
                    if (detailToConversion != null)
                    {
                        var dm = detailToConversion.DM;
                        var unitRate = (int)detailToConversion.ConversionFactor;
                        baseQty = dm == 1 ? (int)(giftLine.numberOfGift / unitRate) : giftLine.numberOfGift * unitRate;
                    }
                }
            }
            return baseQty;
        }

        public async Task<int> commonGetBaseQtyWithItemGroup(string itemGroupCode, string uomCode, int qty)
        {
            int baseQty = 0;
            //call api GetDetailItemGroup
            var itemGroupDetail = _clientService.CommonRequest<ItemGroupDetailModel>(CommonData.SystemUrlCode.SystemAdminAPI, $"ItemGroup/GetItemGroupWithUOMByCode/{itemGroupCode}", Method.GET, $"Rdos {UserToken.Split(" ").Last()}", null);

            var selectedItem = itemGroupDetail.ListInventory.Where(x => x != null).FirstOrDefault();

            //call api Detail Items
            var detailSelectedItem = _clientService.CommonRequest<ItemMng_InventoryItemAttribute>(CommonData.SystemUrlCode.SystemAdminAPI, $"InventoryItem/GetInventoryAttributeByCode/{selectedItem.InventoryItemId}", Method.GET, $"Rdos {UserToken.Split(" ").Last()}", null);
            if (detailSelectedItem.BaseUOMCode == uomCode)
            {
                baseQty = qty;
            }
            if (detailSelectedItem.BaseUOMCode != uomCode)
            {
                var detailFromConversion = selectedItem.UomConversion.Where(x => x != null && x.FromUnitName == uomCode).FirstOrDefault();

                if (detailFromConversion != null)
                {
                    var dm = detailFromConversion.DM;
                    var unitRate = (int)detailFromConversion.ConversionFactor;
                    baseQty = dm == 1 ? qty * unitRate : (int)(qty / unitRate);
                }
                else
                {
                    var detailToConversion = selectedItem.UomConversion.Where(x => x.ToUnitName == uomCode && x.FromUnitName == detailSelectedItem.BaseUOMCode).FirstOrDefault();
                    if (detailToConversion != null)
                    {
                        var dm = detailToConversion.DM;
                        var unitRate = (int)detailToConversion.ConversionFactor;
                        baseQty = dm == 1 ? (int)(qty / unitRate) : qty * unitRate;
                    }
                }
            }
            return baseQty;
        }

        public async Task<int?> commonGetBaseQtyFromSKU(List<UomConversionModel> UomConversion, ItemMng_InventoryItemAttribute detailSelectedItem, string currentUom, int currentQty, string uomToChange)
        {
            int returnQty = 0;

            if (uomToChange == currentUom)
            {
                returnQty = currentQty;
            }
            if (uomToChange != currentUom)
            {
                var detailFromConversion = UomConversion.Where(x => x != null && x.FromUnitName == currentUom && x.ToUnitName == uomToChange).FirstOrDefault();
                // from UOM to Base UOM => BaseUOm = from * unitRate
                if (detailFromConversion != null)
                {
                    var dm = detailFromConversion.DM;
                    var unitRate = (int)detailFromConversion.ConversionFactor;
                    returnQty = dm == 1 ? currentQty * unitRate : (int)(currentQty / unitRate);
                }
                else
                {
                    // from Base UOM to  UOM => BaseUOm = from / unitRate
                    var detailToConversion = UomConversion.Where(x => x.ToUnitName == currentUom && x.FromUnitName == uomToChange).FirstOrDefault();
                    if (detailToConversion != null)
                    {
                        var dm = detailToConversion.DM;
                        var unitRate = (int)detailToConversion.ConversionFactor;
                        returnQty = dm == 1 ? (int)(currentQty / unitRate) : currentQty * unitRate;
                    }
                }
            }

            return returnQty;
        }

        async Task<List<ProgramCustomerDetailsItems>> CommonHandleRewardItemPromotion(
            PromotionCustomerDetailsModel programCustomerDetail,
            List<ProgramCustomerDetailsItems> programCustomerDetailsItems,
            string promotionCode,
            string itemScope,
            string programBuyType,
            string promotionRefNumber,
            string UserToken)
        {
            try
            {
                // List<Temp_ProgramCustomerDetailsItems> programCustomerDetailsItems = new();
                var pgGifts = _clientService.CommonRequest<List<ProductForGiftModel>>(CommonData.SystemUrlCode.ODTpAPI, $"tppromotion/getlistproductforgiftbypromotioncode/{promotionCode}", Method.GET, UserToken, null, true);

                // Nếu promotion ItemScope thuộc LINE
                if (itemScope == PROMO_ITEMSCOPECONST.LINE)
                {
                    foreach (var salesProduct in programCustomerDetail.ProgramCustomerItemsGroup)
                    {
                        if (programCustomerDetail.DetailType == PROMO_RULEOFGIVING.BOX)
                        {
                            if (programCustomerDetail.ProgramsBuyType == PROMO_BYBREAKDOWNCONST.QUANTITY)
                            {
                                if (salesProduct.ItemGroupQuantities < programCustomerDetail.QuantityPurchased)
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                if (salesProduct.Amount < programCustomerDetail.ValuePurchased)
                                {
                                    continue;
                                }
                            }
                        }
                        else
                        {
                            if (salesProduct.ItemGroupQuantities < programCustomerDetail.DetailQuantities || salesProduct.Amount < programCustomerDetail.DetailAmount)
                            {
                                continue;
                            }
                        }

                        int multiplyRw = programBuyType == PROMO_BYBREAKDOWNCONST.QUANTITY ? salesProduct.ItemGroupQuantities / programCustomerDetail.DetailQuantities : (int)(salesProduct.Amount / programCustomerDetail.DetailAmount + (decimal)0.5);
                        var selectedProducCode = salesProduct.ProductTypeForSale == TP_SALE_TYPE.SKU ? salesProduct.InventoryItemCode : salesProduct.ProductTypeForSale == TP_SALE_TYPE.ITEMGROUP ? salesProduct.ItemGroupCode : salesProduct.ItemHierarchyValueForSale;
                        var giftLine = pgGifts.Where(x => x.levelCode == programCustomerDetail.ProgramDetailsKey && (x.productCode == selectedProducCode || x.isDefaultProduct)).FirstOrDefault();
                        if (giftLine != null)
                        {
                            if (!programCustomerDetail.Allowance)
                            {
                                //Get  Promotion Detail
                                var promotionDetailResult = _clientService.CommonRequest<ResultModelWithObject<TpPromotionDetailModel>>(CommonData.SystemUrlCode.ODTpAPI, $"tppromotion/Getdetailpromotionexternalbycode/{promotionCode}", Method.GET, UserToken, null, true);
                                if (promotionDetailResult.IsSuccess && promotionDetailResult.Data != null && promotionDetailResult.Data.promotionCheckBy == PROMOTIONCHECKBY.AMOUNT)
                                {
                                    TpPromotionDetailModel promotionDetail = promotionDetailResult.Data;
                                    DefinitionStructureModel definitionStructure = promotionDetail.listDefinitionStructure.Where(x => x.levelCode == programCustomerDetail.ProgramDetailsKey).FirstOrDefault();

                                    ProgramCustomerDetailsItems pcDetailItem = new()
                                    {
                                        Id = _includeSaved ? Guid.NewGuid() : Guid.Empty,
                                        ProgramCustomersDetailCode = programCustomerDetail.ProgramCustomersDetailCode,
                                        InventoryId = "KMT",
                                        ItemCode = null,
                                        ItemDescription = null,
                                        PromotionRefNumber = promotionRefNumber,
                                        Description = "Khuyến mãi tiền",
                                        UOMCode = null,
                                        IsDisCountLine = true,
                                        DiscountLineCode = null,
                                        // OrderQuantites = (int)(giftLine.numberOfGift * multiplyRw),
                                        OrderQuantites = 1,
                                        UnitPrice = 0,
                                        Amount = 0,
                                        DiscountPercented = 0,
                                        DisCountAmount = definitionStructure.amountOfDonation * programCustomerDetail.ActualQantities,
                                        IsDeleted = false,
                                        IsPromotion = true,
                                        PromotionType = PROMO_PROMOTIONTYPECONST.Promotion,
                                        PromotionCode = promotionCode,
                                        ItemShortName = null,
                                        BaseUnit = null,
                                        SalesUnit = null,
                                        PurchaseUnit = null,
                                        ItemId = null,
                                        BaseOrderQuantities = 0,
                                        VatId = null,
                                        VATCode = null,
                                        VatValue = 0,
                                        ProgramDetailDesc = programCustomerDetail.DetailDescription,
                                    };
                                    programCustomerDetailsItems.Add(pcDetailItem);
                                }
                            }

                            if (giftLine.productType == TP_GIFT_TYPE.SKU)
                            {
                                var detailSKU = _clientService.CommonRequest<ItemMng_IventoryItemModel>(CommonData.SystemUrlCode.SystemAdminAPI, $"InventoryItem/GetInventoryItemByCode/{giftLine.productCode}", Method.GET, $"Rdos {UserToken.Split(" ").Last()}", null);

                                if (detailSKU != null)
                                {
                                    //get Base qty
                                    var baseQty = await commonGetBaseQtyWithSKU(detailSKU, giftLine);
                                    VATDetail vatDetail = _clientService.CommonRequest<VATDetail>(CommonData.SystemUrlCode.SystemAdminAPI, $"Vat/GetVatById/{detailSKU.InventoryItem.Vat}", Method.GET, $"Rdos {UserToken.Split(" ").Last()}", null);
                                    ProgramCustomerDetailsItems pcDetailItem = new()
                                    {
                                        Id = _includeSaved ? Guid.NewGuid() : Guid.Empty,
                                        ProgramCustomersDetailCode = programCustomerDetail.ProgramCustomersDetailCode,
                                        InventoryId = detailSKU.InventoryItem.InventoryItemId,
                                        ItemCode = detailSKU.InventoryItem.InventoryItemId,
                                        ItemDescription = detailSKU.InventoryItem.Description,
                                        PromotionRefNumber = promotionRefNumber,
                                        Description = giftLine.productDescription,
                                        UOMCode = giftLine.packing,
                                        IsDisCountLine = true,
                                        DiscountLineCode = null,
                                        OrderQuantites = (int)(giftLine.numberOfGift * multiplyRw),
                                        UnitPrice = 0,
                                        Amount = 0,
                                        DiscountPercented = 0,
                                        DisCountAmount = 0,
                                        IsDeleted = false,
                                        IsPromotion = true,
                                        PromotionType = PROMO_PROMOTIONTYPECONST.Promotion,
                                        PromotionCode = promotionCode,
                                        ItemShortName = detailSKU.InventoryItem.ShortName,
                                        BaseUnit = detailSKU.InventoryItem.BaseUnit,
                                        SalesUnit = detailSKU.InventoryItem.SalesUnit,
                                        PurchaseUnit = detailSKU.InventoryItem.PurchaseUnit,
                                        ItemId = detailSKU.InventoryItem.Id,
                                        BaseOrderQuantities = baseQty,
                                        VatId = detailSKU.InventoryItem.Vat,
                                        VATCode = vatDetail.VatId,
                                        VatValue = vatDetail.VatValues,
                                        ProgramDetailDesc = programCustomerDetail.DetailDescription,
                                    };
                                    programCustomerDetailsItems.Add(pcDetailItem);
                                }
                            }
                            else
                            {
                                var itemGroupCode = giftLine.productCode;
                                if (giftLine.productType == TP_GIFT_TYPE.ITEMHIERARCHY)
                                {
                                    var hierarchyLevel = programCustomerDetail.ItemHierarchyLevelForGift;
                                    var request = new
                                    {
                                        IT01Code = hierarchyLevel == ItemSettingConst.Industry ? giftLine.productCode : null,
                                        IT02Code = hierarchyLevel == ItemSettingConst.Category ? giftLine.productCode : null,
                                        IT03Code = hierarchyLevel == ItemSettingConst.SubCategory ? giftLine.productCode : null,
                                        IT04Code = hierarchyLevel == ItemSettingConst.Brand ? giftLine.productCode : null,
                                        IT05Code = hierarchyLevel == ItemSettingConst.SubBrand ? giftLine.productCode : null,
                                        IT06Code = hierarchyLevel == ItemSettingConst.PackSize ? giftLine.productCode : null,
                                        IT07Code = hierarchyLevel == ItemSettingConst.PackType ? giftLine.productCode : null,
                                        IT08Code = hierarchyLevel == ItemSettingConst.Packaging ? giftLine.productCode : null,
                                        IT09Code = hierarchyLevel == ItemSettingConst.Weight ? giftLine.productCode : null,
                                        IT10Code = hierarchyLevel == ItemSettingConst.Volume ? giftLine.productCode : null
                                    };
                                    var result = _clientService.CommonRequest<ResultModelWithObject<List<ItemGroupFromHierarchyResult>>>(CommonData.SystemUrlCode.SystemAdminAPI, $"ItemGroup/GetItemGroupByHierarchy", Method.POST, UserToken, request);
                                    //Tìm Itemgroup
                                    if (result.IsSuccess && result.Data != null)
                                    {
                                        itemGroupCode = result.Data.Select(x => x.ItemgroupCode).FirstOrDefault();
                                    }

                                }
                                var itemGroupBaseQty = await commonGetBaseQtyWithItemGroup(itemGroupCode, giftLine.packing, giftLine.numberOfGift * multiplyRw);

                                //call api transaction
                                var standardSkus = _clientService.CommonRequest<List<StandardItemModel>>(CommonData.SystemUrlCode.SystemAdminAPI, $"Standard/GetInventoryItemStdByItemGroupByQuantity/{itemGroupCode}/{itemGroupBaseQty}", Method.GET, $"Rdos {UserToken.Split(" ").Last()}", null);

                                if (standardSkus != null && standardSkus.Count > 0)
                                {
                                    foreach (var standardItem in standardSkus)
                                    {
                                        //call api Detail Items
                                        var detailSKU = _clientService.CommonRequest<ItemMng_IventoryItemModel>(CommonData.SystemUrlCode.SystemAdminAPI, $"InventoryItem/GetInventoryItemById/{standardItem.InventoryId}", Method.GET, $"Rdos {UserToken.Split(" ").Last()}", null);

                                        VATDetail vatDetail = _clientService.CommonRequest<VATDetail>(CommonData.SystemUrlCode.SystemAdminAPI, $"Vat/GetVatById/{detailSKU.InventoryItem.Vat}", Method.GET, $"Rdos {UserToken.Split(" ").Last()}", null);
                                        //call api Detail Items
                                        var detailSelectedItem = _clientService.CommonRequest<ItemMng_InventoryItemAttribute>(CommonData.SystemUrlCode.SystemAdminAPI, $"InventoryItem/GetInventoryAttributeByCode/{standardItem.InventoryCode}", Method.GET, $"Rdos {UserToken.Split(" ").Last()}", null);

                                        ProgramCustomerDetailsItems pcDetailItem = new()
                                        {
                                            Id = _includeSaved ? Guid.NewGuid() : Guid.Empty,
                                            ProgramCustomersDetailCode = programCustomerDetail.ProgramCustomersDetailCode,
                                            InventoryId = detailSKU.InventoryItem.InventoryItemId,
                                            ItemCode = detailSKU.InventoryItem.InventoryItemId,
                                            ItemDescription = detailSKU.InventoryItem.Description,
                                            PromotionRefNumber = promotionRefNumber,
                                            Description = giftLine.productDescription,
                                            UOMCode = detailSelectedItem.BaseUOMCode,
                                            IsDisCountLine = true,
                                            DiscountLineCode = null,
                                            // OrderQuantites = (int)(giftLine.numberOfGift * multiplyRw),
                                            OrderQuantites = standardItem.Avaiable,
                                            UnitPrice = 0,
                                            Amount = 0,
                                            DiscountPercented = 0,
                                            DisCountAmount = 0,
                                            IsDeleted = false,
                                            IsPromotion = true,
                                            PromotionType = PROMO_PROMOTIONTYPECONST.Promotion,
                                            PromotionCode = promotionCode,
                                            ItemShortName = detailSKU.InventoryItem.ShortName,
                                            BaseUnit = detailSKU.InventoryItem.BaseUnit,
                                            SalesUnit = detailSKU.InventoryItem.SalesUnit,
                                            PurchaseUnit = detailSKU.InventoryItem.PurchaseUnit,
                                            ItemId = detailSKU.InventoryItem.Id,
                                            BaseOrderQuantities = standardItem.Avaiable,
                                            VatId = detailSKU.InventoryItem.Vat,
                                            VATCode = vatDetail.VatId,
                                            VatValue = vatDetail.VatValues,
                                            ProgramDetailDesc = programCustomerDetail.DetailDescription,
                                        };
                                        programCustomerDetailsItems.Add(pcDetailItem);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    bool isValidRew = true;
                    int sumqty = programCustomerDetail.ProgramCustomerItemsGroup.Sum(x => x.ItemGroupQuantities);
                    decimal sumAmt = programCustomerDetail.ProgramCustomerItemsGroup.Sum(x => x.Amount);
                    if (programCustomerDetail.DetailType == PROMO_RULEOFGIVING.BOX)
                    {
                        if ((programBuyType == PROMO_BYBREAKDOWNCONST.QUANTITY && sumqty < programCustomerDetail.QuantityPurchased) ||
                            (programBuyType == PROMO_BYBREAKDOWNCONST.AMOUNT && sumAmt < programCustomerDetail.ValuePurchased))
                        {
                            isValidRew = false;
                        }
                    }

                    if (isValidRew)
                    {
                        var giftLine = pgGifts.Where(x => x.levelCode == programCustomerDetail.ProgramDetailsKey && x.isDefaultProduct).FirstOrDefault();
                        int multiplyRw = programCustomerDetail.ActualQantities;
                        if (!programCustomerDetail.Allowance)
                        {
                            //Get  Promotion Detail
                            var promotionDetailResult = _clientService.CommonRequest<ResultModelWithObject<TpPromotionDetailModel>>(CommonData.SystemUrlCode.ODTpAPI, $"tppromotion/Getdetailpromotionexternalbycode/{promotionCode}", Method.GET, UserToken, null, true);
                            if (promotionDetailResult.IsSuccess && promotionDetailResult.Data != null && promotionDetailResult.Data.promotionCheckBy == PROMOTIONCHECKBY.AMOUNT)
                            {
                                TpPromotionDetailModel promotionDetail = promotionDetailResult.Data;
                                DefinitionStructureModel definitionStructure = promotionDetail.listDefinitionStructure.Where(x => x.levelCode == programCustomerDetail.ProgramDetailsKey).FirstOrDefault();

                                ProgramCustomerDetailsItems pcDetailItem = new()
                                {
                                    Id = _includeSaved ? Guid.NewGuid() : Guid.Empty,
                                    ProgramCustomersDetailCode = programCustomerDetail.ProgramCustomersDetailCode,
                                    InventoryId = "KMT",
                                    ItemCode = null,
                                    ItemDescription = null,
                                    PromotionRefNumber = promotionRefNumber,
                                    Description = "Khuyến mãi tiền",
                                    UOMCode = null,
                                    IsDisCountLine = true,
                                    DiscountLineCode = null,
                                    // OrderQuantites = (int)(giftLine.numberOfGift * multiplyRw),
                                    OrderQuantites = 1,
                                    UnitPrice = 0,
                                    Amount = 0,
                                    DiscountPercented = 0,
                                    DisCountAmount = definitionStructure.amountOfDonation * programCustomerDetail.ActualQantities,
                                    IsDeleted = false,
                                    IsPromotion = true,
                                    PromotionType = PROMO_PROMOTIONTYPECONST.Promotion,
                                    PromotionCode = promotionCode,
                                    ItemShortName = null,
                                    BaseUnit = null,
                                    SalesUnit = null,
                                    PurchaseUnit = null,
                                    ItemId = null,
                                    BaseOrderQuantities = 0,
                                    VatId = null,
                                    VATCode = null,
                                    VatValue = 0,
                                    ProgramDetailDesc = programCustomerDetail.DetailDescription,
                                };
                                programCustomerDetailsItems.Add(pcDetailItem);
                            }
                        }
                        if (giftLine != null)
                        {
                            if (giftLine.productType == TP_GIFT_TYPE.SKU)
                            {
                                var detailSKU = _clientService.CommonRequest<ItemMng_IventoryItemModel>(CommonData.SystemUrlCode.SystemAdminAPI, $"InventoryItem/GetInventoryItemByCode/{giftLine.productCode}", Method.GET, $"Rdos {UserToken.Split(" ").Last()}", null);

                                if (detailSKU != null)
                                {
                                    //get Base qty
                                    var baseQty = await commonGetBaseQtyWithSKU(detailSKU, giftLine);
                                    VATDetail vatDetail = _clientService.CommonRequest<VATDetail>(CommonData.SystemUrlCode.SystemAdminAPI, $"Vat/GetVatById/{detailSKU.InventoryItem.Vat}", Method.GET, $"Rdos {UserToken.Split(" ").Last()}", null);
                                    ProgramCustomerDetailsItems pcDetailItem = new()
                                    {
                                        Id = _includeSaved ? Guid.NewGuid() : Guid.Empty,
                                        ProgramCustomersDetailCode = programCustomerDetail.ProgramCustomersDetailCode,
                                        InventoryId = detailSKU.InventoryItem.InventoryItemId,
                                        ItemCode = detailSKU.InventoryItem.InventoryItemId,
                                        ItemDescription = detailSKU.InventoryItem.Description,
                                        PromotionRefNumber = promotionRefNumber,
                                        Description = giftLine.productDescription,
                                        UOMCode = giftLine.packing,
                                        IsDisCountLine = true,
                                        DiscountLineCode = null,
                                        OrderQuantites = (int)(giftLine.numberOfGift * multiplyRw),
                                        UnitPrice = 0,
                                        Amount = 0,
                                        DiscountPercented = 0,
                                        DisCountAmount = 0,
                                        IsDeleted = false,
                                        IsPromotion = true,
                                        PromotionType = PROMO_PROMOTIONTYPECONST.Promotion,
                                        PromotionCode = promotionCode,
                                        ItemShortName = detailSKU.InventoryItem.ShortName,
                                        BaseUnit = detailSKU.InventoryItem.BaseUnit,
                                        SalesUnit = detailSKU.InventoryItem.SalesUnit,
                                        PurchaseUnit = detailSKU.InventoryItem.PurchaseUnit,
                                        ItemId = detailSKU.InventoryItem.Id,
                                        BaseOrderQuantities = (int)(baseQty * multiplyRw),
                                        VatId = detailSKU.InventoryItem.Vat,
                                        VATCode = vatDetail.VatId,
                                        VatValue = vatDetail.VatValues,
                                        ProgramDetailDesc = programCustomerDetail.DetailDescription,
                                    };
                                    programCustomerDetailsItems.Add(pcDetailItem);
                                }
                            }
                            else
                            {
                                var itemGroupCode = giftLine.productCode;
                                if (giftLine.productType == TP_GIFT_TYPE.ITEMHIERARCHY)
                                {
                                    var hierarchyLevel = programCustomerDetail.ItemHierarchyLevelForGift;
                                    var request = new
                                    {
                                        IT01Code = hierarchyLevel == ItemSettingConst.Industry ? giftLine.productCode : null,
                                        IT02Code = hierarchyLevel == ItemSettingConst.Category ? giftLine.productCode : null,
                                        IT03Code = hierarchyLevel == ItemSettingConst.SubCategory ? giftLine.productCode : null,
                                        IT04Code = hierarchyLevel == ItemSettingConst.Brand ? giftLine.productCode : null,
                                        IT05Code = hierarchyLevel == ItemSettingConst.SubBrand ? giftLine.productCode : null,
                                        IT06Code = hierarchyLevel == ItemSettingConst.PackSize ? giftLine.productCode : null,
                                        IT07Code = hierarchyLevel == ItemSettingConst.PackType ? giftLine.productCode : null,
                                        IT08Code = hierarchyLevel == ItemSettingConst.Packaging ? giftLine.productCode : null,
                                        IT09Code = hierarchyLevel == ItemSettingConst.Weight ? giftLine.productCode : null,
                                        IT10Code = hierarchyLevel == ItemSettingConst.Volume ? giftLine.productCode : null
                                    };
                                    var result = _clientService.CommonRequest<ResultModelWithObject<List<ItemGroupFromHierarchyResult>>>(CommonData.SystemUrlCode.SystemAdminAPI, $"ItemGroup/GetItemGroupByHierarchy", Method.POST, UserToken, request);
                                    //Tìm Itemgroup
                                    if (result.IsSuccess && result.Data != null)
                                    {
                                        itemGroupCode = result.Data.Select(x => x.ItemgroupCode).FirstOrDefault();
                                    }
                                }
                                var itemGroupBaseQty = await commonGetBaseQtyWithItemGroup(itemGroupCode, giftLine.packing, giftLine.numberOfGift * multiplyRw);

                                //call api transaction
                                var standardSkus = _clientService.CommonRequest<List<StandardItemModel>>(CommonData.SystemUrlCode.SystemAdminAPI, $"Standard/GetInventoryItemStdByItemGroupByQuantity/{itemGroupCode}/{itemGroupBaseQty}", Method.GET, $"Rdos {UserToken.Split(" ").Last()}", null);

                                if (standardSkus != null && standardSkus.Count > 0)
                                {
                                    foreach (var standardItem in standardSkus)
                                    {
                                        //call api Detail Items
                                        var detailSKU = _clientService.CommonRequest<ItemMng_IventoryItemModel>(CommonData.SystemUrlCode.SystemAdminAPI, $"InventoryItem/GetInventoryItemById/{standardItem.InventoryId}", Method.GET, $"Rdos {UserToken.Split(" ").Last()}", null);

                                        VATDetail vatDetail = _clientService.CommonRequest<VATDetail>(CommonData.SystemUrlCode.SystemAdminAPI, $"Vat/GetVatById/{detailSKU.InventoryItem.Vat}", Method.GET, $"Rdos {UserToken.Split(" ").Last()}", null);

                                        //call api Detail Items
                                        var detailSelectedItem = _clientService.CommonRequest<ItemMng_InventoryItemAttribute>(CommonData.SystemUrlCode.SystemAdminAPI, $"InventoryItem/GetInventoryAttributeByCode/{standardItem.InventoryCode}", Method.GET, $"Rdos {UserToken.Split(" ").Last()}", null);

                                        ProgramCustomerDetailsItems pcDetailItem = new()
                                        {
                                            Id = _includeSaved ? Guid.NewGuid() : Guid.Empty,
                                            ProgramCustomersDetailCode = programCustomerDetail.ProgramCustomersDetailCode,
                                            InventoryId = detailSKU.InventoryItem.InventoryItemId,
                                            ItemCode = detailSKU.InventoryItem.InventoryItemId,
                                            ItemDescription = detailSKU.InventoryItem.Description,
                                            PromotionRefNumber = promotionRefNumber,
                                            Description = giftLine.productDescription,
                                            UOMCode = detailSelectedItem.BaseUOMCode,
                                            IsDisCountLine = true,
                                            DiscountLineCode = null,
                                            OrderQuantites = standardItem.Avaiable,
                                            UnitPrice = 0,
                                            Amount = 0,
                                            DiscountPercented = 0,
                                            DisCountAmount = 0,
                                            IsDeleted = false,
                                            IsPromotion = true,
                                            PromotionType = PROMO_PROMOTIONTYPECONST.Promotion,
                                            PromotionCode = promotionCode,
                                            ItemShortName = detailSKU.InventoryItem.ShortName,
                                            BaseUnit = detailSKU.InventoryItem.BaseUnit,
                                            SalesUnit = detailSKU.InventoryItem.SalesUnit,
                                            PurchaseUnit = detailSKU.InventoryItem.PurchaseUnit,
                                            ItemId = detailSKU.InventoryItem.Id,
                                            BaseOrderQuantities = standardItem.Avaiable,
                                            VatId = detailSKU.InventoryItem.Vat,
                                            VATCode = vatDetail.VatId,
                                            VatValue = vatDetail.VatValues,
                                            ProgramDetailDesc = programCustomerDetail.DetailDescription,
                                        };
                                        programCustomerDetailsItems.Add(pcDetailItem);
                                    }
                                }
                            }

                        }
                    }
                }
                programCustomerDetailsItems = programCustomerDetailsItems.GroupBy(x => new
                {
                    x.ProgramCustomersDetailCode,
                    x.InventoryId,
                    x.ItemCode,
                    x.ItemDescription,
                    x.PromotionRefNumber,
                    x.Description,
                    x.UOMCode,
                    x.ItemShortName,
                    x.BaseUnit,
                    x.SalesUnit,
                    x.PurchaseUnit,
                    x.ItemId,
                    x.VatId,
                    x.VATCode,
                    x.VatValue,
                })
                .Select(x => new ProgramCustomerDetailsItems
                {
                    Id = _includeSaved ? Guid.NewGuid() : Guid.Empty,
                    ProgramCustomersDetailCode = programCustomerDetail.ProgramCustomersDetailCode,
                    InventoryId = x.Key.InventoryId,
                    ItemCode = x.Key.ItemCode,
                    ItemDescription = x.Key.ItemDescription,
                    PromotionRefNumber = x.Key.PromotionRefNumber,
                    Description = x.Key.Description,
                    UOMCode = x.Key.UOMCode,
                    IsDisCountLine = true,
                    DiscountLineCode = null,
                    OrderQuantites = x.Sum(x => x.OrderQuantites),
                    UnitPrice = 0,
                    Amount = 0,
                    DiscountPercented = 0,
                    DisCountAmount = 0,
                    IsDeleted = false,
                    IsPromotion = true,
                    PromotionType = PROMO_PROMOTIONTYPECONST.Promotion,
                    PromotionCode = promotionCode,
                    ItemShortName = x.Key.ItemShortName,
                    BaseUnit = x.Key.BaseUnit,
                    SalesUnit = x.Key.SalesUnit,
                    PurchaseUnit = x.Key.PurchaseUnit,
                    ItemId = x.Key.ItemId,
                    BaseOrderQuantities = x.Sum(x => x.BaseOrderQuantities),
                    VatId = x.Key.VatId,
                    VATCode = x.Key.VATCode,
                    VatValue = x.Key.VatValue,
                    ProgramDetailDesc = programCustomerDetail.DetailDescription,
                }).ToList();
                return programCustomerDetailsItems;
            }
            catch (System.Exception ex)
            {
                return null;
            }
        }

        #endregion

        public async Task<List<ItemMng_InventoryItem>> GetRewardItemChange(string promotionCode, string detailCode, List<string> excludedProduct, string token)
        {
            UserToken = token;
            try
            {
                List<ItemMng_InventoryItem> resultItems = new List<ItemMng_InventoryItem>();
                var promotionDetailResult = _clientService.CommonRequest<ResultModelWithObject<TpPromotionDetailModel>>(CommonData.SystemUrlCode.ODTpAPI, $"tppromotion/Getdetailpromotionexternalbycode/{promotionCode}", Method.GET, UserToken, null, true);
                if (promotionDetailResult.IsSuccess && promotionDetailResult.Data != null)
                {
                    var definitionStructure = promotionDetailResult.Data.listDefinitionStructure.Where(x => x.levelCode == detailCode).FirstOrDefault();
                    if (definitionStructure != null)
                    {
                        var gifts = definitionStructure.listProductForGifts;
                        if (gifts.Count > 0)
                        {
                            foreach (var gift in gifts)
                            {
                                if (definitionStructure.productTypeForGift == TP_GIFT_TYPE.SKU)
                                {
                                    //call api Detail Items
                                    var detailItem = _clientService.CommonRequest<ItemMng_IventoryItemModel>(CommonData.SystemUrlCode.ODItemAPI, $"InventoryItem/GetInventoryItemByCode/{gift.productCode}", Method.GET, $"Rdos {UserToken.Split(" ").Last()}", null);
                                    if (!excludedProduct.Contains(detailItem.InventoryItem.InventoryItemId))
                                        resultItems.Add(detailItem.InventoryItem);
                                }
                                else
                                {
                                    List<string> listItemGroupCode = new();
                                    if (gift.productType == TP_GIFT_TYPE.ITEMHIERARCHY)
                                    {
                                        var hierarchyLevel = definitionStructure.itemHierarchyLevelForGift;
                                        var request = new
                                        {
                                            IT01Code = hierarchyLevel == ItemSettingConst.Industry ? gift.productCode : null,
                                            IT02Code = hierarchyLevel == ItemSettingConst.Category ? gift.productCode : null,
                                            IT03Code = hierarchyLevel == ItemSettingConst.SubCategory ? gift.productCode : null,
                                            IT04Code = hierarchyLevel == ItemSettingConst.Brand ? gift.productCode : null,
                                            IT05Code = hierarchyLevel == ItemSettingConst.SubBrand ? gift.productCode : null,
                                            IT06Code = hierarchyLevel == ItemSettingConst.PackSize ? gift.productCode : null,
                                            IT07Code = hierarchyLevel == ItemSettingConst.PackType ? gift.productCode : null,
                                            IT08Code = hierarchyLevel == ItemSettingConst.Packaging ? gift.productCode : null,
                                            IT09Code = hierarchyLevel == ItemSettingConst.Weight ? gift.productCode : null,
                                            IT10Code = hierarchyLevel == ItemSettingConst.Volume ? gift.productCode : null
                                        };
                                        var result = _clientService.CommonRequest<ResultModelWithObject<List<ItemGroupFromHierarchyResult>>>(CommonData.SystemUrlCode.ODItemAPI, $"ItemGroup/GetItemGroupByHierarchy", Method.POST, UserToken, request);
                                        //Tìm Itemgroup
                                        if (result.IsSuccess && result.Data != null)
                                        {
                                            listItemGroupCode = result.Data.Select(x => x.ItemgroupCode).ToList();
                                        }

                                    }
                                    else
                                    {
                                        listItemGroupCode = new List<string> { gift.productCode };
                                    }

                                    foreach (var itemGroupCode in listItemGroupCode)
                                    {
                                        var itemGroupDetail = _clientService.CommonRequest<ItemGroupDetailModel>(CommonData.SystemUrlCode.ODItemAPI, $"ItemGroup/GetItemGroupWithUOMByCode/{itemGroupCode}", Method.GET, $"Rdos {UserToken.Split(" ").Last()}", null);
                                        if (itemGroupDetail != null && itemGroupDetail.ListInventory.Count > 0)
                                        {
                                            foreach (var invItem in itemGroupDetail.ListInventory)
                                            {
                                                if (invItem.Status.Trim().ToLower() != "active") continue;
                                                var detailItem = _clientService.CommonRequest<ItemMng_IventoryItemModel>(CommonData.SystemUrlCode.ODItemAPI, $"InventoryItem/GetInventoryItemByCode/{invItem.InventoryItemId}", Method.GET, $"Rdos {UserToken.Split(" ").Last()}", null);
                                                if (!excludedProduct.Contains(detailItem.InventoryItem.InventoryItemId) && detailItem.InventoryItem.Status == "1" && detailItem.InventoryItem.OrderItem)
                                                    resultItems.Add(detailItem.InventoryItem);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return resultItems.Distinct().ToList();

            }
            catch (System.Exception)
            {
                return new List<ItemMng_InventoryItem>();
            }
        }


        public async Task<BaseResultModel> ImportDataFromFFA(SaleOrderModel model, string token)
        {
            UserToken = token;
            // var select = model.OrderItems.Select(x => new {
            //     x.ItemId,
            //     x.IsFree,
            //     x.PromotionCode,
            //     x.OrderQuantities,
            //     x.UOM,
            //     x.PromotionLevel,
            //     x.ItemGroupCode
            // });
            var prog_mapping = model.OrderItems.Where(x => x.PromotionType == PROMO_PROMOTIONTYPECONST.Promotion && x.PromotionCode != null).GroupBy(x => x.PromotionCode).Select(x => new PromotionCustomerModel
            {
                ProgramCode = x.Key,
                ProgramsCustomerDetails = x.GroupBy(y => y.PromotionLevel).Select(z => new PromotionCustomerDetailsModel
                {
                    DetailLevel = z.Key,
                    ItemsConvert = z.ToList()
                }).ToList(),
            }).ToList();

            //PromotionrefNumber
            var promotionRefNumber = model.PromotionRefNumber;

            foreach (var prog in prog_mapping)
            {
                //Get  Promotion Detail
                var promotionDetailResult = _clientService.CommonRequest<ResultModelWithObject<TpPromotionDetailModel>>(CommonData.SystemUrlCode.ODTpAPI, $"tppromotion/Getdetailpromotionexternalbycode/{prog.ProgramCode}", Method.GET, UserToken, null, true);
                if (!promotionDetailResult.IsSuccess || promotionDetailResult.Data == null)
                {
                    return new BaseResultModel
                    {
                        Code = 400,
                        Data = null,
                        Message = "False",
                        IsSuccess = false,
                    };
                }
                TpPromotionDetailModel promotion = promotionDetailResult.Data;

                var promoItemScope = promotion.promotionType == "01" ? PROMO_ITEMSCOPECONST.LINE : promotion.promotionType == "02" ? PROMO_ITEMSCOPECONST.GROUP : promotion.promotionType == "03" ? PROMO_ITEMSCOPECONST.BUNDLE : "Undefined";

                prog.Id = Guid.NewGuid();
                prog.ProgramCustomersKey = promotionRefNumber + "-" + promotion.code;
                prog.ProgramCode = promotion.code;
                prog.ProgramsType = PROMO_PROMOTIONTYPECONST.Promotion;
                prog.ProgramsDescription = promotion.fullName;
                prog.ProgramsItemScope = promoItemScope;
                prog.ShiptoCode = model.CustomerShiptoID;
                prog.CustomerCode = model.CustomerId;
                prog.IsDeleted = false;
                prog.EffectiveDate = promotion.effectiveDateFrom;
                prog.ValidUntil = promotion.validUntil;
                prog.Shipto_Attribute1 = model.Shipto_Attribute1;
                prog.Shipto_Attribute2 = model.Shipto_Attribute2;
                prog.Shipto_Attribute3 = model.Shipto_Attribute3;
                prog.Shipto_Attribute4 = model.Shipto_Attribute4;
                prog.Shipto_Attribute5 = model.Shipto_Attribute5;
                prog.Shipto_Attribute6 = model.Shipto_Attribute6;
                prog.Shipto_Attribute7 = model.Shipto_Attribute7;
                prog.Shipto_Attribute8 = model.Shipto_Attribute8;
                prog.Shipto_Attribute9 = model.Shipto_Attribute9;
                prog.Shipto_Attribute10 = model.Shipto_Attribute10;
                prog.SalesOrgCode = model.SalesOrgID;
                prog.SicCode = model.SIC_ID;
                prog.RouteZoneCode = model.RouteZoneID;
                prog.DsaCode = model.DSAID;
                prog.Branch = model.BranchId;
                prog.Region = model.RegionId;
                prog.SubRegion = model.SubRegionId;
                prog.Area = model.AreaId;
                prog.SubArea = model.SubAreaId;
                prog.PromotionRefNumber = promotionRefNumber;

                foreach (var detail in prog.ProgramsCustomerDetails)
                {
                    var definitionStructure = promotion.listDefinitionStructure.Where(x => x.levelCode == detail.DetailLevel).FirstOrDefault();
                    if (definitionStructure == null)
                    {
                        return new BaseResultModel
                        {
                            Code = 400,
                            Data = null,
                            Message = "False",
                            IsSuccess = false,
                        };
                    }

                    var buyType = definitionStructure.quantityPurchased > 0 ? PROMO_BYBREAKDOWNCONST.QUANTITY : PROMO_BYBREAKDOWNCONST.AMOUNT;
                    var givingType = definitionStructure.listProductForGifts.Count > 0 ? PROMO_GIVINGTYPECONST.FREEITEM : definitionStructure.amountOfDonation > 0 ? PROMO_GIVINGTYPECONST.AMOUNT : definitionStructure.percentageOfAmount > 0 ? PROMO_GIVINGTYPECONST.PERCENTED : null;
                    var detailType = definitionStructure.ruleOfGiving && (definitionStructure.onEachValue > 0 || definitionStructure.onEachQuantity > 0) ? PROMO_RULEOFGIVING.BOX : PROMO_RULEOFGIVING.PASSLEVEL;
                    var detailQty = detailType == PROMO_RULEOFGIVING.PASSLEVEL ? definitionStructure.quantityPurchased : definitionStructure.onEachQuantity;
                    var detailAmt = detailType == PROMO_RULEOFGIVING.PASSLEVEL ? definitionStructure.valuePurchased : definitionStructure.onEachValue;
                    detail.Id = Guid.NewGuid();

                    #region Flow Budget
                    var budgetCode = definitionStructure.budgetCodeForGift != null ? definitionStructure.budgetCodeForGift : definitionStructure.budgetCodeForDonate != null ? definitionStructure.budgetCodeForDonate : null;
                    var budgetType = definitionStructure.budgetTypeOfGift != null ? definitionStructure.budgetTypeOfGift : definitionStructure.budgetTypeOfDonate != null ? definitionStructure.budgetTypeOfDonate : null;
                    var budgetAllocationLevel = definitionStructure.budgetAllocationLevelOfGift != null ? definitionStructure.budgetAllocationLevelOfGift : definitionStructure.budgetAllocationLevelOfDonate != null ? definitionStructure.budgetAllocationLevelOfDonate : null;
                    #endregion

                    detail.ProgramCustomersDetailCode = prog.ProgramCustomersKey + "-" + definitionStructure.levelCode;
                    detail.ProgramCustomersKey = prog.ProgramCustomersKey;
                    detail.ProgramDetailsKey = definitionStructure.levelCode;
                    detail.PromotionRefNumber = promotionRefNumber;
                    detail.ActualQantities = 0;
                    detail.ActualAmount = 0;
                    detail.RemainAmount = 0;
                    detail.RemainQuantities = 0;
                    detail.SuggestQantities = 0;
                    detail.DetailLevel = definitionStructure.levelCode;
                    detail.DetailDescription = definitionStructure.levelName;
                    detail.DetailType = detailType;
                    detail.DetailQuantities = detailQty;
                    detail.DetailAmount = detailAmt;
                    detail.EffectiveDate = DateTime.Now;
                    detail.ValidUntil = null;
                    detail.IsDeleted = false;
                    detail.ProgramsBuyType = buyType;
                    detail.ProgramsGivingType = givingType;
                    detail.ProductTypeForSale = definitionStructure.productTypeForGift;
                    detail.ProductTypeForGift = definitionStructure.productTypeForSale;
                    detail.ItemHierarchyLevelForSale = definitionStructure.itemHierarchyLevelForSale;
                    detail.ItemHierarchyLevelForGift = definitionStructure.itemHierarchyLevelForGift;
                    detail.QuantityPurchased = definitionStructure.quantityPurchased;
                    detail.OnEachQuantity = definitionStructure.onEachQuantity;
                    detail.ValuePurchased = definitionStructure.valuePurchased;
                    detail.OnEachValue = definitionStructure.onEachValue;
                    detail.BudgetCode = budgetCode;
                    detail.BudgetType = budgetType;
                    detail.BudgetAllocationLevel = budgetAllocationLevel;
                    detail.BudgetBook = 0;
                    detail.BudgetBooked = 0;
                    detail.BudgetBookOver = false;
                    detail.Allowance = definitionStructure.Allowance;
                    detail.ProgramCustomerItemsGroup = new List<ProgramCustomerItemsGroup>();
                    detail.ProgramCustomerDetailsItems = new List<ProgramCustomerDetailsItems>();

                    List<ProgramCustomerDetailsItems> listItemDetail = new();
                    //Product for sales
                    foreach (var prodForSales in definitionStructure.listProductForSales)
                    {
                        var listItem = detail.ItemsConvert.Where(x => prodForSales.promotionCode == x.PromotionCode &&
                                x.PromotionLevel == prodForSales.levelCode &&
                                (prodForSales.productType.ToLower().Trim() == TP_SALE_TYPE.SKU.ToLower().Trim() ? prodForSales.productCode == x.ItemCode : prodForSales.productType.ToLower().Trim() == TP_SALE_TYPE.ITEMGROUP.ToLower().Trim() ? prodForSales.productCode == x.ItemGroupCode : false)).ToList();
                        var cusItemGroup = new ProgramCustomerItemsGroup
                        {
                            Id = Guid.NewGuid(),
                            ProgramCustomerItemsGroupCode = detail.ProgramCustomersDetailCode + "-" + prodForSales.productCode,
                            ProgramCustomersDetailCode = detail.ProgramCustomersDetailCode,
                            PromotionRefNumber = promotionRefNumber,
                            Quantities = 0,
                            ItemGroupQuantities = 0,
                            Amount = 0,
                            Description = prodForSales.productDescription,
                            ItemGroupCode = prodForSales.productCode,
                            UOMCode = prodForSales.packing,
                            FixedQuantities = prodForSales.sellNumber,
                            IsDeleted = false,
                            MinQty = 0,
                            MinAmt = 0,
                            ProductTypeForSale = prodForSales.productType,
                            InventoryItemCode = prodForSales.productCode,
                            ItemHierarchyValueForSale = prodForSales.productCode,
                        };

                        int quantities = 0;
                        decimal amt = 0;
                        float purchaseRate = 0;//suất mua

                        foreach (var item in listItem)
                        {
                            // //call api Detail Item
                            // var itemDetail = _clientService.CommonRequest<ItemMng_IventoryItemModel>(CommonData.SystemUrlCode.SystemAdminAPI, $"InventoryItem/GetInventoryItemByCode/{item.ItemCode}", Method.GET, $"Rdos {UserToken.Split(" ").Last()}", null);
                            //call api Detail Item
                            var detailItem = _clientService.CommonRequest<ItemMng_IventoryItemModel>(CommonData.SystemUrlCode.ODItemAPI, $"InventoryItem/GetInventoryItemByCode/{item.ItemCode}", Method.GET, $"Rdos {UserToken.Split(" ").Last()}", null);
                            if (!item.IsFree)
                            {
                                switch (item.AllocateType)
                                {
                                    case AllocateType.SKU:
                                        {

                                            if (buyType == PROMO_BYBREAKDOWNCONST.QUANTITY)
                                            {
                                                quantities += item.OrderQuantities;
                                                purchaseRate = quantities / detail.DetailQuantities;
                                            }
                                            else
                                            {
                                                amt += item.Ord_Line_Amt;
                                                purchaseRate += (int)(amt / detail.DetailAmount);
                                            }
                                            break;
                                        }
                                    case AllocateType.GROUP:
                                        {
                                            if (buyType == PROMO_BYBREAKDOWNCONST.QUANTITY)
                                            {
                                                quantities += await this.CommonGetQtyFromUnitToUnit(detailItem.UomConversion, item.UOM, prodForSales.packing, item.OrderQuantities);
                                                purchaseRate = (float)quantities / (float)detail.DetailQuantities;
                                            }
                                            else
                                            {
                                                amt += item.Ord_Line_Amt;
                                                purchaseRate = (float)amt / (float)detail.DetailAmount;
                                            }
                                            break;
                                        }
                                    default: break;
                                }
                            }

                            listItemDetail.Add(new ProgramCustomerDetailsItems
                            {
                                Id = Guid.NewGuid(),
                                ProgramCustomersDetailCode = detail.ProgramCustomersDetailCode,
                                ProgramCustomerItemsGroupCode = cusItemGroup.ProgramCustomerItemsGroupCode,
                                PromotionRefNumber = promotionRefNumber,
                                Description = prodForSales.packingDescription,
                                InventoryId = item.ItemCode,
                                ItemCode = item.ItemCode,
                                ItemId = item.ItemId,
                                ItemDescription = item.ItemDescription,
                                UOMCode = item.UOM,
                                IsDisCountLine = item.IsFree,
                                DiscountLineCode = null,
                                OrderQuantites = item.OrderQuantities,
                                UnitPrice = item.UnitPrice,
                                Amount = item.Ord_Line_Amt,
                                DiscountPercented = item.DiscountPercented,
                                DisCountAmount = item.DisCountAmount,
                                IsDeleted = false,
                                IsPromotion = true,
                                PromotionType = item.PromotionType,
                                VatId = item.VatId,
                                VatValue = item.VatValue,
                                VATCode = item.VATCode,
                                BaseOrderQuantities = item.OrderBaseQuantities,
                                PromotionCode = item.PromotionCode,
                                ItemShortName = item.ItemShortName,
                                BaseUnit = item.BaseUnit,
                                SalesUnit = item.SalesUnit,
                                PurchaseUnit = item.PurchaseUnit,
                                OriginalQty = 0, //cusItemGroup.ItemGroupQuantities
                                OriginalAmt = 0, //usItemGroup.Amount
                                ProgramDetailDesc = item.PromotionDescription,
                            });
                        }

                        cusItemGroup.Quantities = (int)purchaseRate;
                        cusItemGroup.ItemGroupQuantities = quantities;
                        cusItemGroup.Amount = amt;
                        detail.ActualQantities = cusItemGroup.FixedQuantities > 0 ? cusItemGroup.Quantities / cusItemGroup.FixedQuantities : 0;

                        foreach (var itemDetaill in listItemDetail)
                        {
                            itemDetaill.OriginalQty = cusItemGroup.ItemGroupQuantities;
                            itemDetaill.OriginalAmt = cusItemGroup.Amount;
                        }

                        detail.ProgramCustomerItemsGroup.Add(cusItemGroup);
                    }


                    //Product for sales
                    foreach (var prodForGift in definitionStructure.listProductForGifts)
                    {
                        var listItem = detail.ItemsConvert.Where(x => x.IsFree && prodForGift.promotionCode == x.PromotionCode &&
                                x.PromotionLevel == prodForGift.levelCode &&
                                (prodForGift.productType.ToLower().Trim() == TP_SALE_TYPE.SKU.ToLower().Trim() ? prodForGift.productCode == x.ItemCode : prodForGift.productType.ToLower().Trim() == TP_SALE_TYPE.ITEMGROUP.ToLower().Trim() ? prodForGift.productCode == x.ItemGroupCode : false)).ToList();

                        foreach (var item in listItem)
                        {
                            listItemDetail.Add(new ProgramCustomerDetailsItems
                            {
                                Id = Guid.NewGuid(),
                                ProgramCustomersDetailCode = detail.ProgramCustomersDetailCode,
                                ProgramCustomerItemsGroupCode = null,
                                PromotionRefNumber = promotionRefNumber,
                                Description = prodForGift.packingDescription,
                                InventoryId = item.ItemCode,
                                ItemCode = item.ItemCode,
                                ItemId = item.ItemId,
                                ItemDescription = item.ItemDescription,
                                UOMCode = item.UOM,
                                IsDisCountLine = item.IsFree,
                                DiscountLineCode = null,
                                OrderQuantites = item.OrderQuantities,
                                UnitPrice = item.UnitPrice,
                                Amount = item.Ord_Line_Amt,
                                DiscountPercented = item.DiscountPercented,
                                DisCountAmount = item.DisCountAmount,
                                IsDeleted = false,
                                IsPromotion = true,
                                PromotionType = item.PromotionType,
                                VatId = item.VatId,
                                VatValue = item.VatValue,
                                VATCode = item.VATCode,
                                BaseOrderQuantities = item.OrderBaseQuantities,
                                PromotionCode = item.PromotionCode,
                                ItemShortName = item.ItemShortName,
                                BaseUnit = item.BaseUnit,
                                SalesUnit = item.SalesUnit,
                                PurchaseUnit = item.PurchaseUnit,
                                OriginalQty = 0, //cusItemGroup.ItemGroupQuantities
                                OriginalAmt = 0, //usItemGroup.Amount
                                ProgramDetailDesc = item.PromotionDescription,
                            });
                        }

                    }
                    _customerProgramDetailsItemsRepo.AddRange(listItemDetail, _schemaName);
                    _customerProgramItemsGroupRepo.AddRange(detail.ProgramCustomerItemsGroup, _schemaName);
                }


                //Product reward


                _customerProgramDetailRepo.AddRange(prog.ProgramsCustomerDetails, _schemaName);
                _customerProgramRepo.Add(prog, _schemaName);
            }

            return new BaseResultModel
            {
                Code = 200,
                Data = null,
                Message = "OK",
                IsSuccess = true,
            };
        }

        public async Task<int> CommonGetQtyFromUnitToUnit(List<UomConversionModel> UomConversion, string fromUnit, string toUnit, int qty)
        {
            if (fromUnit == toUnit) return qty;
            var converFromBase = UomConversion.Where(x => x != null &&
                ((x.FromUnitName == fromUnit && x.ToUnitName == toUnit) ||
                (x.FromUnitName == toUnit && x.ToUnitName == fromUnit))
                ).FirstOrDefault();


            if (converFromBase == null)
            {
                return 0;
            }
            else
            {
                if (converFromBase.FromUnitName == toUnit)
                {
                    if (converFromBase.DM == 1)
                    {
                        var adjustedQty = (int)(qty / converFromBase.ConversionFactor);
                        // var adjustedBaseQty = adjustedQty * converFromBase.ConversionFactor;

                        return adjustedQty;
                    }
                    else if (converFromBase.DM == 2)
                    {
                        var adjustedQty = (int)(qty * converFromBase.ConversionFactor);

                        return adjustedQty;
                    }
                    else
                    {
                        return 0;
                    }

                }
                else if (converFromBase.FromUnitName == fromUnit)
                {
                    if (converFromBase.DM == 1)
                    {
                        var adjustedQty = (int)(qty * converFromBase.ConversionFactor);

                        return adjustedQty;
                    }
                    else if (converFromBase.DM == 2)
                    {
                        var adjustedQty = (int)(qty / converFromBase.ConversionFactor);
                        return 0;
                    }
                    else
                    {
                        return 0;
                    }
                }
                else
                {
                    return 0;
                }
            }
        }


        public async Task<int> CommonGetQtyFromUnitIdToUnitId(List<UomConversionModel> UomConversion, Guid fromUnit, Guid toUnit, int qty)
        {
            if (fromUnit == toUnit) return qty;
            var converFromBase = UomConversion.Where(x => x != null &&
                ((x.FromUnit == fromUnit && x.ToUnit == toUnit) ||
                (x.FromUnit == toUnit && x.ToUnit == fromUnit))
                ).FirstOrDefault();


            if (converFromBase == null)
            {
                return 0;
            }
            else
            {
                if (converFromBase.FromUnit == toUnit)
                {
                    if (converFromBase.DM == 1)
                    {
                        var adjustedQty = (int)(qty / converFromBase.ConversionFactor);
                        // var adjustedBaseQty = adjustedQty * converFromBase.ConversionFactor;

                        return adjustedQty;
                    }
                    else if (converFromBase.DM == 2)
                    {
                        var adjustedQty = (int)(qty * converFromBase.ConversionFactor);

                        return adjustedQty;
                    }
                    else
                    {
                        return 0;
                    }

                }
                else if (converFromBase.FromUnit == fromUnit)
                {
                    if (converFromBase.DM == 1)
                    {
                        var adjustedQty = (int)(qty * converFromBase.ConversionFactor);

                        return adjustedQty;
                    }
                    else if (converFromBase.DM == 2)
                    {
                        var adjustedQty = (int)(qty / converFromBase.ConversionFactor);
                        return 0;
                    }
                    else
                    {
                        return 0;
                    }
                }
                else
                {
                    return 0;
                }
            }
        }

        // public async Task<BudgetResModel> getDetailBudget(BudgetReqModel model)
        // {
        //     try
        //     {
        //         return null
        //     }
        //     catch (System.Exception ex)
        //     {
        //         return null;
        //     }


        // }

    }

}
