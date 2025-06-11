using System;
using System.Collections.Generic;

#nullable disable

namespace ODSaleOrder.API.Infrastructure
{
    public partial class UserToken
    {
        public Guid UserId { get; set; }
        public string LoginProvider { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }

        public virtual User User { get; set; }
    }
}
