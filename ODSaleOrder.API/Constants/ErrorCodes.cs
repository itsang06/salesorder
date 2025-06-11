namespace SysAdmin.API.Constants
{
    public static class ErrorCodes
    {
        public static class General
        {
            public const string IsRequired = "{0}_IsRequired";
            public const string Exception = "GEN_SomethingWrong";
            public const string InvalidId = "GEN_InvalidId";
            public const string InvalidParam = "GEN_InvalidParam";
            public const string Server = "GEN_Server";
            public const string CannotGetData = "GEN_DataNotFound";
            public const string CreateFailed = "GEN_CreateFailed";
            public const string UpdateFailed = "GEN_UpdateFailed";
            public const string DoNotHavePermission = "GEN_DoNotHavePermission";
            public const string EmailConfigNotFound = "GEN_EmailConfigNotFound";
            public const string NotFound = "GEN_DataNotFound";
            public const string DuplicateCode = "DuplicateCode";
            public const string CannotDelete = "GEN_CannotDelete";
        }

        public static class ScheduleJob
        {
            public const string CronExpressInvalid = "SCHE_CronInvalid";
            public const string CronExpressEmpty = "SCHE_CronEmpty";
            public const string NotFound = "SCHE_NotFound";
            public const string NotFoundInScheduler = "SCHE_NotFoundInScheduler";
            public const string AlreadyStarted = "SCHE_AlreadyStarted";
            public const string Existed = "SCHE_Existed";
            public const string AddSchedulerFail = "SCHE_AddSchedulerFail";
        }

        public static class Container
        {
            public const string Existed = "CON_Existed";
        }

        public static class SystemSetting
        {
            public const string ExistedSettingKey = "SYSTEMSETTING_ExistedSettingKey";
        }
        public static class CleanDataConfigure
        {
            public const string ExistedJobSchedulerIdAndTableName = "SYSTEMSETTING_ExistedJobSchedulerAndTableName";
        }

        public static class DynamicFieldConfigure
        {
            public const string ExistedFeatureId = "DYNAMICFIELD_ExistedFeatureId";
            public const string FeatureIdOutOfScope = "DYNAMICFIELD_FeatureIdOutOfScope";
        }

        public static class DashBoard
        {
            public const string CPUAndRamExceeded = "DASHBOARD_CPU_Ecxeeded_{0}_RAM_Exceeded_{1}";
            public const string CPUAndRamExceededMetric = "DASHBOARD_CPU_Ecxeeded_{0}/{1}_RAM_Exceeded_{2}/{3}";
            public const string CPUExceeded = "DASHBOARD_CPU_Ecxeeded_{0}";
            public const string CPUExceededMetric = "DASHBOARD_CPU_Ecxeeded_{0}/{1}";
            public const string RamExceeded = "DASHBOARD_RAM_Exceeded_{0}";
            public const string RamExceededMetric = "DASHBOARD_RAM_Exceeded_{0}/{1}";
        }

        public static class Principal
        {
            public const string NotFound = "PRINCIPAL_NotFound";
        }

        public static class MobileUser
        {
            public const string NotFound = "MOBILEUSER_NotFound";
        }
    }
}
