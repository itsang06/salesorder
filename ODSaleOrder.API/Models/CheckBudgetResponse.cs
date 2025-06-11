using System.Collections.Generic;
using System;
using Newtonsoft.Json;

namespace ODSaleOrder.API.Models
{
    public class CheckBudgetResponse
    {
        [JsonProperty("objectId")]
        public int ObjectId { get; set; }

        [JsonProperty("objectGuidId")]
        public Guid ObjectGuidId { get; set; }

        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("isSuccess")]
        public bool IsSuccess { get; set; }

        [JsonProperty("data")]
        public List<BudgetData> Data { get; set; }
    }
    public class BudgetData
    {
        [JsonProperty("budgetCode")]
        public string BudgetCode { get; set; }

        [JsonProperty("budgetType")]
        public string BudgetType { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("customerCode")]
        public string CustomerCode { get; set; }

        [JsonProperty("customerShiptoCode")]
        public string CustomerShiptoCode { get; set; }

        [JsonProperty("budgetAllocationLevel")]
        public string BudgetAllocationLevel { get; set; }

        [JsonProperty("salesTerritoryValueCode")]
        public string SalesTerritoryValueCode { get; set; }

        [JsonProperty("budgetRemains")]
        public int? BudgetRemains { get; set; }

        [JsonProperty("customerBudget")]
        public int? CustomerBudget { get; set; }

        [JsonProperty("budgetBooked")]
        public int? BudgetBooked { get; set; }
    }


}
