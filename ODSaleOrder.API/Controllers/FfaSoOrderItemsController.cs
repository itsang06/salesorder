using DynamicSchema.Helper.Models.Header;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Infrastructure.SOInfrastructure;
using ODSaleOrder.API.Models.SaleHistories;
using ODSaleOrder.API.Services.Ffa.Interface;
using Sys.Common.JWT;
using Sys.Common.Utils;
using System;
using System.Collections.Generic;

namespace ODSaleOrder.API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class FfaSoOrderItemsController : ControllerBase
    {
        private readonly IFfaSoOrderItemService _service;

        private readonly IHttpContextAccessor _contextAccessor;
        private readonly string _token;

        public FfaSoOrderItemsController(IHttpContextAccessor contextAccessor, IFfaSoOrderItemService service)
        {
            _contextAccessor = contextAccessor;
            _service = service;
            _token = _contextAccessor.HttpContext.Request.Headers["Authorization"];
        }


        [HttpPost]
        [Route("Create")]
        [MapToApiVersion("1.0")]
        public IActionResult CreateFfaSoOrderInformation(FfasoOrderItem model)
        {
            var result = _service.CreateFfaSoOrderItem(model, _token, User.GetName());
            return Ok(result);
        }

        [HttpPut]
        [Route("Update")]
        [MapToApiVersion("1.0")]
        public IActionResult UpdateFfaSoOrderInformation(FfasoOrderItem model)
        {
            var result = _service.UpdateFfaSoOrderItem(model, _token, User.GetName());
            return Ok(result);
        }


        [HttpDelete]
        [Route("Delete/{Id}")]
        [MapToApiVersion("1.0")]
        [HeaderModel]
        public IActionResult DeleteFfaSoOrderItem(Guid Id)
        {
            var result = _service.DeleteFfaSoOrderItem(Id, _token, User.GetName());
            return Ok(result);
        }

        [HttpGet]
        [Route("GetAll")]
        [MapToApiVersion("1.0")]
        public IActionResult GetAll()
        {
            var result = _service.GetAll();
            return Ok(result);
        }

        [HttpPost]
        [Route("InsertMany")]
        [MapToApiVersion("1.0")]
        [HeaderModel]
        public IActionResult InsertMany(List<FfasoOrderItem> model)
        {
            var result = _service.InsertOrUpdate(model);
            return Ok(result);
        }

        [HttpPut]
        [Route("UpdateMany")]
        [MapToApiVersion("1.0")]
        [HeaderModel]
        public IActionResult UpdateMany(List<FfasoOrderItem> model)
        {
            var result = _service.InsertOrUpdate(model);
            return Ok(result);
        }

        [HttpPut]
        [Route("DeleteMany")]
        [MapToApiVersion("1.0")]
        public IActionResult DeleteMany(List<string> model)
        {
            var result = _service.DeleteMany(model, _token, User.GetName());
            return Ok(result);
        }

        [HttpDelete]
        [Route("DeleteByExternal_OrdNBR/{External_OrdNBR}")]
        [MapToApiVersion("1.0")]
        public IActionResult DeleteByExternal_OrdNBR(string External_OrdNBR)
        {
            var result = _service.DeleteByExternal_OrdNBR(External_OrdNBR, _token, User.GetName());
            return Ok(result);
        }

        [HttpPost]
        [Route("GetHistoryTransactions")]
        [MapToApiVersion("1.0")]
        [HeaderModel]
        public IActionResult GetHistoryTransactions(SyncTransactionRequest model)
        {
            var result = _service.GetHistoryTransactions(model);
            return Ok(result);
        }
    }
}
