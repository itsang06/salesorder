using System;
using System.Collections.Generic;
using ODSaleOrder.API.Infrastructure;
using Sys.Common.Models;

namespace ODSaleOrder.API.Models
{
      public class ListReasonModel
    {
        public List<SO_Reason> Items { get; set; }
        public MetaData MetaData { get; set; }
    }
}
