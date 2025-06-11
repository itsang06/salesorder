using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DynamicSchema.Helper.Models.Header;
using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Infrastructure.SOInfrastructure;
using ODSaleOrder.API.Services.TotalSalesToDate.Interface;
using Sys.Common.JWT;
using Sys.Common.Utils;
using System;
using System.Collections.Generic;

namespace ODSaleOrder.API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [Authorize]
    [ApiVersion("1.0")]
    public class TotalSalesToDateController : ControllerBase
    {
        private readonly ITotalSalesToDateService _service;

        private readonly IHttpContextAccessor _contextAccessor;
        private readonly string _token;

        public TotalSalesToDateController(IHttpContextAccessor contextAccessor, ITotalSalesToDateService service)
        {
            _contextAccessor = contextAccessor;
            _service = service;
            _token = _contextAccessor.HttpContext.Request.Headers["Authorization"];
        }

        [HttpGet]
        [Route("GetTotalSalesToDate")]
        [MapToApiVersion("1.0")]
        [HeaderModel]
        public IActionResult GetTotalSalesToDate(string employeeCode)
        {
            var result = _service.GetTotalSalesToDate(employeeCode, _token);
            return Ok(result);
        }
    }
}
