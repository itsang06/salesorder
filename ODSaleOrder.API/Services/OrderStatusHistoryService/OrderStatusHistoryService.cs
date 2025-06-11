using DynamicSchema.Helper.Services;
using DynamicSchema.Helper.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ODSaleOrder.API.Infrastructure;
using Sys.Common.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static SysAdmin.API.Constants.Constant;

namespace ODSaleOrder.API.Services.OrderStatusHistoryService
{
    public class OrderStatusHistoryService : IOrderStatusHistoryService
    {
        private readonly IDynamicBaseRepository<OsorderStatusHistory> _osOrderStatusHisRepo;
        private readonly IDynamicBaseRepository<SystemSetting> _systemSettingRepo;
        private readonly IDynamicBaseRepository<ODMappingOrderStatus> _odMappingOrderStatusRepo;
        private readonly IDynamicBaseRepository<OsOrderInformation> _osOrderRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private string _schemaName = OD_Constant.DEFAULT_SCHEMA;
        public OrderStatusHistoryService(RDOSContext context, IHttpContextAccessor httpContextAccessor) 
        {
            _osOrderStatusHisRepo = new DynamicBaseRepository<OsorderStatusHistory>(context);
            _systemSettingRepo = new DynamicBaseRepository<SystemSetting>(context);
            _odMappingOrderStatusRepo = new DynamicBaseRepository<ODMappingOrderStatus>(context);
            _osOrderRepository = new DynamicBaseRepository<OsOrderInformation>(context);
            _httpContextAccessor = httpContextAccessor;
            _schemaName = _httpContextAccessor.HttpContext?.Items["SchemaName"] as string ?? _schemaName;
        }
        public async Task<BaseResultModel> SaveStatusHistory(OsorderStatusHistory input, bool isSave = true, bool isCancelFromWeb = false)
        {
            try
            {
                input.Id = Guid.NewGuid();
                input.CreatedDate = DateTime.Now;

                if (string.IsNullOrEmpty(input.SOStatusName) && !string.IsNullOrEmpty(input.Sostatus))
                {
                    string _desSetting = await _systemSettingRepo
                        .GetAllQueryable(x => x.SettingKey == input.Sostatus)
                        .AsNoTracking()
                        .Select(x => x.Description)
                        .FirstOrDefaultAsync();

                    if (_desSetting != null)
                    {
                        input.SOStatusName = _desSetting;
                    }
                }

                if (string.IsNullOrEmpty(input.OneShopStatusName) && !string.IsNullOrEmpty(input.OneShopStatus))
                {
                    string _desSetting = await _systemSettingRepo
                        .GetAllQueryable(x => x.SettingKey == input.OneShopStatus)
                        .AsNoTracking()
                        .Select(x => x.Description)
                        .FirstOrDefaultAsync();

                    if (_desSetting != null)
                    {
                        input.OneShopStatusName = _desSetting;
                    }
                }

                _osOrderStatusHisRepo.Add(input, _schemaName);

                // Cập nhật status cho đơn hàng One shop
                if (!string.IsNullOrEmpty(input.ExternalOrdNbr) 
                    && !SO_SaleOrderStatusConst.NotUpdateStatuses.Contains(input.Sostatus)) 
                {
                    // Handle flow cancel
                    if ((input.Sostatus == SO_SaleOrderStatusConst.CANCEL 
                        && input.OneShopStatus == OSSOSTATUS.OSCancel) || isCancelFromWeb)
                    {
                        if (isSave)
                        {
                            _osOrderStatusHisRepo.Save(_schemaName);
                        }
                        return new BaseResultModel
                        {
                            Code = 201,
                            IsSuccess = true,
                            Message = "Successfully"
                        };
                    }

                    OsOrderInformation osOrderInDb = await _osOrderRepository
                        .GetAllQueryable(x => x.ExternalOrdNbr == input.ExternalOrdNbr, null, null, _schemaName)
                        .FirstOrDefaultAsync();
                        
                    if (osOrderInDb != null) {
                        osOrderInDb.SOStatus = input.Sostatus;
                        osOrderInDb.Status = input.OneShopStatus;
                        osOrderInDb.UpdatedDate = DateTime.Now;
                        _osOrderRepository.UpdateUnSaved(osOrderInDb, _schemaName);
                    }
                }

                if (isSave)
                {
                    _osOrderStatusHisRepo.Save(_schemaName);
                }
                return new BaseResultModel
                {
                    Code = 201,
                    IsSuccess = true,
                    Message = "Successfully"
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
        }
        //public async Task<string> HandleOSMappingStatus(string SoStatus)
        //{
        //    try
        //    {
        //        return await _odMappingOrderStatusRepo
        //            .GetAllQueryable(x => x.SaleOrderStatus == SoStatus)
        //            .Select(d => d.OneShopOrderStatus)
        //            .FirstOrDefaultAsync();
        //    }
        //    catch (System.Exception ex) 
        //    {
        //        return null;
        //    }
        //}

        public async Task<ODMappingOrderStatus> HandleOSMappingStatus(string SoStatus, bool isFromOs = false)
        {
            try
            {
                if (isFromOs && SoStatus == SO_SaleOrderStatusConst.CANCEL) {
                    return await _odMappingOrderStatusRepo
                        .GetAllQueryable(x => x.SaleOrderStatus == SoStatus && x.OneShopOrderStatus == OSSOSTATUS.OSCancel)
                        .FirstOrDefaultAsync();
                }
                else if (!isFromOs && SoStatus == SO_SaleOrderStatusConst.CANCEL) {
                    return await _odMappingOrderStatusRepo
                        .GetAllQueryable(x => x.SaleOrderStatus == SoStatus && x.OneShopOrderStatus == OSSOSTATUS.SOCancel)
                        .FirstOrDefaultAsync();
                }
                else {
                    return await _odMappingOrderStatusRepo
                        .GetAllQueryable(x => x.SaleOrderStatus == SoStatus)
                        .FirstOrDefaultAsync();
                }
            }
            catch (System.Exception ex)
            {
                return null;
            }
        }
    }
}
