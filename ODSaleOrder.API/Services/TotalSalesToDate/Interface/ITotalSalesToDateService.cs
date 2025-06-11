
using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Infrastructure.SOInfrastructure;
using Sys.Common.Models;
using System;
using System.Collections.Generic;

namespace ODSaleOrder.API.Services.TotalSalesToDate.Interface
{
    public interface ITotalSalesToDateService
    {
        BaseResultModelMobile GetTotalSalesToDate(string employeeCode, string token);
    }
}
