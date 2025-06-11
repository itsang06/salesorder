using DynamicSchema.Helper.Models.Header;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Models;
using ODSaleOrder.API.Services;
using ODSaleOrder.API.Services.SaleOrder.Interface;
using Sys.Common.Extensions;
using Sys.Common.JWT;
using Sys.Common.Models;
using Sys.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ODSaleOrder.API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [Authorize]
    [ApiVersion("1.0")]
    public class SOReturnController : ControllerBase
    {
        private readonly ISaleOrderReturnService _service;

        private readonly IHttpContextAccessor _contextAccessor;
        private readonly string _token;
        static AsyncLocker<string> AsyncLocker = new AsyncLocker<string>();

        public SOReturnController(IHttpContextAccessor contextAccessor, ISaleOrderReturnService service)
        {
            _contextAccessor = contextAccessor;
            _service = service;
            _token = _contextAccessor.HttpContext.Request.Headers["Authorization"];
        }

        [HttpPost]
        [Route("Create")]
        [MapToApiVersion("1.0")]
        [HeaderModel]
        public async Task<IActionResult> InsertOrder(SaleOrderModel model)
        {
            using (await AsyncLocker.LockAsync(model.CustomerId + model.CustomerShiptoID))
            {
                return Ok(await _service.InsertOrder(model, _token, User.GetName()));
            }
        }
        [HttpPost]
        [Route("CreateV2")]
        [MapToApiVersion("1.0")]
        [HeaderModel]
        public async Task<IActionResult> InsertOrderV2(SaleOrderModel model)
        {
            using (await AsyncLocker.LockAsync(model.CustomerId + model.CustomerShiptoID))
            {
                return Ok(await _service.InsertOrder(model, _token, User.GetName(), true));
            }
        }

        [HttpPost]
        [Route("SaveWithConfirm")]
        [MapToApiVersion("1.0")]
        [HeaderModel]
        public async Task<IActionResult> SaveWithConfirm(SaleOrderModel model)
        {
            if (!string.IsNullOrWhiteSpace(model.OrderRefNumber))
            {
                using (await AsyncLocker.LockAsync(model.OrderRefNumber))
                {
                    return Ok(await _service.SaveWithConfirm(model, _token, User.GetName()));
                }
            }
            else
            {
                return Ok(await _service.SaveWithConfirm(model, _token, User.GetName()));
            }
        }

        [HttpPut]
        [Route("Update")]
        [MapToApiVersion("1.0")]
        [HeaderModel]
        public async Task<IActionResult> UpdateOrder(SaleOrderModel model)
        {
            return Ok(await _service.UpdateSOReturn(model, _token, User.GetName()));
        }

        [HttpPut]
        [Route("Confirm")]
        [MapToApiVersion("1.0")]
        [HeaderModel]
        public async Task<IActionResult> Confirm(SaleOrderModel model)
        {
            using (await AsyncLocker.LockAsync(model.OrderRefNumber))
            {
                return Ok(await _service.Confirm(model, _token, User.GetName()));
            }
        }

        [HttpPost]
        [Route("Detail")]
        [MapToApiVersion("1.0")]
        [HeaderModel]
        public async Task<IActionResult> GetDetailSO(SaleOrderDetailQueryModel query)
        {
            return Ok(await _service.GetDetailSOReturn(query));
        }

        [HttpPost]
        [Route("Search")]
        [MapToApiVersion("1.0")]
        [HeaderModel]
        public async Task<IActionResult> SearchSO(SaleOrderSearchParamsModel parameters)
        {
            return Ok(await _service.SearchSOReturn(parameters));
        }

        [HttpDelete]
        [Route("Delete")]
        [MapToApiVersion("1.0")]
        [HeaderModel]
        public async Task<IActionResult> DeleteSO(SaleOrderDetailQueryModel query)
        {
            return Ok(await _service.DeleteSOReturn(query, User.GetName()));
        }

    }
}
