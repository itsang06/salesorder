using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ODSaleOrder.API.Infrastructure.SOInfrastructure;
using ODSaleOrder.API.Services.Ffa.Interface;
using System.Collections.Generic;
using DynamicSchema.Helper.Models.Header;
using ODSaleOrder.API.Infrastructure;

namespace ODSaleOrder.API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class FFASoSuggestOrderController : ControllerBase
    {
        private readonly IFFASoSuggestOrderService _service;

        private readonly IHttpContextAccessor _contextAccessor;
        private readonly string _token;

        public FFASoSuggestOrderController(IHttpContextAccessor contextAccessor, IFFASoSuggestOrderService service)
        {
            _contextAccessor = contextAccessor;
            _service = service;
            _token = _contextAccessor.HttpContext.Request.Headers["Authorization"];
        }

        [HttpPost]
        [Route("InsertMany")]
        [MapToApiVersion("1.0")]
        [HeaderModel]
        public IActionResult InsertMany(List<FFASoSuggestOrder> model)
        {
            var result = _service.InsertOrUpdate(model);
            return Ok(result);
        }

        [HttpPut]
        [Route("UpdateMany")]
        [MapToApiVersion("1.0")]
        [HeaderModel]
        public IActionResult UpdateMany(List<FFASoSuggestOrder> model)
        {
            var result = _service.InsertOrUpdate(model);
            return Ok(result);
        }

    }
}
