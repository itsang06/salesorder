using System;
using System.Collections.Generic;
using ODSaleOrder.API.Infrastructure;
using Sys.Common.Models;
using SysAdmin.Models.Enum;

namespace ODSaleOrder.API.Models
{

    public class ListCustomerInfoModel
    {
        public List<CustomerInfoModel> Items { get; set; }
        public MetaData MetaData { get; set; }
    }



    public class CustomerInfoModel
    {
        public Guid Id { get; set; }
        public string CustomerCode { get; set; }
        public string CodeAtVendor { get; set; }
        public string CodeAtDistributor { get; set; }
        public string ShortName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string BusinessAddress { get; set; }
        public CustomerStatusEnum Status { get; set; }
        public string ErpCode { get; set; }
        public DateTime CreatedDate { get; set; }
        public string TaxCode { get; set; }

        public bool IsDirectCustomer { get; set; }
        public string LegalInformation { get; set; }
        public string BankName { get; set; }
        public string BankAccount { get; set; }
        public string BankNumber { get; set; }
        public string Title { get; set; }
        public string BusinessTitle { get; set; }
        public string StreetLine { get; set; }
        public Guid? Wards { get; set; }
        public Guid? District { get; set; }
        public Guid? City { get; set; }
        public Guid? Province { get; set; }
        public Guid? State { get; set; }
        public Guid? Region { get; set; }
        public Guid Country { get; set; }

        public double Longtiue { get; set; }
        public double Lattitue { get; set; }
        public string DeptNo { get; set; }


        // public List<CustomerContactDetailModel> CustomerContact { get; set; }
        public List<CustomerShiptoDetailsModel> CustomerShiptos { get; set; }
    }


    public class CustomerShiptoDetailsModel
    {
        public Guid Id { get; set; }
        public string ShiptoCode { get; set; }

        public string ShiptoName { get; set; }

        public string CustomerCode { get; set; }
        public string Avatar { get; set; }
        public string CustomerName { get; set; }
        public CustomerStatusEnum Status { get; set; }
        public string BusinessStatus { get; set; }
        public string ClassType { get; set; }
        public string Address { get; set; }
        public string Street { get; set; }
        public string DeptNo { get; set; }
        public double Longtiue { get; set; }
        public double Lattitue { get; set; }

        public Guid? Wards { get; set; }
        public Guid? District { get; set; }
        public Guid? City { get; set; }
        public Guid? Province { get; set; }
        public Guid? State { get; set; }
        public Guid? Region { get; set; }
        public Guid Country { get; set; }
        public Guid CustomerInfomationId { get; set; }
        public Guid MainContactId { get; set; }
        // public List<CustomerShiptoContactModel> CustomerShiptoContacts { get; set; }
        public List<CustomerDmsAttributeModel> CustomerDmsAttributes { get; set; }
    }

    public class CustomerDmsAttributeModel
    {
        public Guid? Id { get; set; }
        public Guid CustomerAttributeId { get; set; }

        public CustomerAttributeModel CustomerAttribute { get; set; }
    }

    public class CustomerAttributeModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string AttributeMaster { get; set; }
        public string Description { get; set; }
        public string ShortName { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime? ValidUntil { get; set; }
        public bool IsDistributorAttribute { get; set; }
        public bool IsCustomerAttribute { get; set; }
        public string Parent { get; set; }
        public Guid? ParentCustomerAttributeId { get; set; }
        public CustomerSettingModel CustomerSetting { get; set; }
    }


    public class CustomerSettingModel
    {
        public Guid Id { get; set; }

        public string AttributeID { get; set; }
        public string AttributeName { get; set; }
        public string Description { get; set; }
        public bool IsDistributorAttribute { get; set; }
        public bool IsCustomerAttribute { get; set; }
        public bool Used { get; set; }
    }


    public class CustomerSettingHierarchyModel
    {
        public Guid Id { get; set; }

        public int? HierarchyLevel { get; set; }

        public CustomerSettingModel CustomerSetting { get; set; }
    }


    public class CustomerSettingHierarchyGetAllResultModel
    {
        public List<CustomerSettingHierarchyModel> CustomerHierarchies { get; set; }
        public List<CustomerSettingHierarchyModel> DistrutorHierarchies { get; set; }
        public bool IsSuccess { get; set; }
    }

}
