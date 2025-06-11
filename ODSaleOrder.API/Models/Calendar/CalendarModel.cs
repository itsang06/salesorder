using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ODSaleOrder.API.Infrastructure;
using Sys.Common.Models;
using static SysAdmin.API.Constants.Constant;

namespace ODSaleOrder.API.Models
{
    public class CalendarModel
    {
        public Guid Id { get; set; }
        public Guid SaleCalendarId { get; set; }
        public string Type { get; set; }
        public string Code { get; set; }
        public int Ordinal { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }
    }


    public class ResultCalendar
    {
        public List<CalendarModel> Items { get; set; }
        public MetaData MetaData { get; set; }
    }
}
