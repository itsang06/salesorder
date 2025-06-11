using System;
using System.Collections.Generic;
using ODSaleOrder.API.Infrastructure;
using Sys.Common.Models;

namespace ODSaleOrder.API.Models
{
    public class EmployeeModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string EmployeeCode { get; set; }
        public string FullName { get; set; }
        public string ShortName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
        public string Position { get; set; }
        public string EmailType { get; set; }
        public string Email { get; set; }
        public string PhoneType { get; set; }
        public string PhoneNumber { get; set; }
        public string ZaloAccount { get; set; }
        public string ViberAccount { get; set; }
        public string Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Idcard { get; set; }
        public string Idcard2 { get; set; }
        public string PermanentAddress { get; set; }
        public string BusinessAddress { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }
    }

    public class PrincipleEmployeeModel : AuditTable
    {
        public Guid Id { get; set; }
        public string AccountName { get; set; }
        public string AccountPassword { get; set; }
        public string AccountStatus { get; set; }
        public string EmployeeCode { get; set; }
        public string PrincipalEmpCode { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
        public DateTime? StartWorkingDate { get; set; }
        public object TerminateDate { get; set; }
        public object Title { get; set; }
        public object Position { get; set; }
        public string Gender { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Idcard { get; set; }
        public string Idcard2 { get; set; }
        public object InsuranceId { get; set; }
        public object TaxNumber { get; set; }
        public string EmailType { get; set; }
        public string Email { get; set; }
        public string MainPhoneNumber { get; set; }
        public string ExtraPhoneNumber { get; set; }
        public int SaleGroup { get; set; }
        public string JobTitle { get; set; }
        public string Territory { get; set; }
        public object AddressCountry { get; set; }
        public string AddressRegion { get; set; }
        public string AddressProvince { get; set; }
        public string AddressDistrict { get; set; }
        public string AddressWard { get; set; }
        public object AddressCity { get; set; }
        public string AddressState { get; set; }
        public string AddressStreet { get; set; }
        public string AddressDeparmentNo { get; set; }
        public object FullAddress { get; set; }
        public object BankName { get; set; }
        public string FullName { get; set; }
        public object BankBranch { get; set; }
        public object BankAccountName { get; set; }
        public object BankAccountNumber { get; set; }
        public object AvartarFilePath { get; set; }
        public string Status { get; set; }
        public string DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }
        public List<ExtraPhoneNumberDeserialized> ExtraPhoneNumberDeserialized { get; set; }
        public string SaleGroupName { get; set; }
        public string JobTitleName { get; set; }

        //For IMV Module
        public string EmployeeCodeDescription { get; set; }
        public bool IsChecked { get; set; }
    }

    public class ExtraPhoneNumberDeserialized
    {
        public string Type { get; set; }
        public string PhoneNumber { get; set; }
    }

    public class PrincipleEmployeeListModel
    {
        public List<PrincipleEmployeeModel> Items { get; set; }
        public MetaData MetaData { get; set; }
    }

    public class EmployeeParameter
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string OrderBy { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string Search { get; set; }
        public string EmployeeId { get; set; }
        public string SocialId { get; set; }
        public string Status { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string JobTitle { get; set; }
    }

    public class SearchEmployeeCodeRequest
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string OrderBy { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string EmployeeCode { get; set; }
    }

    public class HOResultModel
    {
        public string EmployeeCode { get; set; }
        public string AccountName { get; set; }
        public string AccountStatus { get; set; }
    }
}
