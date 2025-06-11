using nProx.Helpers.Models;
using System.Collections.Generic;

namespace ODSaleOrder.API.Models
{
    public class GeneralSearch : SimpleSearch
    {
        public Dictionary<string, string> SearchDynamic { get; set; }
    }
}
