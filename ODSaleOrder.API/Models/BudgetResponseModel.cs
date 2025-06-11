namespace ODSaleOrder.API.Models
{
    public class BudgetResponseModel
    {
        public string budgetCode { get; set;}
        public string referalCode { get; set; }
        public string budgetType { get; set;}
        public string promotionCode { get; set; }
        public string promotionLevel { get; set; }
        public string customerCode { get; set;}
        public string customerShiptoCode { get; set;}
        public int budgetBook { get; set; }
        public int budgetBooked { get; set; }
        public bool budgetBookOver { get; set; }
        public bool status { get; set; }
        public string message { get; set; }
    }
}
