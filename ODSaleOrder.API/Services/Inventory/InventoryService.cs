using DynamicSchema.Helper.Services;
using DynamicSchema.Helper.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Models;
using ODSaleOrder.API.Services.SaleOrder;
using RDOS.INVAPI.Infratructure;
using Sys.Common.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static SysAdmin.API.Constants.Constant;

namespace ODSaleOrder.API.Services.Inventory
{
    public class InventoryService : IInventoryService
    {
        private readonly IDynamicBaseRepository<INV_AllocationDetail> _allocationDetailRepo;
        private readonly IDynamicBaseRepository<InvAllocationTracking> _alocationtrackinglogRepo;
        private readonly IDynamicBaseRepository<INV_InventoryTransaction> _inventoryTransactionRepo;
        private readonly IDynamicBaseRepository<PrincipalWarehouseLocation> _principalWarehouseLocationRepo;
        private readonly ILogger<InventoryService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private string _schemaName = OD_Constant.DEFAULT_SCHEMA;
        private string _distributorCode = null;
        public InventoryService(RDOSContext dbcontext, ILogger<InventoryService> logger, IHttpContextAccessor httpContextAccessor) 
        {
            _allocationDetailRepo = new DynamicBaseRepository<INV_AllocationDetail>(dbcontext);
            _alocationtrackinglogRepo = new DynamicBaseRepository<InvAllocationTracking>(dbcontext);
            _inventoryTransactionRepo = new DynamicBaseRepository<INV_InventoryTransaction>(dbcontext);
            _principalWarehouseLocationRepo = new DynamicBaseRepository<PrincipalWarehouseLocation>(dbcontext);
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _schemaName = _httpContextAccessor.HttpContext?.Items["SchemaName"] as string ?? _schemaName;
            _distributorCode = _httpContextAccessor.HttpContext?.Items["DistributorCode"] as string ?? _distributorCode;
        }

        public async Task<ResultModelWithObject<INV_AllocationDetail>> GetAllocationDetailCurrent(QueryAllocationModel req)
        {
            try
            {
                var allocatioonDetail = await _allocationDetailRepo
                    .GetAllQueryable(null, null, null, _schemaName)
                        .FirstOrDefaultAsync(x => x.ItemCode == req.ItemCode &&
                        x.LocationCode == req.LocationCode &&
                        x.DistributorCode == req.DistributorCode &&
                        x.WareHouseCode == req.WarehouseCode);

                if (allocatioonDetail == null)
                {
                    return new ResultModelWithObject<INV_AllocationDetail>
                    {
                        IsSuccess = false,
                        Code = 404,
                        Message = $"Cannot found allocation detail with DistributorCode: {req.DistributorCode}, WarehouseCode: {req.WarehouseCode}, LocationCode: {req.LocationCode}, ItemCode: {req.ItemCode}",
                    };
                }

                return new ResultModelWithObject<INV_AllocationDetail>
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Successfully",
                    Data = allocatioonDetail
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new ResultModelWithObject<INV_AllocationDetail>
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
            
        }

        public async Task<ResultModelWithObject<INV_AllocationDetail>> GetListAllocationDetailCurrent(QueryAllocationModel req)
        {
            try
            {
                var allocatioonDetail = await _allocationDetailRepo
                    .GetAllQueryable(null, null, null, _schemaName)
                        .FirstOrDefaultAsync(x => x.ItemCode == req.ItemCode &&
                        x.DistributorCode == req.DistributorCode &&
                        x.WareHouseCode == req.WarehouseCode);

                if (allocatioonDetail == null)
                {
                    return new ResultModelWithObject<INV_AllocationDetail>
                    {
                        IsSuccess = false,
                        Code = 404,
                        Message = $"Cannot found allocation detail with DistributorCode: {req.DistributorCode}, WarehouseCode: {req.WarehouseCode}, LocationCode: {req.LocationCode}, ItemCode: {req.ItemCode}",
                    };
                }

                return new ResultModelWithObject<INV_AllocationDetail>
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Successfully",
                    Data = allocatioonDetail
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new ResultModelWithObject<INV_AllocationDetail>
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }

        }

        public async Task<BaseResultModel> UpdateBooked(INV_AllocationDetail allocatioonDetail, BookAllocationModel req, List<INV_InventoryTransaction> listInvTransaction)
        {
            try
            {
                if (req.BookBaseQty > allocatioonDetail.Available)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Code = 400,
                        Message = $"SOBooked quantity of item code: {allocatioonDetail.ItemCode} cannot be greater than available: {allocatioonDetail.Available}"
                    };
                }

                UpdateBookedAllocationModel reqBook = new UpdateBookedAllocationModel();
                reqBook.ItemCode = allocatioonDetail.ItemCode;
                reqBook.WareHouseCode = allocatioonDetail.WareHouseCode;
                reqBook.LocationCode = allocatioonDetail.LocationCode;
                reqBook.DistributorCode = allocatioonDetail.DistributorCode;
                reqBook.SOBooked = req.BookBaseQty;
                reqBook.OneShopId = req.OrderID;

                // Tracking
                var trackingLog = new InvAllocationTracking
                {
                    Id = Guid.NewGuid(),
                    ItemKey = allocatioonDetail.ItemKey,
                    ItemId = allocatioonDetail.ItemId,
                    ItemCode = allocatioonDetail.ItemCode,
                    BaseUom = allocatioonDetail.BaseUom,
                    ItemDescription = allocatioonDetail.ItemDescription,
                    WareHouseCode = allocatioonDetail.WareHouseCode,
                    LocationCode = allocatioonDetail.LocationCode,
                    DistributorCode = allocatioonDetail.DistributorCode,
                    OnHandBeforChanged = allocatioonDetail.OnHand,
                    OnHandToChanged = 0,
                    OnHandChanged = allocatioonDetail.OnHand,
                    OnSoShippingBeforChanged = 0,
                    OnSoShippingToChanged = 0,
                    OnSoShippingChanged = 0,
                    OnSoBookedBeforChanged = allocatioonDetail.OnSoBooked,
                    OnSoBookedToChanged = req.BookBaseQty,
                    OnSoBookedChanged = allocatioonDetail.OnSoBooked + req.BookBaseQty,
                    AvailableBeforChanged = allocatioonDetail.Available,
                    AvailableToChanged = -req.BookBaseQty,
                    AvailableChanged = allocatioonDetail.Available - req.BookBaseQty,
                    ItemGroupCode = allocatioonDetail.ItemGroupCode,
                    DSACode = allocatioonDetail.DSACode,
                    FromFeature = "SOImport",
                    RequestDate = DateTime.Now,
                    RequestId = req.OrderID,
                    IsSuccess = true,
                    RequestBody = Newtonsoft.Json.JsonConvert.SerializeObject(reqBook),
                    OwnerCode = _distributorCode,
                    OwnerType = OwnerTypeConstant.DISTRIBUTOR
                };

                allocatioonDetail.OnSoBooked += req.BookBaseQty;
                allocatioonDetail.Available -= req.BookBaseQty;
                allocatioonDetail.UpdatedDate = DateTime.Now;
                allocatioonDetail.UpdatedBy = req.CreatedBy;
                _alocationtrackinglogRepo.Add(trackingLog, _schemaName);

                if (allocatioonDetail.OnSoBooked < 0)
                {
                    //Detach all tracking entites
                    _alocationtrackinglogRepo.DetachEntity(_schemaName);
                    trackingLog.IsSuccess = false;
                    //Save only failed trackingLog
                    _alocationtrackinglogRepo.Add(trackingLog, _schemaName);
                    _alocationtrackinglogRepo.Save(_schemaName);
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Code = 400,
                        Message = $"SOBooked Booked quantity result of item code: {allocatioonDetail.ItemCode} cannot be less than 0. Booking quantities: {req.BookBaseQty}, Result: {allocatioonDetail.OnSoBooked}"
                    };
                }

                // Save transaction
                INV_InventoryTransaction transactionInv = new();
                transactionInv.Id = Guid.NewGuid();
                transactionInv.ItemId = allocatioonDetail.ItemId;
                transactionInv.ItemCode = allocatioonDetail.ItemCode;
                transactionInv.ItemDescription = allocatioonDetail.ItemDescription;
                transactionInv.Uom = req.BookUom;
                transactionInv.Quantity = req.BookQty;
                transactionInv.BaseQuantity = req.BookBaseQty;
                transactionInv.OrderBaseQuantity = req.BookBaseQty;
                transactionInv.TransactionDate = DateTime.Now;
                transactionInv.TransactionType = INV_TransactionType.SO_CONFIRM;
                transactionInv.WareHouseCode = allocatioonDetail.WareHouseCode;
                transactionInv.LocationCode = allocatioonDetail.LocationCode;
                transactionInv.DistributorCode = allocatioonDetail.DistributorCode;
                transactionInv.OrderCode = null;
                transactionInv.ItemKey = allocatioonDetail.ItemKey;
                transactionInv.BegQty = allocatioonDetail.OnHand;
                transactionInv.EndQty = transactionInv.BegQty;
                transactionInv.IsDeleted = false;
                transactionInv.CreatedBy = req.CreatedBy;
                transactionInv.CreatedDate = DateTime.Now;
                transactionInv.FFAVisitId = req.FFAVisitID;
                transactionInv.OneShopId = req.OneShopID;
                transactionInv.ItemGroupCode = req.ItemGroupCode;
                transactionInv.Priority = req.Priority;
                transactionInv.IsCreateOrderItem = true;
                transactionInv.IsCreateInFlow = true;
                transactionInv.OwnerCode = _distributorCode;
                transactionInv.OwnerType = OwnerTypeConstant.DISTRIBUTOR;
                _inventoryTransactionRepo.Add(transactionInv, _schemaName);
                listInvTransaction.Add(transactionInv);

                // Save allocation current
                _allocationDetailRepo.UpdateUnSaved(allocatioonDetail, _schemaName);
                return new BaseResultModel
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Successfully",
                };
            }
            catch (Exception ex)
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

        public async Task<BaseResultModel> CancelBooked(INV_AllocationDetail allocatioonDetail, BookAllocationModel req, List<INV_InventoryTransaction> listInvTransaction)
        {
            try
            {
                //if (req.BookBaseQty > allocatioonDetail.OnSoBooked)
                //{
                //    return new BaseResultModel
                //    {
                //        IsSuccess = false,
                //        Code = 400,
                //        Message = $"Base quantity of item code: {allocatioonDetail.ItemCode} cannot be greater than OnSoBooked: {allocatioonDetail.OnSoBooked}"
                //    };
                //}

                UpdateBookedAllocationModel reqBook = new UpdateBookedAllocationModel();
                reqBook.ItemCode = allocatioonDetail.ItemCode;
                reqBook.WareHouseCode = allocatioonDetail.WareHouseCode;
                reqBook.LocationCode = allocatioonDetail.LocationCode;
                reqBook.DistributorCode = allocatioonDetail.DistributorCode;
                reqBook.SOBooked = req.BookBaseQty;
                reqBook.FFAOrderId = req.OrderID;

                // Tracking
                var trackingLog = new InvAllocationTracking
                {
                    Id = Guid.NewGuid(),
                    ItemKey = allocatioonDetail.ItemKey,
                    ItemId = allocatioonDetail.ItemId,
                    ItemCode = allocatioonDetail.ItemCode,
                    BaseUom = allocatioonDetail.BaseUom,
                    ItemDescription = allocatioonDetail.ItemDescription,
                    WareHouseCode = allocatioonDetail.WareHouseCode,
                    LocationCode = allocatioonDetail.LocationCode,
                    DistributorCode = allocatioonDetail.DistributorCode,
                    OnHandBeforChanged = allocatioonDetail.OnHand,
                    OnHandToChanged = 0,
                    OnHandChanged = allocatioonDetail.OnHand,
                    OnSoShippingBeforChanged = 0,
                    OnSoShippingToChanged = 0,
                    OnSoShippingChanged = 0,
                    OnSoBookedBeforChanged = allocatioonDetail.OnSoBooked,
                    OnSoBookedToChanged = -req.BookBaseQty,
                    OnSoBookedChanged = allocatioonDetail.OnSoBooked - req.BookBaseQty,
                    AvailableBeforChanged = allocatioonDetail.Available,
                    AvailableToChanged = req.BookBaseQty,
                    AvailableChanged = allocatioonDetail.Available + req.BookBaseQty,
                    ItemGroupCode = allocatioonDetail.ItemGroupCode,
                    DSACode = allocatioonDetail.DSACode,
                    FromFeature = "OSImportCancel",
                    RequestDate = DateTime.Now,
                    RequestId = req.OrderID,
                    IsSuccess = true,
                    RequestBody = Newtonsoft.Json.JsonConvert.SerializeObject(reqBook)
                };

                allocatioonDetail.OnSoBooked -= req.BookBaseQty;
                allocatioonDetail.Available += req.BookBaseQty;
                allocatioonDetail.UpdatedDate = DateTime.Now;
                allocatioonDetail.UpdatedBy = req.CreatedBy;
                _alocationtrackinglogRepo.Add(trackingLog, _schemaName);

                // Save transaction
                INV_InventoryTransaction transactionInv = new();
                transactionInv.Id = Guid.NewGuid();
                transactionInv.ItemId = allocatioonDetail.ItemId;
                transactionInv.ItemCode = allocatioonDetail.ItemCode;
                transactionInv.ItemDescription = allocatioonDetail.ItemDescription;
                transactionInv.Uom = req.BookUom;
                transactionInv.Quantity = req.BookQty;
                transactionInv.BaseQuantity = req.BookBaseQty;
                transactionInv.OrderBaseQuantity = req.BookBaseQty;
                transactionInv.TransactionDate = DateTime.Now;
                transactionInv.TransactionType = INV_TransactionType.SO_BOOKED_CANCEL;
                transactionInv.WareHouseCode = allocatioonDetail.WareHouseCode;
                transactionInv.LocationCode = allocatioonDetail.LocationCode;
                transactionInv.DistributorCode = allocatioonDetail.DistributorCode;
                transactionInv.OrderCode = null;
                transactionInv.ItemKey = allocatioonDetail.ItemKey;
                transactionInv.BegQty = allocatioonDetail.OnHand;
                transactionInv.EndQty = transactionInv.BegQty;
                transactionInv.IsDeleted = false;
                transactionInv.CreatedBy = req.CreatedBy;
                transactionInv.CreatedDate = DateTime.Now;
                transactionInv.FFAVisitId = req.FFAVisitID;
                transactionInv.OneShopId = req.OneShopID;
                transactionInv.ItemGroupCode = req.ItemGroupCode;
                transactionInv.Priority = req.Priority;
                transactionInv.IsCreateOrderItem = true;
                transactionInv.IsCreateInFlow = true;
                transactionInv.OwnerType = OwnerTypeConstant.DISTRIBUTOR;
                transactionInv.OwnerCode = _distributorCode;
                transactionInv.Source = SO_SOURCE_CONST.ONESHOP;
                _inventoryTransactionRepo.Add(transactionInv, _schemaName);
                listInvTransaction.Add(transactionInv);

                // Save allocation current
                _allocationDetailRepo.UpdateUnSaved(allocatioonDetail, _schemaName);
                return new BaseResultModel
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Successfully",
                };
            }
            catch (Exception ex)
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

        public async Task<List<INV_InventoryTransaction>> GetTransactionsByOneShopID(string oneShopID)
        {
            try
            {
                return await _inventoryTransactionRepo
                    .GetAllQueryable(x => x.OneShopId == oneShopID, null, null, _schemaName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message);
                return new List<INV_InventoryTransaction>();
            }
        }

        public async Task<List<INV_InventoryTransaction>> GetTransactionsByFfaVisitId(string ffaVisitId, string orderType)
        {
            try
            {
                return await _inventoryTransactionRepo
                    .GetAllQueryable(x => x.FFAVisitId == ffaVisitId && x.OrderType == orderType, null, null, _schemaName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message);
                return new List<INV_InventoryTransaction>();
            }
        }

        public async Task<ResultModelWithObject<List<PrincipalWarehouseLocation>>> GetListPrincipalWarehouseLocation()
        {
            try
            {
                List<PrincipalWarehouseLocation> listPrincipalWarehouseLocation = await _principalWarehouseLocationRepo
                    .GetAllQueryable(x => x.DeletedDate == null
                        && x.EffectiveFrom <= DateTime.Now &&
                        (!x.ValidUntil.HasValue || x.ValidUntil.Value >= DateTime.Now) &&
                        x.AllowOut)
                    .ToListAsync();

                return new ResultModelWithObject<List<PrincipalWarehouseLocation>>
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Successfully",
                    Data = listPrincipalWarehouseLocation
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message);
                return new ResultModelWithObject<List<PrincipalWarehouseLocation>>
                {
                    Code = 500,
                    IsSuccess = false,
                    Message = ex.InnerException?.Message ?? ex.Message
                };
            }
        }

        public async Task<BaseResultModel> CancelBookedFFAOrder(INV_InventoryTransaction input, string username)
        {
            try
            {
                // Get allocation detail current
                QueryAllocationModel reqGetRealtimeAllocation = new();
                reqGetRealtimeAllocation.DistributorCode = input.DistributorCode;
                reqGetRealtimeAllocation.WarehouseCode = input.WareHouseCode;
                reqGetRealtimeAllocation.LocationCode = input.LocationCode;
                reqGetRealtimeAllocation.ItemCode = input.ItemCode;

                ResultModelWithObject<INV_AllocationDetail> resAllocationDetailCurrent = await GetAllocationDetailCurrent(reqGetRealtimeAllocation);
                if (!resAllocationDetailCurrent.IsSuccess)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Code = resAllocationDetailCurrent.Code,
                        Message = resAllocationDetailCurrent.Message
                    };
                }

                INV_AllocationDetail allocatioonDetail = resAllocationDetailCurrent.Data;

                UpdateBookedAllocationModel reqBook = new UpdateBookedAllocationModel();
                reqBook.ItemCode = input.ItemCode;
                reqBook.WareHouseCode = input.WareHouseCode;
                reqBook.LocationCode = input.LocationCode;
                reqBook.DistributorCode = input.DistributorCode;
                reqBook.SOBooked = input.BaseQuantity;
                reqBook.FFAOrderId = input.Id;

                // Tracking
                var trackingLog = new InvAllocationTracking
                {
                    Id = Guid.NewGuid(),
                    ItemKey = allocatioonDetail.ItemKey,
                    ItemId = allocatioonDetail.ItemId,
                    ItemCode = allocatioonDetail.ItemCode,
                    BaseUom = allocatioonDetail.BaseUom,
                    ItemDescription = allocatioonDetail.ItemDescription,
                    WareHouseCode = allocatioonDetail.WareHouseCode,
                    LocationCode = allocatioonDetail.LocationCode,
                    DistributorCode = allocatioonDetail.DistributorCode,
                    OnHandBeforChanged = allocatioonDetail.OnHand,
                    OnHandToChanged = 0,
                    OnHandChanged = allocatioonDetail.OnHand,
                    OnSoShippingBeforChanged = 0,
                    OnSoShippingToChanged = 0,
                    OnSoShippingChanged = 0,
                    OnSoBookedBeforChanged = allocatioonDetail.OnSoBooked,
                    OnSoBookedToChanged = -input.BaseQuantity,
                    OnSoBookedChanged = allocatioonDetail.OnSoBooked - input.BaseQuantity,
                    AvailableBeforChanged = allocatioonDetail.Available,
                    AvailableToChanged = input.BaseQuantity,
                    AvailableChanged = allocatioonDetail.Available + input.BaseQuantity,
                    ItemGroupCode = allocatioonDetail.ItemGroupCode,
                    DSACode = allocatioonDetail.DSACode,
                    FromFeature = "Baseline",
                    RequestDate = DateTime.Now,
                    RequestId = input.Id,
                    IsSuccess = true,
                    RequestBody = Newtonsoft.Json.JsonConvert.SerializeObject(reqBook)
                };

                allocatioonDetail.OnSoBooked -= input.BaseQuantity;
                allocatioonDetail.Available += input.BaseQuantity;
                allocatioonDetail.UpdatedDate = DateTime.Now;
                allocatioonDetail.UpdatedBy = username;
                _alocationtrackinglogRepo.Add(trackingLog, _schemaName);

                // Save transaction
                INV_InventoryTransaction transactionInv = new();
                transactionInv.Id = Guid.NewGuid();
                transactionInv.ItemId = allocatioonDetail.ItemId;
                transactionInv.ItemCode = allocatioonDetail.ItemCode;
                transactionInv.ItemDescription = allocatioonDetail.ItemDescription;
                transactionInv.Uom = input.Uom;
                transactionInv.Quantity = input.Quantity;
                transactionInv.BaseQuantity = input.BaseQuantity;
                transactionInv.OrderBaseQuantity = input.BaseQuantity;
                transactionInv.TransactionDate = DateTime.Now;
                transactionInv.TransactionType = INV_TransactionType.SO_BOOKED_CANCEL;
                transactionInv.WareHouseCode = allocatioonDetail.WareHouseCode;
                transactionInv.LocationCode = allocatioonDetail.LocationCode;
                transactionInv.DistributorCode = allocatioonDetail.DistributorCode;
                transactionInv.OrderCode = null;
                transactionInv.ItemKey = allocatioonDetail.ItemKey;
                transactionInv.BegQty = allocatioonDetail.OnHand;
                transactionInv.EndQty = transactionInv.BegQty;
                transactionInv.IsDeleted = false;
                transactionInv.CreatedBy = username;
                transactionInv.CreatedDate = DateTime.Now;
                transactionInv.FFAVisitId = input.FFAVisitId;
                transactionInv.OneShopId = input.OneShopId;
                transactionInv.ItemGroupCode = input.ItemGroupCode;
                transactionInv.Priority = input.Priority;
                transactionInv.IsCreateOrderItem = true;
                transactionInv.IsCreateInFlow = true;
                transactionInv.OwnerType = OwnerTypeConstant.DISTRIBUTOR;
                transactionInv.OwnerCode = _distributorCode;
                transactionInv.Source = SO_SOURCE_CONST.MOBILE;
                transactionInv.OrderLineId = input.OrderLineId;
                transactionInv.OrderType = input.OrderType;
                _inventoryTransactionRepo.Add(transactionInv, _schemaName);

                // Save allocation current
                _allocationDetailRepo.UpdateUnSaved(allocatioonDetail, _schemaName);
                return new BaseResultModel
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Successfully",
                };
            }
            catch (Exception ex)
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
    }
}
