using System;
using System.Collections.Generic;

namespace ODSaleOrder.API.Models
{
    public class SalesCalendarResultModel
    {
        public Guid Id { get; set; }
        public int SaleYear { get; set; }
        public string StartDayOfWeek { get; set; }
        public string IncludeWeekend { get; set; }
        public int NumberOfWorkingDay { get; set; }
        public string QuarterStructure { get; set; }
        public string Status { get; set; }
    }

    public class ListSalesCalendar
    {
        public List<SalesCalendarResultModel> Data { get; set; }
    }
}
