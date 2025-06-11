namespace ODSaleOrder.API.Models.SalesOrder
{
    public class OrderLineTaxModel
    {
        public decimal vatValue { get; set; } = 0;
        public double ord_Line_TotalAfterTax_Amt { get; set; } = 0;
        public double ord_Line_TotalBeforeTax_Amt { get; set; } = 0;
        public decimal disCountAmount { get; set; } = 0;
        public decimal orig_Ord_line_Disc_Amt { get; set; } = 0;
        public decimal ord_Line_Amt { get; set; } = 0;
        public bool salespriceincludeVAT { get; set; }
        public bool IsBeforeTax { get; set; }
    }
}