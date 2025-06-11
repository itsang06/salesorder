using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

#nullable disable

namespace ODSaleOrder.API.Infrastructure
{
    public partial class Temp_Programs
    {
        public Guid Id { get; set; }
      [MaxLength(250)]  public string ProgramCode { get; set; }
      [MaxLength(250)]  public string ProgramsType { get; set; } //Promotion, Display, Accumulate
      [MaxLength(250)]  public string Description { get; set; } //Mô tả chương trình
      [MaxLength(250)]  public string ItemScope { get; set; } //Line, Group, Bundle
      [MaxLength(250)]  public string BuyType { get; set; } // Quantity, Amount, 
      [MaxLength(250)]  public string GivingType { get; set; } // FreeItem, Amount, Percented
        public bool IsDeleted {get;set;}

    }
}
