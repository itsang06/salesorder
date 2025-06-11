using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RDOS.INVAPI.Infratructure
{
    public partial class InvAllocationTracking
    {
        public Guid Id { get; set; }
        public string ItemKey { get; set; }
        public Guid ItemId { get; set; }
        public string ItemCode { get; set; }
        public string BaseUom { get; set; }
        public string ItemDescription { get; set; }
        public string WareHouseCode { get; set; }
        public string LocationCode { get; set; }
        public string DistributorCode { get; set; }
        public int OnHandBeforChanged { get; set; }
        public int OnHandToChanged { get; set; }
        public int OnHandChanged { get; set; }
        public int OnSoShippingBeforChanged { get; set; }
        public int OnSoShippingToChanged { get; set; }
        public int OnSoShippingChanged { get; set; }
        public int OnSoBookedBeforChanged { get; set; }
        public int OnSoBookedToChanged { get; set; }
        public int OnSoBookedChanged { get; set; }
        public int AvailableBeforChanged { get; set; }
        public int AvailableToChanged { get; set; }
        public int AvailableChanged { get; set; }
        public string ItemGroupCode { get; set; }
        public string DSACode { get; set; }
        public string FromFeature { get; set; }
        public DateTime RequestDate { get; set; }
        public Guid RequestId { get; set; }
        public string RequestBody {get;set;}
        public Boolean IsSuccess {get;set;} = true;
        [MaxLength(100)] public string OwnerType { get; set; }
        [MaxLength(255)] public string OwnerCode { get; set; }
    }
}
