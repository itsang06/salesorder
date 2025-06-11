using System;
using System.Collections.Generic;

#nullable disable

namespace ODSaleOrder.API.Infrastructure
{
    public partial class UserPolicy
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid PolicyId { get; set; }

        public virtual Policy Policy { get; set; }
        public virtual User User { get; set; }
    }
}
