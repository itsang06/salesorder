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
    public class FfaSoOrderInformationController : ControllerBase
    {
        private readonly IFfaSoOrderInformationService _service;

        private readonly IHttpContextAccessor _contextAccessor;
        private readonly string _token;

        public FfaSoOrderInformationController(IHttpContextAccessor contextAccessor, IFfaSoOrderInformationService service)
        {
            _contextAccessor = contextAccessor;
            _service = service;
            _token = _contextAccessor.HttpContext.Request.Headers["Authorization"];
        }

        [HttpPost]
        [Route("Create")]
        [MapToApiVersion("1.0")]
        public  IActionResult CreateFfaSoOrderInformation(FfasoOrderInformation model)
        {
            var result = _service.CreateFfaSoOrderInformation(model, _token, User.GetName());
            return Ok(result);
        }

        [HttpPut]
        [Route("Update")]
        [MapToApiVersion("1.0")]
        public IActionResult UpdateFfaSoOrderInformation(FfasoOrderInformation model)
        {
            var result = _service.UpdateFfaSoOrderInformation(model, _token, User.GetName());
            return Ok(result);
        }


        [HttpDelete]
        [Route("Delete/{Id}")]
        [MapToApiVersion("1.0")]
        [HeaderModel]
        public IActionResult DeleteFfaSoOrderInformation(Guid Id)
        {
            var result = _service.DeleteFfaSoOrderInformation(Id, _token, User.GetName());
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
        public IActionResult InsertMany(List<FfasoOrderInformation> model)
        {
            var result = _service.InsertOrUpdate(model);
            return Ok(result);
        }

        [HttpPut]
        [Route("UpdateMany")]
        [MapToApiVersion("1.0")]
        [HeaderModel]
        public IActionResult UpdateMany(List<FfasoOrderInformation> model)
        {
            var result = _service.InsertOrUpdate(model);
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

        [HttpGet]
        [Route("GetOrderByVisitId/{VisitId}")]
        [MapToApiVersion("1.0")]
        [HeaderModel]
        public IActionResult GetOrderDetailByVisitId(string VisitId)
        {
            var result = _service.GetOrderDetailByVisitId(VisitId);
            return Ok(result);
        }
    }
}
