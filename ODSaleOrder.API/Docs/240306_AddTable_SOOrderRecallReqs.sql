DROP TABLE IF EXISTS "public"."SOOrderRecallReqs";
CREATE TABLE "public"."SOOrderRecallReqs" (
    "Id" uuid NOT NULL,
    "Code" character varying(50) NOT NULL,
    "ExternalCode" character varying(50),
    "Reason" character varying(255),
    "OrderDateFrom" timestamp NULL,
    "OrderDateTo" timestamp NULL,
    "RecallDateFrom" timestamp NULL,
    "RecallDateTo" timestamp NULL,
    "FilePath" character varying(255),
    "FileName" character varying(255),
    "Status" character varying(50),
    "RecallProductType" character varying(50) ,
    "RecallProductLevel" character varying(255),
    "RecallProductCode" character varying(10),
    "RecallProductDescription" character varying(255),
    "GiveBackProductType" character varying(50),
    "GiveBackProductLevel" character varying(255),
    "SameRecallItem" boolean NOT NULL,
    "SaleOrgCode" character varying(10),
    "TerritoryStructureCode" character varying(10),
    "ScopeType" character varying(50) ,
    "SaleTerritoryLevel" character varying(10),
    "CreatedDate" timestamp NULL,
    "UpdatedDate" timestamp NULL,
    "CreatedBy" character varying(255) NULL,
    "UpdatedBy" character varying(255) NULL,
    "IsDeleted" boolean NOT NULL,
    "OwnerType" character varying(100) NULL,
    "OwnerCode" character varying(255) NULL,
    "IsSync" boolean NOT NULL,
    CONSTRAINT "PK_SOOrderRecallReqs" PRIMARY KEY ("Id")
);

DROP TABLE IF EXISTS "public"."SOOrderRecallReqScopes";
CREATE TABLE "public"."SOOrderRecallReqScopes" (
    "Id" uuid NOT NULL,
    "RecallReqCode" character varying(50),
    "Code" character varying(10),
    "Description" character varying(255),
    "CreatedDate" timestamp NULL,
    "UpdatedDate" timestamp NULL,
    "CreatedBy" character varying(255) NULL,
    "UpdatedBy" character varying(255) NULL,
    "IsDeleted" boolean NOT NULL,
    "OwnerType" character varying(100) NULL,
    "OwnerCode" character varying(255) NULL,
    CONSTRAINT "PK_SOOrderRecallReqScopes" PRIMARY KEY ("Id")
);

DROP TABLE IF EXISTS "public"."SOOrderRecallReqGiveBacks";
CREATE TABLE "public"."SOOrderRecallReqGiveBacks" (
    "Id" uuid NOT NULL,
    "RecallReqCode" character varying(50),
    "ItemCode" character varying(10),
    "ItemDescription" character varying(80),
    "ItemGroupCode" character varying(16),
    "ItemGroupDescription" character varying(80),
    "ItemAttributeCode" character varying(10),
    "ItemAttributeDescription" character varying(80),
    "Uom" character varying(10),
    "Quantity" int,
    "IsDefault" boolean NOT NULL,
    "CreatedDate" timestamp NULL,
    "UpdatedDate" timestamp NULL,
    "CreatedBy" character varying(255) NULL,
    "UpdatedBy" character varying(255) NULL,
    "IsDeleted" boolean NOT NULL,
    "OwnerType" character varying(100) NULL,
    "OwnerCode" character varying(255) NULL,
    CONSTRAINT "PK_SOOrderRecallReqGiveBacks" PRIMARY KEY ("Id")
);

DROP TABLE IF EXISTS "public"."SOOrderRecallReqOrders";
CREATE TABLE "public"."SOOrderRecallReqOrders" (
    "Id" uuid NOT NULL,
    "RecallReqCode" character varying(50),
    "DistributorCode" character varying(10),
    "WarehouseId" character varying(50),
    "LocationId" character varying(50),
    "CustomerCode" character varying(100),
    "CustomerName" character varying(255),
    "CustomerShiptoCode" character varying(100),
    "CustomerShiptoName" character varying(100),
    "SalesRepId" character varying(100),
    "SalesRepEmpName" character varying(255),
    "Status" character varying(100),
    "OrderCode" character varying(50),
    "ItemCode" character varying(100),
    "ItemDescription" character varying(80),
    "Uom" character varying(100),
    "OrderQuantity" int,
    "OrderBaseQuantity" int,
    "IsRecall" boolean NOT NULL,
    "RecallCode" character varying(50),
    "CreatedDate" timestamp NULL,
    "UpdatedDate" timestamp NULL,
    "CreatedBy" character varying(255) NULL,
    "UpdatedBy" character varying(255) NULL,
    "IsDeleted" boolean NOT NULL,
    "OwnerType" character varying(100) NULL,
    "OwnerCode" character varying(255) NULL,
    CONSTRAINT "PK_SOOrderRecallReqOrders" PRIMARY KEY ("Id")
);

-- Insert SystemSetting
INSERT INTO "SystemSettings" ("Id", "SettingType", "SettingKey", "SettingValue", "Description", "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate", "DeletedBy", "DeletedDate", "IsActive", "PrincipalDescription", "IsDefault") VALUES
(uuid_generate_v4(),	'SORECALLTYPE',	'SKU',	'Sku',	'Sku',	'Admin',	'2024-03-06 11:25:15.11563',	NULL,	NULL,	NULL,	NULL,	't',	NULL,	't'),
(uuid_generate_v4(),	'SORECALLTYPE',	'ITEMATTRIBUTE',	'Item Attribute',	'Item Attribute',	'Admin',	'2024-03-06 11:25:15.11563',	NULL,	NULL,	NULL,	NULL,	't',	NULL,	't'),
(uuid_generate_v4(),	'SORECALLTYPE',	'ITEMGROUP',	'Item Group',	'Item Group',	'Admin',	'2024-03-06 11:25:15.11563',	NULL,	NULL,	NULL,	NULL,	't',	NULL,	't'),
(uuid_generate_v4(),	'SORECALLSCOPETYPE',	'DISTRIBUTOR',	'Distributor',	'Distributor',	'Admin',	'2024-03-06 11:25:15.11563',	NULL,	NULL,	NULL,	NULL,	't',	NULL,	't'),
(uuid_generate_v4(),	'SORECALLSCOPETYPE',	'SALEAREA',	'Sell area',	'Sell area',	'Admin',	'2024-03-06 11:25:15.11563',	NULL,	NULL,	NULL,	NULL,	't',	NULL,	't'),
(uuid_generate_v4(),	'SORECALLSTATUS',	'NEW',	'New',	'New',	'Admin',	'2024-03-06 11:25:15.11563',	NULL,	NULL,	NULL,	NULL,	't',	NULL,	't'),
(uuid_generate_v4(),	'SORECALLSTATUS',	'RELEASED',	'Released',	'Released',	'Admin',	'2024-03-06 11:25:15.11563',	NULL,	NULL,	NULL,	NULL,	't',	NULL,	't'),
(uuid_generate_v4(),	'SORECALLFROMTYPE',	'DISTRIBUTOR',	'From Distributor',	'From Distributor',	'Admin',	'2024-03-12 11:25:15.11563',	NULL,	NULL,	NULL,	NULL,	't',	NULL,	't'),
(uuid_generate_v4(),	'SORECALLFROMTYPE',	'PRINCIPAL',	'From Principal',	'From Principal',	'Admin',	'2024-03-12 11:25:15.11563',	NULL,	NULL,	NULL,	NULL,	't',	NULL,	't'),
(uuid_generate_v4(),	'INVTYPE',	'INV20',	'SO_RECALL',	'Order recall',	'Admin',	'2024-03-12 15:16:57.018325',	NULL,	NULL,	NULL,	NULL,	't',	'string',	'f'),
(uuid_generate_v4(),	'INVTYPE',	'INV21',	'SO_GIVEBACK',	'Order give back',	'Admin',	'2024-03-12 15:16:57.018325',	NULL,	NULL,	NULL,	NULL,	't',	'string',	'f');