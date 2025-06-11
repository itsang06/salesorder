using System;
using System.Linq;
using System.Text.RegularExpressions;
using AutoMapper.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RDOS.INVAPI.Infratructure;
using ODSaleOrder.API.Infrastructure.SOInfrastructure;
using ODSaleOrder.API.Models;
using ODSaleOrder.API.Models.PrincipalModel;
using ODSaleOrder.API.Models.ReportModel;
using ODSaleOrder.API.Models.SaleHistories;
using ODSaleOrder.API.Models.SyncHistory;
using ODSaleOrder.API.Models.Distributor;

#nullable disable

namespace ODSaleOrder.API.Infrastructure
{
    public partial class RDOSContext : DbContext
    {


        public RDOSContext(DbContextOptions<RDOSContext> options)
            : base(options)
        {

        }

        public RDOSContext(string principalCode, IConfiguration configuration)
            : base(GetOptions(principalCode, configuration))
        {

        }

        private static DbContextOptions GetOptions(string principalCode, IConfiguration configuration)
        {
            string conn = Environment.GetEnvironmentVariable("CONNECTION");
            return NpgsqlDbContextOptionsBuilderExtensions.UseNpgsql(new DbContextOptionsBuilder(), conn).Options;
        }

        private static string ConnectionString(string principalDatabase, IConfiguration configuration)
        {
            //var conn = configuration.GetConnectionString("DefaultConnection");
            var conn = Environment.GetEnvironmentVariable("CONNECTION");
            Regex obj = new Regex(@"(Database=){1}([\w-]+)[;]?");
            Match match = obj.Match(conn);
            string currentDatabase = match.Value;
            string principalDatabaseString = $"Database={principalDatabase};";
            conn = conn.Replace(currentDatabase, principalDatabaseString);
            return conn;
        }

        // public virtual DbSet<Action> Actions { get; set; }
        public virtual DbSet<Application> Applications { get; set; }
        public virtual DbSet<ApplicationService> ApplicationServices { get; set; }
        public virtual DbSet<PhoneType> PhoneTypes { get; set; }
        public virtual DbSet<Policy> Policies { get; set; }
        public virtual DbSet<Role> Roles { get; set; }
        public virtual DbSet<RoleClaim> RoleClaims { get; set; }
        public virtual DbSet<Service> Services { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<InvoiceOrder> SoInvoiceOrders { get; set; }
        public virtual DbSet<UserClaim> UserClaims { get; set; }
        public virtual DbSet<UserLogin> UserLogins { get; set; }
        public virtual DbSet<UserLoginLog> UserLoginLogs { get; set; }
        public virtual DbSet<UserPolicy> UserPolicies { get; set; }
        public virtual DbSet<UserRole> UserRoles { get; set; }
        public virtual DbSet<UserToken> UserTokens { get; set; }
        public virtual DbSet<UserType> UserTypes { get; set; }
        public virtual DbSet<Version> Versions { get; set; }

        //Sales Order
        public virtual DbSet<SO_FirstTimeCustomer> SO_FirstTimeCustomers { get; set; }
        public virtual DbSet<SO_OrderInformations> SO_OrderInformations { get; set; }
        public virtual DbSet<SO_OrderItems> SO_OrderItems { get; set; }
        public virtual DbSet<SO_SumPickingListHeader> SO_SumPickingListHeaders { get; set; }
        public virtual DbSet<SO_SumPickingListDetail> SO_SumPickingListDetails { get; set; }
        public virtual DbSet<SO_Reason> SO_Reasons { get; set; }
        public virtual DbSet<OsOrderInformation> OsOrderInformations { get; set; }
        public virtual DbSet<OsOrderItem> OsOrderItems { get; set; }

        public virtual DbSet<ODMappingOrderStatus> OdmappingOrderStatuses { get; set; }

        public virtual DbSet<Temp_SOBudgets> Temp_SOBudgets { get; set; }

        //Temp_Promotion
        public virtual DbSet<Temp_Programs> Temp_Programs { get; set; }
        public virtual DbSet<Temp_ProgramsDetails> Temp_ProgramsDetails { get; set; }
        public virtual DbSet<Temp_ProgramDetailsItemsGroup> Temp_ProgramDetailsItemsGroup { get; set; }
        public virtual DbSet<Temp_ProgramDetailReward> Temp_ProgramDetailReward { get; set; }
        public virtual DbSet<ProgramCustomers> SO_PRO_ProgramCustomers { get; set; }
        public virtual DbSet<ProgramCustomersDetail> SO_PRO_ProgramCustomersDetails { get; set; }
        public virtual DbSet<ProgramCustomerItemsGroup> SO_PRO_ProgramCustomerItemsGroup { get; set; }
        public virtual DbSet<ProgramCustomerDetailsItems> SO_PRO_ProgramCustomerDetailsItems { get; set; }
        // public virtual DbSet<Temp_PromotionOrderRefNumber> Temp_PromotionOrderRefNumber { get; set; }

        public virtual DbSet<FfasoOrderInformation> FFASoOrderInformations { get; set; }
        public virtual DbSet<FfasoOrderItem> FFASoOrderItems { get; set; }
        public virtual DbSet<FfasoImportItem> FFASoImportItems { get; set; }
        public virtual DbSet<FfadsSoLot> FfadsSoLots { get; set; }
        public virtual DbSet<FfadsSoPayment> FfadsSoPayments { get; set; }
        public virtual DbSet<FFASoSuggestOrder> FFASoSuggestOrders { get; set; }
        
        public virtual DbSet<SO_SalesOrderSetting> SO_SalesOrderSettings { get; set; }

        public virtual DbSet<Principal> Principals { get; set; }
        public virtual DbSet<SaleCalendar> SaleCalendars { get; set; }
        public virtual DbSet<SaleCalendarGenerate> SaleCalendarGenerates { get; set; }
        public virtual DbSet<SaleCalendarHoliday> SaleCalendarHolidays { get; set; }
        public virtual DbSet<Kit> Kits { get; set; }
        public virtual DbSet<Vat> Vats { get; set; }
        public virtual DbSet<OrderResultModel> OrderResult { get; set; }
        public virtual DbSet<SaleVolumnReportModel> SaleVolumnReport { get; set; }
        public virtual DbSet<INV_InventoryTransaction> INV_InventoryTransactions { get; set; }
        public virtual DbSet<INV_AllocationDetail> INV_AllocationDetails { get; set; }
        public virtual DbSet<InvAllocationTracking> INV_AllocationTracking { get; set; }
        public virtual DbSet<FFAOrderInfoExisted> FFAOrderInfoExisted { get; set; }
        public virtual DbSet<FFAOrderItemExisted> FFAOrderItemExisted { get; set; }
        public virtual DbSet<ProductivityReportModel> ProductivityReport { get; set; }
        public virtual DbSet<ProductivityByDayReportModel> ProductivityByDayReport { get; set; }
        public virtual DbSet<CommonSoOrderModel> CommonSoOrderModel { get; set; }
        public virtual DbSet<DisProductivityBySalesReportModel> DisProductivityBySalesReport { get; set; }
        public virtual DbSet<HoProductivityBySalesReportModel> HoProductivityBySalesReport { get; set; }
        public virtual DbSet<FnProductivityReportModel> FnProductivityReportModels { get; set; }
        public virtual DbSet<FnProductivityReportWithRouteZoneModel> FnProductivityReportWithRouteZoneModels { get; set; }
        public virtual DbSet<FnProductivityByDayReportModel> FnProductivityByDayReportModels { get; set; }
        public virtual DbSet<FnProductivityByDayReportWithRouteZoneModel> FnProductivityByDayReportWithRouteZoneModels { get; set; }
        public virtual DbSet<FnSaslesDetailReportModel> FnSaslesDetailReportModels { get; set; }
        public virtual DbSet<FnReportShippingStatusModel> FnReportShippingStatusModels { get; set; }
        public virtual DbSet<FnSalesSynthesisReportModel> FnSalesSynthesisReportModels { get; set; }
        public virtual DbSet<FnDsaProductivityByProductReportModel> FnDsaProductivityByProductReportModels { get; set; }
        public virtual DbSet<FnRouteZoneProductivityByProductReportModel> FnRouteZoneProductivityByProductReportModels { get; set; }
        public virtual DbSet<FnReportTrackingOrderModel> FnReportTrackingOrderModels { get; set; }

        public virtual DbSet<ODDistributorSchema> ODDistributorSchemas { get; set; }
        public virtual DbSet<OsorderStatusHistory> OsorderStatusHistories { get; set; }
        public virtual DbSet<SystemSetting> SystemSettings { get; set; }

        public virtual DbSet<PrincipalWarehouseLocation> PrincipalWarehouseLocations { get; set; }

        // SO Order Recall Request
        public virtual DbSet<SoorderRecallReq> SoorderRecallReqs { get; set; }
        public virtual DbSet<SoorderRecallReqGiveBack> SoorderRecallReqGiveBacks { get; set; }
        public virtual DbSet<SoorderRecallReqOrder> SoorderRecallReqOrders { get; set; }
        public virtual DbSet<SoorderRecallReqScope> SoorderRecallReqScopes { get; set; }
        public virtual DbSet<SoorderRecall> SoorderRecalls { get; set; }
        public virtual DbSet<SoorderRecallOrder> SoorderRecallOrders { get; set; }

        public virtual DbSet<StagingSyncDataHistory> StagingSyncDataHistories { get; set; }

        //Khoa enhacne 
        public virtual DbSet<Models.Common.EmployeeModel> DistributorEmployee { get; set; }
        public virtual DbSet<Models.Common.EmployeeV2Model> DistributorEmployeeV2 { get; set; }
        
        public virtual DbSet<DistributorRouteZoneModel> DistributorRouteZone { get; set; }
        public virtual DbSet<DisRouteZoneBasicModel> DisRouteZoneBasic { get; set; }
        public virtual DbSet<DistributorCustomerModel> DistributorCustomer { get; set; }
        public virtual DbSet<DistributorCustomerWithPagingModel> DistributorCustomerWithPaging { get; set; }
        public virtual DbSet<DistributorCustomerShiptoModel> DisCustomerShipto { get; set; }
        public virtual DbSet<DisCusShiptoDetailModel> DisCusShiptoDetail { get; set; }

        public virtual DbSet<DistributorCommonInfoModel> DistributorCommonInfo { get; set; }
        public virtual DbSet<DistributorBasicInfoModel> DistributorBasicInfo { get; set; }
         
        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //{
        //    if (!optionsBuilder.IsConfigured)
        //    {
        //        optionsBuilder.UseNpgsql("Server=db.rdos.online;Port=5494;Database=onesdev_system;User Id=postgresrdos;Password=PAssword65464");
        //    }
        //}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresExtension("uuid-ossp")
                .HasAnnotation("Relational:Collation", "en_US.UTF-8");

            modelBuilder.Entity<SaleCalendar>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.CreatedDate).HasColumnType("timestamp without time zone");

                entity.Property(e => e.DeletedDate).HasColumnType("timestamp without time zone");

                entity.Property(e => e.LastDayOfFirstWeek).HasColumnType("timestamp without time zone");

                entity.Property(e => e.QuarterStructure).HasMaxLength(20);

                entity.Property(e => e.ReleasedDate).HasColumnType("timestamp without time zone");

                entity.Property(e => e.UpdatedDate).HasColumnType("timestamp without time zone");
            });

          

            modelBuilder.Entity<SaleCalendarGenerate>(entity =>
            {
                entity.HasIndex(e => e.SaleCalendarId, "IX_SaleCalendarGenerates_SaleCalendarId");

                entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.Code).HasMaxLength(20);

                entity.Property(e => e.CreatedDate).HasColumnType("timestamp without time zone");

                entity.Property(e => e.DeletedDate).HasColumnType("timestamp without time zone");

                entity.Property(e => e.EndDate).HasColumnType("timestamp without time zone");

                entity.Property(e => e.StartDate).HasColumnType("timestamp without time zone");

                entity.Property(e => e.UpdatedDate).HasColumnType("timestamp without time zone");

                entity.HasOne(d => d.SaleCalendar)
                    .WithMany(p => p.SaleCalendarGenerates)
                    .HasForeignKey(d => d.SaleCalendarId);
            });

            modelBuilder.Entity<ODMappingOrderStatus>(entity =>
            {
                entity.ToTable("ODMappingOrderStatus");

                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.Property(e => e.CreatedBy).HasMaxLength(250);
                entity.Property(e => e.CreatedDate).HasColumnType("timestamp without time zone");
                entity.Property(e => e.ImportStatus).HasMaxLength(100);
                entity.Property(e => e.OneShopOrderStatus).HasMaxLength(50);
                entity.Property(e => e.OwnerCode).HasMaxLength(255);
                entity.Property(e => e.OwnerType).HasMaxLength(100);
                entity.Property(e => e.SaleOrderStatus).HasMaxLength(50);
                entity.Property(e => e.UpdatedBy).HasMaxLength(250);
                entity.Property(e => e.UpdatedDate).HasColumnType("timestamp without time zone");
            });

            modelBuilder.Entity<OsOrderInformation>(entity =>
            {
                entity.ToTable("OSOrderInformations");

                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.Property(e => e.CreatedBy).HasMaxLength(100);
                entity.Property(e => e.CreatedDate).HasColumnType("timestamp(6) without time zone");
                entity.Property(e => e.CusAddressCountry).HasMaxLength(255);
                entity.Property(e => e.CusAddressCountryId).HasMaxLength(100);
                entity.Property(e => e.CusAddressDistrict).HasMaxLength(255);
                entity.Property(e => e.CusAddressDistrictId).HasMaxLength(100);
                entity.Property(e => e.CusAddressProvince).HasMaxLength(255);
                entity.Property(e => e.CusAddressProvinceId).HasMaxLength(100);
                entity.Property(e => e.CusAddressStreetNo).HasMaxLength(100);
                entity.Property(e => e.CusAddressWard).HasMaxLength(255);
                entity.Property(e => e.CusAddressWardId).HasMaxLength(100);
                entity.Property(e => e.CustomerAddress).HasMaxLength(255);
                entity.Property(e => e.CustomerId).HasMaxLength(100);
                entity.Property(e => e.CustomerName).HasMaxLength(255);
                entity.Property(e => e.CustomerPhone).HasMaxLength(20);
                entity.Property(e => e.CustomerType).HasMaxLength(100);
                entity.Property(e => e.DeliveryAddressCountry).HasMaxLength(255);
                entity.Property(e => e.DeliveryAddressCountryId).HasMaxLength(100);
                entity.Property(e => e.DeliveryAddressDistrict).HasMaxLength(255);
                entity.Property(e => e.DeliveryAddressDistrictId).HasMaxLength(100);
                entity.Property(e => e.DeliveryAddressProvince).HasMaxLength(255);
                entity.Property(e => e.DeliveryAddressProvinceId).HasMaxLength(100);
                entity.Property(e => e.DeliveryAddressStreetNo).HasMaxLength(100);
                entity.Property(e => e.DeliveryAddressWard).HasMaxLength(255);
                entity.Property(e => e.DeliveryAddressWardId).HasMaxLength(100);
                entity.Property(e => e.DisBankAccount).HasMaxLength(100);
                entity.Property(e => e.DisBankAccountName).HasMaxLength(100);
                entity.Property(e => e.DisBankName).HasMaxLength(100);
                entity.Property(e => e.DiscountDescription).HasMaxLength(255);
                entity.Property(e => e.DiscountId)
                    .HasMaxLength(100)
                    .HasColumnName("DiscountID");
                entity.Property(e => e.DiscountType).HasMaxLength(100);
                entity.Property(e => e.DistributorCode).HasMaxLength(50);
                entity.Property(e => e.ExpectShippedDate).HasColumnType("timestamp(6) without time zone");
                entity.Property(e => e.ExternalOrdNbr)
                    .HasMaxLength(100)
                    .HasColumnName("External_OrdNBR");
                entity.Property(e => e.ImportStatus).HasMaxLength(10);
                entity.Property(e => e.MainCustomerId).HasMaxLength(100);
                entity.Property(e => e.OrderDate).HasColumnType("timestamp(6) without time zone");
                entity.Property(e => e.OrderDescription).HasMaxLength(255);
                entity.Property(e => e.OrderRefNumber).HasMaxLength(100);
                entity.Property(e => e.OrderType).HasMaxLength(50);
                entity.Property(e => e.OrigOrdAmt).HasColumnName("Orig_Ord_Amt");
                entity.Property(e => e.OrigOrdDiscAmt).HasColumnName("Orig_Ord_Disc_Amt");
                entity.Property(e => e.OrigOrdExtendAmt).HasColumnName("Orig_Ord_Extend_Amt");
                entity.Property(e => e.OrigOrdQty).HasColumnName("Orig_Ord_Qty");
                entity.Property(e => e.OrigOrdSkus).HasColumnName("Orig_Ord_SKUs");
                entity.Property(e => e.OrigOrdlineDiscAmt).HasColumnName("Orig_Ordline_Disc_Amt");
                entity.Property(e => e.OrigPromotionQty).HasColumnName("Orig_Promotion_Qty");
                entity.Property(e => e.PaymentBankNote).HasMaxLength(255);
                entity.Property(e => e.PaymentStatus).HasMaxLength(100);
                entity.Property(e => e.PaymentType).HasMaxLength(100);
                entity.Property(e => e.PaymentTypeDesc).HasMaxLength(100);
                entity.Property(e => e.PrincipalId)
                    .HasMaxLength(50)
                    .HasColumnName("PrincipalID");
                entity.Property(e => e.PromotionAmt).HasColumnName("Promotion_Amt");
                entity.Property(e => e.ReceiverName).HasMaxLength(100);
                entity.Property(e => e.ReceiverPhone).HasMaxLength(50);
                entity.Property(e => e.ShippedDate).HasColumnType("timestamp(6) without time zone");
                entity.Property(e => e.Source).HasMaxLength(20);
                entity.Property(e => e.Status).HasMaxLength(50);
                entity.Property(e => e.UpdatedBy).HasMaxLength(100);
                entity.Property(e => e.UpdatedDate).HasColumnType("timestamp(6) without time zone");
                entity.Property(e => e.SOStatus).HasMaxLength(100);
            });

            modelBuilder.Entity<OsOrderItem>(entity =>
            {
                entity.ToTable("OSOrderItems");

                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.Property(e => e.AllocateType).HasMaxLength(100);
                entity.Property(e => e.AllowChangeSku).HasColumnName("AllowChangeSKU");
                entity.Property(e => e.BaseUnitCode).HasMaxLength(100);
                entity.Property(e => e.BudgetBookOption).HasMaxLength(10);
                entity.Property(e => e.BudgetCode).HasMaxLength(100);
                entity.Property(e => e.BudgetType).HasMaxLength(100);
                entity.Property(e => e.CreatedBy).HasMaxLength(100);
                entity.Property(e => e.CreatedDate).HasColumnType("timestamp(6) without time zone");
                entity.Property(e => e.DisPriceVolumeCode).HasMaxLength(100);
                entity.Property(e => e.DiscountType).HasMaxLength(100);
                entity.Property(e => e.ExternalOrdNbr)
                    .HasMaxLength(100)
                    .HasColumnName("External_OrdNBR");
                entity.Property(e => e.InventoryAttibute1).HasMaxLength(100);
                entity.Property(e => e.InventoryAttibute10).HasMaxLength(100);
                entity.Property(e => e.InventoryAttibute2).HasMaxLength(100);
                entity.Property(e => e.InventoryAttibute3).HasMaxLength(100);
                entity.Property(e => e.InventoryAttibute4).HasMaxLength(100);
                entity.Property(e => e.InventoryAttibute5).HasMaxLength(100);
                entity.Property(e => e.InventoryAttibute6).HasMaxLength(100);
                entity.Property(e => e.InventoryAttibute7).HasMaxLength(100);
                entity.Property(e => e.InventoryAttibute8).HasMaxLength(100);
                entity.Property(e => e.InventoryAttibute9).HasMaxLength(100);
                entity.Property(e => e.ItemCode).HasMaxLength(100);
                entity.Property(e => e.ItemDescription).HasMaxLength(255);
                entity.Property(e => e.ItemGroupCode).HasMaxLength(100);
                entity.Property(e => e.ItemGroupDescription).HasMaxLength(255);
                entity.Property(e => e.ItemGroupName).HasMaxLength(100);
                entity.Property(e => e.ItemShortName).HasMaxLength(100);
                entity.Property(e => e.KitId).HasMaxLength(100);
                entity.Property(e => e.KitName).HasMaxLength(255);
                entity.Property(e => e.KitUomId).HasMaxLength(100);
                entity.Property(e => e.KitUomName).HasMaxLength(255);
                entity.Property(e => e.OrderRefNumber).HasMaxLength(100);
                entity.Property(e => e.OrigOrdLineAmt).HasColumnName("Orig_Ord_Line_Amt");
                entity.Property(e => e.OrigOrdLineDiscAmt).HasColumnName("Orig_Ord_line_Disc_Amt");
                entity.Property(e => e.OrigOrdLineExtendAmt).HasColumnName("Orig_Ord_Line_Extend_Amt");
                entity.Property(e => e.OwnerCode).HasMaxLength(255);
                entity.Property(e => e.OwnerType).HasMaxLength(100);
                entity.Property(e => e.PromotionCode).HasMaxLength(100);
                entity.Property(e => e.PromotionDescription).HasMaxLength(255);
                entity.Property(e => e.PromotionLevelCode).HasMaxLength(100);
                entity.Property(e => e.PromotionLevelDescription).HasMaxLength(255);
                entity.Property(e => e.PromotionOrderRule).HasMaxLength(100);
                entity.Property(e => e.PromotionOrderType).HasMaxLength(100);
                entity.Property(e => e.PromotionRuleofGiving).HasMaxLength(100);
                entity.Property(e => e.PromotionType).HasMaxLength(100);
                entity.Property(e => e.PurchaseUnitCode).HasMaxLength(100);
                entity.Property(e => e.RewardDescription).HasMaxLength(255);
                entity.Property(e => e.SaleUnitDescription).HasMaxLength(255);
                entity.Property(e => e.SalesUnitCode).HasMaxLength(100);
                entity.Property(e => e.Uom)
                    .HasMaxLength(100)
                    .HasColumnName("UOM");
                entity.Property(e => e.Uomdesc)
                    .HasMaxLength(100)
                    .HasColumnName("UOMDesc");
                entity.Property(e => e.UpdatedBy).HasMaxLength(100);
                entity.Property(e => e.UpdatedDate).HasColumnType("timestamp(6) without time zone");
                entity.Property(e => e.Vat).HasColumnName("VAT");
                entity.Property(e => e.Vatcode)
                    .HasMaxLength(100)
                    .HasColumnName("VATCode");
            });

            modelBuilder.Entity<SoorderRecallReq>(entity =>
            {
                entity.ToTable("SOOrderRecallReqs");

                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.Property(e => e.Code).HasMaxLength(50);
                entity.Property(e => e.CreatedBy).HasMaxLength(255);
                entity.Property(e => e.CreatedDate).HasColumnType("timestamp without time zone");
                entity.Property(e => e.ExternalCode).HasMaxLength(50);
                entity.Property(e => e.FileName).HasMaxLength(255);
                entity.Property(e => e.FilePath).HasMaxLength(255);
                entity.Property(e => e.GiveBackProductLevel).HasMaxLength(255);
                entity.Property(e => e.GiveBackProductType).HasMaxLength(50);
                entity.Property(e => e.OrderDateFrom).HasColumnType("timestamp without time zone");
                entity.Property(e => e.OrderDateTo).HasColumnType("timestamp without time zone");
                entity.Property(e => e.OwnerCode).HasMaxLength(255);
                entity.Property(e => e.OwnerType).HasMaxLength(100);
                entity.Property(e => e.Reason).HasMaxLength(255);
                entity.Property(e => e.RecallDateFrom).HasColumnType("timestamp without time zone");
                entity.Property(e => e.RecallDateTo).HasColumnType("timestamp without time zone");
                entity.Property(e => e.RecallProductCode).HasMaxLength(10);
                entity.Property(e => e.RecallProductDescription).HasMaxLength(255);
                entity.Property(e => e.RecallProductLevel).HasMaxLength(255);
                entity.Property(e => e.RecallProductType).HasMaxLength(50);
                entity.Property(e => e.SaleOrgCode).HasMaxLength(10);
                entity.Property(e => e.SaleTerritoryLevel).HasMaxLength(10);
                entity.Property(e => e.ScopeType).HasMaxLength(50);
                entity.Property(e => e.Status).HasMaxLength(50);
                entity.Property(e => e.TerritoryStructureCode).HasMaxLength(10);
                entity.Property(e => e.UpdatedBy).HasMaxLength(255);
                entity.Property(e => e.UpdatedDate).HasColumnType("timestamp without time zone");
            });

            modelBuilder.Entity<SoorderRecallReqGiveBack>(entity =>
            {
                entity.ToTable("SOOrderRecallReqGiveBacks");

                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.Property(e => e.CreatedBy).HasMaxLength(255);
                entity.Property(e => e.CreatedDate).HasColumnType("timestamp without time zone");
                entity.Property(e => e.ItemAttributeCode).HasMaxLength(10);
                entity.Property(e => e.ItemAttributeDescription).HasMaxLength(80);
                entity.Property(e => e.ItemCode).HasMaxLength(10);
                entity.Property(e => e.ItemDescription).HasMaxLength(80);
                entity.Property(e => e.ItemGroupCode).HasMaxLength(16);
                entity.Property(e => e.ItemGroupDescription).HasMaxLength(80);
                entity.Property(e => e.OwnerCode).HasMaxLength(255);
                entity.Property(e => e.OwnerType).HasMaxLength(100);
                entity.Property(e => e.RecallReqCode).HasMaxLength(50);
                entity.Property(e => e.Uom).HasMaxLength(10);
                entity.Property(e => e.UpdatedBy).HasMaxLength(255);
                entity.Property(e => e.UpdatedDate).HasColumnType("timestamp without time zone");
            });

            modelBuilder.Entity<SoorderRecallReqOrder>(entity =>
            {
                entity.ToTable("SOOrderRecallReqOrders");

                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.Property(e => e.CreatedBy).HasMaxLength(255);
                entity.Property(e => e.CreatedDate).HasColumnType("timestamp without time zone");
                entity.Property(e => e.CustomerCode).HasMaxLength(100);
                entity.Property(e => e.CustomerName).HasMaxLength(255);
                entity.Property(e => e.CustomerShiptoCode).HasMaxLength(100);
                entity.Property(e => e.CustomerShiptoName).HasMaxLength(100);
                entity.Property(e => e.DistributorCode).HasMaxLength(10);
                entity.Property(e => e.ItemCode).HasMaxLength(100);
                entity.Property(e => e.ItemDescription).HasMaxLength(80);
                entity.Property(e => e.LocationId).HasMaxLength(50);
                entity.Property(e => e.OrderCode).HasMaxLength(50);
                entity.Property(e => e.OrderDate).HasColumnType("timestamp without time zone");
                entity.Property(e => e.OwnerCode).HasMaxLength(255);
                entity.Property(e => e.OwnerType).HasMaxLength(100);
                entity.Property(e => e.RecallCode).HasMaxLength(50);
                entity.Property(e => e.RecallReqCode).HasMaxLength(50);
                entity.Property(e => e.SalesRepEmpName).HasMaxLength(255);
                entity.Property(e => e.SalesRepId).HasMaxLength(100);
                entity.Property(e => e.Status).HasMaxLength(100);
                entity.Property(e => e.Uom).HasMaxLength(100);
                entity.Property(e => e.UpdatedBy).HasMaxLength(255);
                entity.Property(e => e.UpdatedDate).HasColumnType("timestamp without time zone");
                entity.Property(e => e.WarehouseId).HasMaxLength(50);
            });

            modelBuilder.Entity<SoorderRecallReqScope>(entity =>
            {
                entity.ToTable("SOOrderRecallReqScopes");

                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.Property(e => e.Code).HasMaxLength(10);
                entity.Property(e => e.CreatedBy).HasMaxLength(255);
                entity.Property(e => e.CreatedDate).HasColumnType("timestamp without time zone");
                entity.Property(e => e.Description).HasMaxLength(255);
                entity.Property(e => e.OwnerCode).HasMaxLength(255);
                entity.Property(e => e.OwnerType).HasMaxLength(100);
                entity.Property(e => e.RecallReqCode).HasMaxLength(50);
                entity.Property(e => e.UpdatedBy).HasMaxLength(255);
                entity.Property(e => e.UpdatedDate).HasColumnType("timestamp without time zone");
            });

            modelBuilder.Entity<InvoiceOrder>(entity =>
            {
                entity.ToTable("SoInvoiceOrders");
                entity.Property(e => e.Id).ValueGeneratedNever();
            });

            modelBuilder.Entity<SoorderRecall>(entity =>
            {
                entity.ToTable("SOOrderRecalls");

                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.Property(e => e.Code).HasMaxLength(50);
                entity.Property(e => e.CreatedBy).HasMaxLength(255);
                entity.Property(e => e.CreatedDate).HasColumnType("timestamp without time zone");
                entity.Property(e => e.Description).HasMaxLength(255);
                entity.Property(e => e.DistributorShiptoCode).HasMaxLength(50);
                entity.Property(e => e.GiveBackLocationCode).HasMaxLength(50);
                entity.Property(e => e.RecallLocationCode).HasMaxLength(50);
                entity.Property(e => e.OwnerCode).HasMaxLength(255);
                entity.Property(e => e.OwnerType).HasMaxLength(100);
                entity.Property(e => e.RecallType).HasMaxLength(50);
                entity.Property(e => e.RequestRecallCode).HasMaxLength(50);
                entity.Property(e => e.RequestRecallReason).HasMaxLength(255);
                entity.Property(e => e.Status).HasMaxLength(50);
                entity.Property(e => e.UpdatedBy).HasMaxLength(255);
                entity.Property(e => e.UpdatedDate).HasColumnType("timestamp without time zone");
            });

            modelBuilder.Entity<SoorderRecallOrder>(entity =>
            {
                entity.ToTable("SOOrderRecallOrders");

                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.Property(e => e.CreatedBy).HasMaxLength(255);
                entity.Property(e => e.CreatedDate).HasColumnType("timestamp without time zone");
                entity.Property(e => e.CustomerCode).HasMaxLength(100);
                entity.Property(e => e.CustomerName).HasMaxLength(255);
                entity.Property(e => e.CustomerShiptoCode).HasMaxLength(100);
                entity.Property(e => e.CustomerShiptoName).HasMaxLength(100);
                entity.Property(e => e.DistributorCode).HasMaxLength(10);
                entity.Property(e => e.GiveBackUom).HasMaxLength(100);
                entity.Property(e => e.ItemCode).HasMaxLength(100);
                entity.Property(e => e.ItemDescription).HasMaxLength(80);
                entity.Property(e => e.ItemGiveBackCode).HasMaxLength(100);
                entity.Property(e => e.ItemGiveBackDesc).HasMaxLength(80);
                entity.Property(e => e.OrderCode).HasMaxLength(50);
                entity.Property(e => e.OrderDate).HasColumnType("timestamp without time zone");
                entity.Property(e => e.OwnerCode).HasMaxLength(255);
                entity.Property(e => e.OwnerType).HasMaxLength(100);
                entity.Property(e => e.RecallCode).HasMaxLength(50);
                entity.Property(e => e.Uom).HasMaxLength(100);
                entity.Property(e => e.UpdatedBy).HasMaxLength(255);
                entity.Property(e => e.UpdatedDate).HasColumnType("timestamp without time zone");
            });

            modelBuilder.Entity<SystemSetting>(entity =>
            {
                entity.HasIndex(e => e.SettingType, "Idx_SystemSettings_01");

                entity.HasIndex(e => e.SettingKey, "Idx_SystemSettings_02");

                entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
                entity.Property(e => e.CreatedDate)
                    .HasDefaultValueSql("'0001-01-01 00:00:00'::timestamp without time zone")
                    .HasColumnType("timestamp(6) without time zone");
                entity.Property(e => e.DeletedDate).HasColumnType("timestamp(6) without time zone");
                entity.Property(e => e.Description).HasDefaultValueSql("''::text");
                entity.Property(e => e.SettingKey).HasDefaultValueSql("''::text");
                entity.Property(e => e.SettingType).HasDefaultValueSql("''::text");
                entity.Property(e => e.UpdatedDate).HasColumnType("timestamp(6) without time zone");
            });

            modelBuilder.Entity<OsorderStatusHistory>(entity =>
            {
                entity.ToTable("OSOrderStatusHistories");

                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.Property(e => e.CreatedBy).HasMaxLength(100);
                entity.Property(e => e.CreatedDate).HasColumnType("timestamp(6) without time zone");
                entity.Property(e => e.DistributorCode).HasMaxLength(100);
                entity.Property(e => e.ExternalOrdNbr)
                    .HasMaxLength(100)
                    .HasColumnName("External_OrdNBR");
                entity.Property(e => e.OneShopStatus).HasMaxLength(100);
                entity.Property(e => e.OneShopStatusName).HasMaxLength(255);
                entity.Property(e => e.OrderDate).HasColumnType("timestamp(6) without time zone");
                entity.Property(e => e.OrderRefNumber).HasMaxLength(100);
                entity.Property(e => e.OutletCode).HasMaxLength(100);
                entity.Property(e => e.Sostatus)
                    .HasMaxLength(100)
                    .HasColumnName("SOStatus");
                entity.Property(e => e.SOStatusName).HasMaxLength(255);
                entity.Property(e => e.UpdatedBy).HasMaxLength(100);
                entity.Property(e => e.UpdatedDate).HasColumnType("timestamp(6) without time zone");
            });

            modelBuilder.Entity<UserLogin>(entity =>
            {
                entity.HasKey(e => new { e.LoginProvider, e.ProviderKey });

                entity.HasIndex(e => e.UserId, "IX_UserLogins_UserId");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.UserLogins)
                    .HasForeignKey(d => d.UserId);
            });

            modelBuilder.Entity<UserLoginLog>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Agent).HasMaxLength(250);

                entity.Property(e => e.Ip)
                    .HasMaxLength(30)
                    .HasColumnName("IP");

                entity.Property(e => e.Message).HasMaxLength(250);

                entity.Property(e => e.UserName).HasMaxLength(256);
            });

            modelBuilder.Entity<UserPolicy>(entity =>
            {
                entity.ToTable("UserPolicy");

                entity.HasIndex(e => e.PolicyId, "IX_UserPolicy_PolicyId");

                entity.HasIndex(e => e.UserId, "IX_UserPolicy_UserId");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.HasOne(d => d.Policy)
                    .WithMany(p => p.UserPolicies)
                    .HasForeignKey(d => d.PolicyId);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.UserPolicies)
                    .HasForeignKey(d => d.UserId);
            });

            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.RoleId });

                entity.HasIndex(e => e.RoleId, "IX_UserRoles_RoleId");

                entity.HasOne(d => d.Role)
                    .WithMany(p => p.UserRoles)
                    .HasForeignKey(d => d.RoleId);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.UserRoles)
                    .HasForeignKey(d => d.UserId);
            });

            modelBuilder.Entity<UserToken>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name });

                entity.HasOne(d => d.User)
                    .WithMany(p => p.UserTokens)
                    .HasForeignKey(d => d.UserId);
            });

            modelBuilder.Entity<UserType>(entity =>
            {
                entity.Property(e => e.Name).HasMaxLength(36);
            });

            modelBuilder.Entity<OrderResultModel>().HasNoKey();

            modelBuilder.Entity<SaleVolumnReportModel>().HasNoKey();

            modelBuilder.Entity<ProductivityReportModel>().HasNoKey();

            modelBuilder.Entity<CommonSoOrderModel>().HasNoKey();

            modelBuilder.Entity<ProductivityByDayReportModel>().HasNoKey();

            modelBuilder.Entity<DisProductivityBySalesReportModel>().HasNoKey();

            modelBuilder.Entity<HoProductivityBySalesReportModel>().HasNoKey();

            modelBuilder.Entity<FnProductivityReportModel>().HasNoKey();
            modelBuilder.Entity<FnProductivityReportWithRouteZoneModel>().HasNoKey();
            modelBuilder.Entity<FnProductivityByDayReportModel>().HasNoKey();
            modelBuilder.Entity<FnProductivityByDayReportWithRouteZoneModel>().HasNoKey();
            modelBuilder.Entity<FnSaslesDetailReportModel>().HasNoKey();
            modelBuilder.Entity<FnReportShippingStatusModel>().HasNoKey();
            modelBuilder.Entity<FnSalesSynthesisReportModel>().HasNoKey();
            modelBuilder.Entity<FnDsaProductivityByProductReportModel>().HasNoKey();
            modelBuilder.Entity<FnRouteZoneProductivityByProductReportModel>().HasNoKey();
            modelBuilder.Entity<FnReportTrackingOrderModel>().HasNoKey();

            //Khoa enhanced 
            modelBuilder.Entity<Models.Common.EmployeeModel>().HasNoKey();
            modelBuilder.Entity<Models.Common.EmployeeV2Model>().HasNoKey();
            modelBuilder.Entity<DistributorRouteZoneModel>().HasNoKey();
            modelBuilder.Entity<DisRouteZoneBasicModel>().HasNoKey();
            modelBuilder.Entity<DistributorCustomerModel>().HasNoKey();
            modelBuilder.Entity<DistributorCustomerWithPagingModel>().HasNoKey();
            modelBuilder.Entity<DistributorCustomerShiptoModel>().HasNoKey();
            modelBuilder.Entity<DisCusShiptoDetailModel>().HasNoKey();
            modelBuilder.Entity<DistributorCommonInfoModel>().HasNoKey();
            modelBuilder.Entity<DistributorBasicInfoModel>().HasNoKey();

            modelBuilder.Entity<FfadsSoLot>(entity =>
            {
                entity.ToTable("FFADsSoLots");

                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.Property(e => e.AllocateType).HasMaxLength(100);
                entity.Property(e => e.CreatedBy).HasMaxLength(100);
                entity.Property(e => e.CreatedDate).HasColumnType("timestamp(6) without time zone");
                entity.Property(e => e.ExpiredDate).HasColumnType("timestamp(6) without time zone");
                entity.Property(e => e.External_OrdNBR)
                    .HasMaxLength(100)
                    .HasColumnName("External_OrdNBR");
                entity.Property(e => e.IssueUom).HasMaxLength(100);
                entity.Property(e => e.IssueUomDesc).HasMaxLength(100);
                entity.Property(e => e.ItemCode).HasMaxLength(100);
                entity.Property(e => e.ItemDescription).HasMaxLength(255);
                entity.Property(e => e.ItemGroupDescription).HasMaxLength(255);
                entity.Property(e => e.ItemGroupId).HasMaxLength(100);
                entity.Property(e => e.LotNum).HasMaxLength(100);
                entity.Property(e => e.OrderRefNumber).HasMaxLength(100);
                entity.Property(e => e.SerialNum).HasMaxLength(100);
                entity.Property(e => e.Uom).HasMaxLength(100);
                entity.Property(e => e.UomDesc).HasMaxLength(100);
                entity.Property(e => e.UpdatedBy).HasMaxLength(100);
                entity.Property(e => e.UpdatedDate).HasColumnType("timestamp(6) without time zone");
                entity.Property(e => e.VisitId).HasMaxLength(100);
            });

            modelBuilder.Entity<FfadsSoPayment>(entity =>
            {
                entity.ToTable("FFADsSoPayments");

                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.Property(e => e.CreatedBy).HasMaxLength(100);
                entity.Property(e => e.CreatedDate).HasColumnType("timestamp(6) without time zone");
                entity.Property(e => e.CustomerAddress).HasMaxLength(255);
                entity.Property(e => e.CustomerId).HasMaxLength(100);
                entity.Property(e => e.CustomerName).HasMaxLength(255);
                entity.Property(e => e.CustomerPhone).HasMaxLength(50);
                entity.Property(e => e.CustomerShiptoId).HasMaxLength(100);
                entity.Property(e => e.CustomerShiptoName).HasMaxLength(255);
                entity.Property(e => e.External_OrdNBR)
                    .HasMaxLength(100)
                    .HasColumnName("External_OrdNBR");
                entity.Property(e => e.OrderRefNumber).HasMaxLength(100);
                entity.Property(e => e.OrderType).HasMaxLength(100);
                entity.Property(e => e.Orig_Ord_Extend_Amt).HasColumnName("Orig_Ord_Extend_Amt");
                entity.Property(e => e.PaymentType).HasMaxLength(100);
                entity.Property(e => e.UpdatedBy).HasMaxLength(100);
                entity.Property(e => e.UpdatedDate).HasColumnType("timestamp(6) without time zone");
                entity.Property(e => e.VisitId).HasMaxLength(100);
            });


            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

        public override int SaveChanges()
        {
            var auditEntries = ChangeTracker.Entries<AuditTable>().ToList();
            foreach (var entry in auditEntries)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedDate = DateTime.Now;
                        break;

                    case EntityState.Modified:
                        entry.Entity.UpdatedDate = DateTime.Now;
                        break;
                }
            }

            try
            {
                return base.SaveChanges();
            }
            catch (Exception ex)
            {
                // Log the exception here if needed
                throw; // Re-throw the exception to handle it in the calling code
            }
        }

    }
}
