using ODSaleOrder.API.Infrastructure;
using Sys.Common.Models;
using System.Collections.Generic;

namespace ODSaleOrder.API.Models
{
    public class TempBudgetModel
    {
        public List<Temp_SOBudgets> Items { get; set; }
        public MetaData MetaData { get; set; }
    }
    
    public class SearchBudgetModel : EcoParameters
    {
        public string CustomerCode { get; set; }
        public string CustomerShiptoCode { get; set; }
        public string PromotionCode { get; set; }
        public string PromotionLevel { get; set; }
        public string BudgetCode { get; set; }
    }
}
