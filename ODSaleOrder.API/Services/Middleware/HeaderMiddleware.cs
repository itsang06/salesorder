using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using ODSaleOrder.API.Services.PrincipalService;
using static SysAdmin.API.Constants.Constant;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SysAdmin.API.Constants;

namespace ODSaleOrder.API.Services.Middleware
{
    public class HeaderMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HeaderMiddleware(RequestDelegate next, IHttpContextAccessor httpContextAccessor)
        {
            _next = next;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task Invoke(HttpContext context)
        {
            string distributorCode = _httpContextAccessor.HttpContext.Request.Headers[OD_Constant.KeyHeader];

            if (!string.IsNullOrEmpty(distributorCode))
            {
                using (var scope = context.RequestServices.CreateScope())
                {
                    var principalService = scope.ServiceProvider.GetRequiredService<IPrincipalService>();
                    var resultNavigate = await principalService.NavigatePrivateSchema(distributorCode);

                    if (!resultNavigate.IsSuccess)
                    {
                        context.Response.StatusCode = 400;
                        context.Response.ContentType = "application/json";

                        await context.Response.WriteAsync(JsonConvert.SerializeObject(new
                        {
                            IsSuccess = false,
                            Code = resultNavigate.Code,
                            Message = resultNavigate.Message,
                        }));
                        return;
                    }
                }
            }
            else
            {
                OD_Constant.SchemaName = OD_Constant.DEFAULT_SCHEMA;
            }

            context.Items["SchemaName"] = OD_Constant.SchemaName;
            context.Items["DistributorCode"] = OD_Constant.DistributorCode;
            await _next(context);
        }
    }
}
