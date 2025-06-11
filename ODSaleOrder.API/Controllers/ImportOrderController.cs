using DynamicSchema.Helper.Models.Header;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RDOS.PurchaseOrderAPI.Services;
using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Models;
using ODSaleOrder.API.Services.SaleOrder.Interface;
using Sys.Common.Extensions;
using Sys.Common.JWT;
using Sys.Common.Models;
using Sys.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ODSaleOrder.API.Services.BaseLine;

namespace ODSaleOrder.API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [Authorize]
    [ApiVersion("1.0")]
    public class ImportOrderController : ControllerBase
    {
        private readonly IImportOrderService _service;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly string _token;
        static AsyncLocker<string> userLock = new AsyncLocker<string>();
        public ImportOrderController(
            IHttpContextAccessor contextAccessor, 
            IImportOrderService service)
        {
            _contextAccessor = contextAccessor;
            _service = service;
            _token = _contextAccessor.HttpContext.Request.Headers["Authorization"];
        }

        [HttpPost]
        [Route("GetList")]
        [MapToApiVersion("1.0")]
        [HeaderModel]
        public async Task<IActionResult> Search(SearchFfaOrderModel req)
        {
            return Ok(await _service.GetListOrderFfa(req));
        }

        [HttpPost]
        [Route("Detail/{orderRefNumber}")]
        [MapToApiVersion("1.0")]
        [Authorize]
        [HeaderModel]
        public async Task<IActionResult> GetDetailSO(string orderRefNumber)
        {
            return Ok(await _service.GetDetailFfaOrder(orderRefNumber));
        }

        [HttpPost]
        [Route("Import")]
        [MapToApiVersion("1.0")]
        [HeaderModel]
        public async Task<IActionResult> Import(SearchFfaOrderModel req)
        {
            using (await userLock.LockAsync(User.GetName()))
            {
                return Ok(await _service.ImportAllOrder(_token, User.GetName(), req));
            }
        }

        [HttpPost]
        [Route("ImportListFfaOrder")]
        [MapToApiVersion("1.0")]
        [HeaderModel]
        public async Task<IActionResult> ImportListFfaOrder(ImportListFfaOrder req)
        {
            using (await userLock.LockAsync(User.GetName()))
            {
                return Ok(await _service.ImportListOrder(req, _token, User.GetName()));
            }
        }

        [HttpPost]
        [Route("CancelListFfaOrder")]
        [MapToApiVersion("1.0")]
        [HeaderModel]
        public async Task<IActionResult> CancelListFfaOrder(List<CancelListFfaOrder> req)
        {
            using (await userLock.LockAsync(User.GetName()))
            {
                return Ok(await _service.CancelFFAOrdersV2(req, _token, User.GetName()));
            }
        }

        [HttpGet]
        [Route("GetBaselineDateCurrent")]
        [MapToApiVersion("1.0")]
        public async Task<IActionResult> GetBaselineDateCurrent()
        {
            return Ok(await _service.HandleCalculateBaselineDate());
        }
    }
}
