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
    public class TempBudgetController : ControllerBase
    {
        private readonly ITempBudgetService _service;

        private readonly IHttpContextAccessor _contextAccessor;
        public TempBudgetController(IHttpContextAccessor contextAccessor, ITempBudgetService service)
        {
            _contextAccessor = contextAccessor;
            _service = service;
        }


        [HttpPost]
        [Route("Search")]
        [MapToApiVersion("1.0")]
        public async Task<IActionResult> Search(SearchBudgetModel parameters)
        {
            return Ok(await _service.Search(parameters));
        }
    }
}
