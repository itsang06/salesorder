using DynamicSchema.Helper.Models.Header;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Models;
using ODSaleOrder.API.Services.SaleOrder.Interface;
using ODSaleOrder.API.Services.Stock;
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
    public class StockController : ControllerBase
    {
        private readonly IStockService _service;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly string _token;
        public StockController(IHttpContextAccessor contextAccessor, IStockService service)
        {
            _contextAccessor = contextAccessor;
            _service = service;
            _token = _contextAccessor.HttpContext.Request.Headers["Authorization"];
        }

        [HttpPost]
        [Route("GetProductWithStock")]
        [HeaderModel]
        public IActionResult GetProductWithStock(ProductWithStockModel body)
        {
            var distributorCode = HttpContext.Request.Headers["DistributorCode"].ToString();
            body.DistributorCode = distributorCode;
            return Ok(_service.GetProductWithStock(body, _token));
        }

        [HttpGet]
        [Route("GetOrderReasonList")]
        public IActionResult GetOrderReasonList()
        {
            return Ok(_service.GetOrderReasonList());
        }

    }
}
