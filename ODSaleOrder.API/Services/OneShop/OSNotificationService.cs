using DynamicSchema.Helper.Services;
using DynamicSchema.Helper.Services.Interface;
using FirebaseAdmin.Messaging;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Models;
using ODSaleOrder.API.Models.OS;
using ODSaleOrder.API.Services.OneShop.Interface;
using ODSaleOrder.API.Services.OrderStatusHistoryService;
using ODSaleOrder.API.Services.SaleOrder.Interface;
using RestSharp;
using Sys.Common.Models;
using SysAdmin.Models.Enum;
using SysAdmin.Models.StaticValue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core.Tokenizer;
using System.Threading.Tasks;
using static SysAdmin.API.Constants.Constant;

namespace ODSaleOrder.API.Services.OneShop
{
    public class OSNotificationService : IOSNotificationService
    {
        private readonly ILogger<OSNotificationService> _logger;
        private readonly IClientService _clientService;
        private readonly IDynamicBaseRepository<SystemSetting> _systemSettingRepo;
        private readonly IOrderStatusHistoryService _orderStatusHisService;
        public OSNotificationService(
            ILogger<OSNotificationService> logger, 
            IClientService clientService, 
            RDOSContext context,
            IOrderStatusHistoryService orderStatusHisService)
        {
            _logger = logger;
            _clientService = clientService;
            _systemSettingRepo = new DynamicBaseRepository<SystemSetting>(context);
            _orderStatusHisService = orderStatusHisService;
        }

        public async Task<BaseResultModel> SendNotification(OSNotificationModel req, string token, bool isFromOs=false)
        {
            _logger.LogInformation($"SendNotification Request: {JsonConvert.SerializeObject(req)}");
            Serilog.Log.Information($"############ SendNotification Request: {JsonConvert.SerializeObject(req)}");
            try
            {
                // Handle list outletcode
                List<string> listOutletCode = new();
                listOutletCode.Add(req.OutletCode);

                // Hanlde status description
                //string statusDes = await _systemSettingRepo
                //    .GetAllQueryable(x => x.SettingKey == req.OSStatus)
                //    .AsNoTracking()
                //    .Select(x => x.Description)
                //    .FirstOrDefaultAsync();

                var mappingStatus = await _orderStatusHisService.HandleOSMappingStatus(req.SOStatus, isFromOs);
                string statusDes = mappingStatus.OneShopOrderStatusName;

                // Handle template data
                string _templateData = null;
                List<string> templateDatas = new();
                templateDatas.Add($"external_ordnbr={req.External_OrdNBR}");                
                templateDatas.Add($"distributor_name={req.DistributorName}");
                if (!string.IsNullOrEmpty(statusDes))
                {
                    templateDatas.Add($"oso_status={statusDes}");
                }
                if (!string.IsNullOrEmpty(req.OrderRefNumber))
                {
                    templateDatas.Add($"order_number={req.OrderRefNumber}");
                }
                _templateData = String.Join(";", templateDatas);

                 // Gói data
                 var osNotiReq = new OSNotificationReqModel()
                {
                    title = $"{req.External_OrdNBR};{statusDes}",
                    body = $"{req.External_OrdNBR};{statusDes}",
                    notiImageFileName = null,
                    notiImagePathFile = null,
                    outletCodeList = listOutletCode,
                    notiType = OSNotificationType.NORMAL,
                    priority = OSNotificationPriority.WARNING,
                    navigateType = OSNotificationNavigateType.OS_ORDER,
                    navigatePath = req.External_OrdNBR,
                    purpose = req.Purpose,
                    templateData = _templateData,
                    ownerType = OwnerTypeConstant.DISTRIBUTOR,
                    ownerCode = req.DistributorCode
                };

                // Call api send noti của mobile
                OsNotiResponse result = await _clientService.CommonRequestAsync<OsNotiResponse>(
                CommonData.SystemUrlCode.ODNotificationAPI,
                    $"notification/expushnotificationoutlet",
                    Method.POST,
                    token,
                    osNotiReq
                   );

                if (result == null)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Message = "API Notification is not working",
                        Code = 502
                    };
                }
                _logger.LogInformation($"SendNotification Result: {JsonConvert.SerializeObject(result)}");
                Serilog.Log.Information($"SendNotification Result: {JsonConvert.SerializeObject(result)}");
                if (result.success)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = true,
                        Message = "Success",
                        Code = 200
                    };
                }
                else
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Message = result.messages != null ? result.messages.ToString() : null,
                        Code = 400
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"SendNotification Exception: {ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace}");
                Serilog.Log.Information($"SendNotification Exception: {ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace}");
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
