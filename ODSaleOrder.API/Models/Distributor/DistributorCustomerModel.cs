using Sys.Common.Models;
using System;
using System.Collections.Generic;

namespace ODSaleOrder.API.Models.Distributor
{
    public class DistributorCustomerModel
    {
        public string CustomerCode { get; set; }
        public string CustomerName { get; set; }
        public string CustomerAddress { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string ShiptoCode { get; set; }
        public string ShiptoName { get; set; }
        public string ShiptoAddress { get; set; }
        public double Lattitue { get; set; }
        public double Longtiue { get; set; }
        public string CustomerType { get; set; }
    }

    public class DistributorCustomerShiptoModel
    {
        public Guid Id { get; set; }
        public string ShiptoCode { get; set; }
        public string ShiptoName { get; set; }
        public string ShiptoAddress { get; set; }
        public Guid? Province { get; set; }
        public Guid? District { get; set; }
        public Guid? Wards { get; set; }
        public Guid? Country { get; set; }
        public Guid? City { get; set; }
        public Guid? Region { get; set; }
        public Guid? State { get; set; }
        public string ProvinceCode { get; set; }
        public string DistrictCode { get; set; }
        public string WardCode { get; set; }
        public string CountryCode { get; set; }
        public string CityCode { get; set; }
        public string RegionCode { get; set; }
        public string StateCode { get; set; }
    }

    public class DistributorCustomerWithPagingModel
    {
        public Guid CustomerId { get; set; }
        public string CustomerCode { get; set; }
        public string CustomerName { get; set; }
        public string CustomerAddress { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string CustomerType { get; set; }
        public bool IsFirstTimeCustomer { get; set; }
        public int TotalCount { get; set; }
    }

    public class ListDistributorCustomerModel
    {
        public List<DistributorCustomerWithPagingModel> Items { get; set; }
        public MetaData MetaData { get; set; }
    }

    public class DisCusShiptoDetailModel
    {
        public Guid Id { get; set; }
        public string ShiptoCode { get; set; }
        public string ShiptoName { get; set; }
        public string ShiptoAddress { get; set; }

        // Attributes 1 to 10
        public Guid? Shipto_AttributeId1 { get; set; }
        public string Shipto_Attribute1 { get; set; }

        public Guid? Shipto_AttributeId2 { get; set; }
        public string Shipto_Attribute2 { get; set; }

        public Guid? Shipto_AttributeId3 { get; set; }
        public string Shipto_Attribute3 { get; set; }

        public Guid? Shipto_AttributeId4 { get; set; }
        public string Shipto_Attribute4 { get; set; }

        public Guid? Shipto_AttributeId5 { get; set; }
        public string Shipto_Attribute5 { get; set; }

        public Guid? Shipto_AttributeId6 { get; set; }
        public string Shipto_Attribute6 { get; set; }

        public Guid? Shipto_AttributeId7 { get; set; }
        public string Shipto_Attribute7 { get; set; }

        public Guid? Shipto_AttributeId8 { get; set; }
        public string Shipto_Attribute8 { get; set; }

        public Guid? Shipto_AttributeId9 { get; set; }
        public string Shipto_Attribute9 { get; set; }

        public Guid? Shipto_AttributeId10 { get; set; }
        public string Shipto_Attribute10 { get; set; }
    }

}
