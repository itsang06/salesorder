namespace SysAdmin.Models.StaticValue
{
    public static class RedisCacheKey
    {
        #region User
        public const string AllUserInfo = "AllUserInfo";
        public const string UserInfo = "UserInfo_";
        public const string UserTypes = "UserTypes";
        #endregion
        #region User Login Log
        public const string UserLoginLogInfo = "UserLoginLogInfo";
        #endregion
        #region System Log
        public const string SystemLogInfo = "SystemLogInfo";

        #endregion
        #region Menu
        public const string MenuInfo = "MenuInfo";
        public const string LeftMenuInfo = "LeftMenuInfo";
        public const string ParentMenuInfo = "ParentMenuInfo";
        #endregion
        #region SystemData
        public const string EmailTypeInfo = "EmailTypeInfo";
        public const string PhoneTypeInfo = "ParentMenuInfo";
        public const string SystemSettingsInfor = "SystemSettingsInfor";
        #endregion
        #region Feature
        public const string FeatureInfo = "FeatureInfo";
        public const string BaseFeatureInfo = "BaseFeatureInfo";
        public const string GetFeaturesList = "GetFeaturesList";
        public const string GetFeaturesUsedByPrincipal = "GetFeaturesUsedByPrincipal";
        #endregion
        #region Action
        public const string ActionInfo = "ActionInfo";
        public const string BaseActionInfo = "BaseActionInfo";
        #endregion
        #region Role
        public const string RoleInfo = "RoleInfo";
        #endregion
        #region Principal
        public const string PrincipalInfo = "PrincipalInfo";
        #endregion
        #region Package
        public const string PackageInfo = "PackageInfo";
        #endregion
        #region AuditLog
        public const string AuditLogInfo = "AuditLogInfo";
        #endregion

        #region ItemData
        public const string ItemSettingInfo = "ItemSettingInfo";
        public const string ItemAttributeInfo = "ItemAttributeInfo";
        public const string ItemHierarchyMappingInfo = "ItemHierarchyMappingInfo";
        public const string CompetitorInfo = "CompetitorInfo";
        public const string UomInfor = "UomInfor";
        public const string VatInfor = "VatInfor";
        public const string ManufactureInfor = "ManufactureInfor";
        public const string InventoryItemInfo = "InventoryItemInfo";
        public const string ItemGroupInfo = "ItemGroupInfo";
        public const string KitInfor = "KitInfor";

        #endregion

        #region Geo
        public const string CountryInfor = "CountryInfor";
        public const string StateInfor = "StateInfor";
        public const string ProvinceInfor = "ProvinceInfor";
        public const string RegionInfor = "RegionInfor";
        public const string CityInfor = "CityInfor";
        public const string DistrictInfor = "DistrictInfor";
        public const string WardInfor = "WardInfor";

        public const string GeoMappingInfor = "GeoMappingInfor";
        public const string GeoMasterInfor = "GeoMasterInfor";
        public const string GeoStructureInfor = "GeoStructureInfor";
        #endregion
    }
}
