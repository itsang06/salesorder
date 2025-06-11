using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

namespace ODSaleOrder.API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [Authorize]
    [ApiVersion("1.0")]
    public class ReasonController : ControllerBase
    {
        private readonly IReasonService _service;

        private readonly IHttpContextAccessor _contextAccessor;
        private readonly string _token;
        public ReasonController(IHttpContextAccessor contextAccessor, IReasonService service)
        {
            _contextAccessor = contextAccessor;
            _service = service;
            _token = _contextAccessor.HttpContext.Request.Headers["Authorization"];
        }

        [HttpPost]
        [Route("BulkUpsert")]
        [MapToApiVersion("1.0")]
        public async Task<IActionResult> BulkUpsert(List<SO_Reason> model)
        {
            return Ok(await _service.BulkUpsertReason(model, User.GetName()));
        }
        
        [HttpGet]
        [Route("CheckInUsed")]
        [MapToApiVersion("1.0")]
        public async Task<IActionResult> CheckInUsed(string ReasonCode)
        {
            return Ok(await _service.CheckInUsed(ReasonCode));
        }

        [HttpPost]
        [Route("Search")]
        [MapToApiVersion("1.0")]
        public async Task<IActionResult> SearchReason(EcoparamsWithGenericFilter parameters)
        {
            return Ok(await _service.SearchReason(parameters));
        }
    }
}
