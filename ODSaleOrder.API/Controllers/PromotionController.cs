using DynamicSchema.Helper.Models.Header;
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
    public class PromotionController : ControllerBase
    {
        private readonly IPromotionsService _service;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly string _token;
        public PromotionController(IHttpContextAccessor contextAccessor, IPromotionsService service)
        {
            _contextAccessor = contextAccessor;
            _service = service;
            _token = _contextAccessor.HttpContext.Request.Headers["Authorization"];
        }



        [HttpPost]
        [Route("GenRefPromotionNumber")]
        [MapToApiVersion("1.0")]
        [HeaderModel]
        public async Task<IActionResult> GenRefPromotionNumber()
        {
            return Ok(await _service.GenRefPromotionNumber(new PromoRefRequestModel
            {
                Username = User.GetName()
            }));
        }

        // [HttpPost]
        // [Route("GenRefPromotionNumberv2")]
        // [MapToApiVersion("1.0")]
        // public async Task<IActionResult> GenRefPromotionNumberv2(PromoRefRequestModel model)
        // {
        //     return Ok(await _service.GenRefPromotionNumber(model));
        // }

        [HttpPost]
        [Route("GetListProgramsByCustomerID/{promotionRefNumber}/{programType}")]
        [MapToApiVersion("1.0")]
        [HeaderModel]
        public async Task<IActionResult> GetListProgramsByCustomerID(CustomerPromotionRequestModel cusInfo, string promotionRefNumber, string programType)
        {
            return Ok(await _service.GetListProgramsByCustomerID(cusInfo, promotionRefNumber, programType, _token));
        }


        [HttpPost]
        [Route("UpsertMutipleCustomerProgram")]
        [MapToApiVersion("1.0")]
        [HeaderModel]
        public async Task<IActionResult> UpsertMutipleCustomerProgram(List<PromotionCustomerModel> models, bool includeSaved = true)
        {
            return Ok(await _service.UpsertMutipleCustomerProgram(models, includeSaved, User.GetName(), _token));
        }

        [HttpPost]
        [Route("GetRewardItemChange")]
        [MapToApiVersion("1.0")]
        [HeaderModel]
        public async Task<IActionResult> GetRewardItemChange(RewardItemChangeRequestModel model)
        {
            return Ok(await _service.GetRewardItemChange(model.PromotionCode, model.DetailCode, model.ExcludedProduct, _token));
        }

    }
}
