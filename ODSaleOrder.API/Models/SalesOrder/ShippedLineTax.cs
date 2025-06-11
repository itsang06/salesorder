namespace ODSaleOrder.API.Models.SalesOrder
{
    public class ShippedLineTax
    {
        public decimal vatValue { get; set; } = 0;
        public double shipped_Line_AfterTax_Amt { get; set; } = 0;
        public double shipped_Line_BeforeTax_Amt { get; set; } = 0;
        public decimal disCountAmount { get; set; } = 0;
        public decimal shipped_line_Disc_Amt { get; set; } = 0;
        public decimal shipped_Line_Amt { get; set; } = 0;
        public bool salespriceincludeVAT { get; set; }
    }
}
