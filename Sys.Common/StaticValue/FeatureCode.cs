namespace SysAdmin.Models.StaticValue
{
    public static class FeatureCode
    {
        public const string PrincipalPage = "PrincipalForm";
        public const string FeaturePage = "FeatureForm";
        public const string ActionPage = "ActionForm";
        public const string ConfigurePage = "ConfigForm";
        public const string LogConfigPage = "LogConfigForm";
        public const string UserLoginLogPage = "UserLoginLogForm";
        public const string SystemLogPage = "SystemLogForm";
        public const string ReportConfigPage = "ReportConfigForm";
        public const string DashboardPage = "DashboardForm";
        public const string PagingConfigPage = "PagingForm";
        public const string MenuConfigPage = "MenuForm";
        public const string AuditLogPage = "AuditLogForm";
        public const string AuditLogConfigPage = "AuditLogConfigForm";
        public const string PackageConfigPage = "PackageForm";
        public const string UserPage = "UserForm";
        public const string AssignPermissionPage = "AssignPermissionForm";
        public const string RolePage = "RoleForm";
        public const string ProfilePage = "ProfileForm";
        public const string TranslationPage = "TranslationForm";
        public const string ReportPage = "ReportForm";
        public const string MobileUserPage = "MobileUserForm";
        public const string JobPage = "JobManagementForm";

        public const string EmailTypePage = "EmailTypeForm";
        public const string PhoneTypePage = "PhoneTypeForm";
        public const string ContractTypePage = "ContractTypeForm";
        public const string RamReportPage = "RAMReportForm";
        public const string CPUReportPage = "CPUReportForm";
        public const string SeverHealthReportPage = "SeverHealthReportForm";
        public const string DynamicFieldPage = "DynamicFieldsForm";
        public const string SystemSettingsForm = "SystemSettingsForm";
        public const string ApplicationPage = "ApplicationForm";
        //Item
        public const string ItemSettingPage = "ItemSettingForm";
        public const string ItemAttributePage = "ItemAttributeForm";
        public const string ItemHierarchyMappingPage = "ItemHierarchyMappingForm";
        public const string CompetitorPage = "CompetitorForm";
        public const string UomPage = "UomForm";
        public const string VatPage = "VatForm";
        public const string ManufacturePage = "ManufactureForm";
        public const string StockItemPage = "StockItemForm";
        public const string ItemGroupPage = "ItemGroupForm";
        public const string KitPage = "KitForm";

        //GEO
        public const string CountryPage = "CountryForm";
        public const string StatePage = "StateForm";
        public const string RegionPage = "RegionForm";
        public const string ProvincePage = "ProvinceForm";
        public const string CityPage = "CityForm";
        public const string DistrictPage = "DistrictForm";
        public const string WardPage = "WardForm";

        public const string GeographicalStructurePage = "GeographicalStructureForm";
        public const string GeographicalMappingPage = "GeographicalMappingForm";
    }
    public static class ActionCode
    {
        public const string Create = "Create";
        public const string Update = "Update";
        public const string Delete = "Delete";
        public const string View = "View";
        public const string SensitiveData = "CanChangeSensitiveData";
        public const string ViewAction = "ViewAction";
        public const string CreateAction = "CreateAction";
        public const string AddUserToRole = "AddUserToRole";
        public const string AddRoletoUser = "AddRoletoUser";
    }

    public static class FieldFeature
    {
        public const int Menu = 1;
        public const int Package = 2;
        public const int Role = 3;
        public const int Feature = 4;
        public const int User = 5;
        public const int Principal = 6;
    }
}
