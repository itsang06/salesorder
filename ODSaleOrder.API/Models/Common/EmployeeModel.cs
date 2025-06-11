using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ODSaleOrder.API.Models.Common
{
    public class EmployeeModel
    {
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public string RouteZoneCode { get; set; }
        public string DsaCode { get; set; }
        public string DistributorCode { get; set; }
        public string BeatPlanCode { get; set; }
        public string FullAddress { get; set; }
        public string JobTitle { get; set; }
        public string MainPhoneNumber { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Idcard { get; set; }
        public string Gender { get; set; }
    }
}
