using System;
using System.Threading.Tasks;
using SysAdmin.Models.SystemUrl;
using AutoMapper;
using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Services.Base;
using static SysAdmin.Models.StaticValue.CommonData;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Flurl;
using SysAdmin.API.Constants;
using SyncToStaging.Helper.Services;

namespace SysAdmin.Web.Services.SystemUrl
{
    public class SystemUrlService : ISystemUrlService
    {
        private readonly IBaseRepository<Service> _servicesRepository;
        private readonly IMapper _mapper;
        private readonly string _principleCode;

        public SystemUrlService(IBaseRepository<Service> servicesRepository, IMapper mapper)
        {
            _servicesRepository = servicesRepository;
            _mapper = mapper;
            _principleCode = Environment.GetEnvironmentVariable("PRINCIPALCODE");
        }
        public async Task<SystemUrlListModel> GetAllSystemUrl()
        {
            try
            {
                List<string> listUrlCodeUsed = new List<string>
                {
                    SystemUrlCode.KpiCode,
                    SystemUrlCode.SystemAdminAPI,
                    SystemUrlCode.RDOSSystem,
                    SystemUrlCode.SalesOrgAPI,
                    SystemUrlCode.ODTpAPI,
                    SystemUrlCode.ODInventoryAPI,
                    SystemUrlCode.PurchaseOrderCode,
                    SystemUrlCode.RouteMngAPI,
                    SystemUrlCode.SaleOrderAPI,
                    SystemUrlCode.PriceMngCode,
                    SystemUrlCode.BaselineAPI,
                    SystemUrlCode.NotiMobileRdosAPI,
                    SystemUrlCode.ODItemAPI,
                    SystemUrlCode.ODCustomerAPI,
                    SystemUrlCode.ODPriceAPI,
                    SystemUrlCode.ODSaleOrderAPI,
                    SystemUrlCode.ODBaseLineAPI,
                    SystemUrlCode.ODDistributorAPI,
                    SystemUrlCode.ODNotificationAPI
                };

                List<Service> _serviceInDb = await _servicesRepository.GetAllQueryable()
                    .Where(x => listUrlCodeUsed.Contains(x.Code)).ToListAsync();

                SystemUrlListModel res = new();
                res.Items = _mapper.Map<List<SystemUrlModel>>(_serviceInDb);

                foreach (var _service in res.Items)
                {
                    if (_service.Code == SystemUrlCode.ODTpAPI ||
                        _service.Code == SystemUrlCode.ODInventoryAPI ||
                        _service.Code == SystemUrlCode.ODItemAPI ||
                        _service.Code == SystemUrlCode.ODCustomerAPI ||
                        _service.Code == SystemUrlCode.ODPriceAPI ||
                        _service.Code == SystemUrlCode.ODBaseLineAPI ||
                        _service.Code == SystemUrlCode.ODDistributorAPI ||
                        _service.Code == SystemUrlCode.NotiMobileRdosAPI ||
                        _service.Code == SystemUrlCode.ODNotificationAPI
                        )
                    {
                        _service.Url = UrlHelperService.InternalBaseUrlDebug(_service.Url, _principleCode);
                    }
                }

                return res;
            }
            catch (System.Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
