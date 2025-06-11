using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sys.Common.Models;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Sys.Common.Helper;
using ODSaleOrder.API.Services.SaleOrder.Interface;
using ODSaleOrder.API.Services.Base;
using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Models;
using static SysAdmin.API.Constants.Constant;
using RestSharp;
using SysAdmin.Models.StaticValue;
using RestSharp.Authenticators;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;

namespace ODSaleOrder.API.Services.SaleOrder
{
    public class ClientService : IClientService
    {
        private readonly ILogger<ClientService> _logger;
        private readonly IMapper _mapper;
        private readonly IBaseRepository<SO_Reason> _reasonRepository;
        private RestClient _client;
        private string _distributorCode = null;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public ClientService(ILogger<ClientService> logger,
            IMapper mapper,
            IBaseRepository<SO_Reason> reasonRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _mapper = mapper;
            _reasonRepository = reasonRepository;
            _httpContextAccessor = httpContextAccessor;
            _distributorCode = _httpContextAccessor.HttpContext?.Items["DistributorCode"] as string ?? _distributorCode;
        }

        public T CommonRequest<T>(string serviceCode, string route, RestSharp.Method method, string token, object dataRequest, bool isInputHeader = false)
        {
            try
            {
                token = token.Split(" ").Last();
                _client = new RestClient(CommonData.SystemUrl.Where(x => x.Code == serviceCode).Select(x => x.Url).FirstOrDefault());
                _client.Authenticator = new JwtAuthenticator($"Rdos {token}");
                var req = new RestRequest($"{route}", method, DataFormat.Json);
                if (dataRequest != null)
                {
                    req.AddJsonBody(dataRequest);
                }
                if (isInputHeader)
                {
                    req.AddHeader(OD_Constant.KeyHeader, _distributorCode);
                }
                var res = _client.Execute<T>(req);
                //var result = JsonConvert.DeserializeObject<T>(JsonConvert.DeserializeObject(res.Content).ToString());
                if (res.StatusCode == System.Net.HttpStatusCode.OK && res.Data != null) return res.Data;
                return default(T);
            }
            catch (System.Exception ex)
            {
                return default(T);
            }
        }


        public async Task<T> CommonRequestAsync<T>(string urlCode, string route, RestSharp.Method method, string token, object dataRequest)
        {
            try
            {
                token = token.Split(" ").Last();
                _client = new RestClient(CommonData.SystemUrl.Where(x => x.Code == urlCode).Select(x => x.Url).FirstOrDefault());
                _client.Authenticator = new JwtAuthenticator($"Rdos {token}");
                var req = new RestRequest($"{route}", method, DataFormat.Json);
                if (dataRequest != null)
                {
                    req.AddJsonBody(dataRequest);
                }
                var res = _client.Execute(req);
                if (typeof(T).Name.Equals("String"))
                {
                    return JsonConvert.DeserializeObject<T>(res.Content);
                }
                return JsonConvert.DeserializeObject<T>(JsonConvert.DeserializeObject(res.Content).ToString());

                // return result;
            }
            catch (System.Exception ex)
            {
                return default(T);
            }
        }

    }
}
