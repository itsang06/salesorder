using ODSaleOrder.API.Models.SaleHistories;
using Sys.Common.Models;
using System.Collections.Generic;

namespace ODSaleOrder.API.Services.SaleHistories
{
    public interface ISaleHistoriesService
    {
        ResultCustomSale<SaleHistoriesModel> SaleHistories(SearchModelv2 _search);
        BaseResultModel OrderResult(string EmployeeCode, string VisitDate);
        BaseResultModel SalesVolumnReport(SaleVolumnReportRequest  _search);
    }
}