using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Infrastructure.SOInfrastructure;
using System.Collections.Generic;

namespace ODSaleOrder.API.Models.SaleHistories
{
    public class SaleHistoriesModel
    {
        public List<FfasoOrderInformation>? OrderList { get; set; }
        //public List<SO_OrderInformations>? soOrderInfomation { get; set; }
        public int? NEEDCONFIRM { get; set; }
        public int? DRAFT { get; set; }
        public int? WAITINGSHIPPING { get; set; }
        public int? SHIPPING { get; set; }
        public int? DELIVERED { get; set; }
        public int? FAILED { get; set; }
        public int? CANCEL { get; set; }
        public int? CANCELSHIPPING { get; set; }
        public int? WAITINGCONFIRM { get; set; }

    }
}
