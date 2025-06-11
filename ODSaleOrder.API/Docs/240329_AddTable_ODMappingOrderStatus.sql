CREATE TABLE "public"."ODMappingOrderStatus" (
  "Id" uuid,
  "SaleOrderStatus" character varying(50) NULL,
  "OneShopOrderStatus" character varying(50) NULL,
  "IsDeleted" boolean,
  "CreatedBy" character varying(250) NULL,
  "CreatedDate" timestamp,
  "UpdatedBy" character varying(250) NULL,
  "UpdatedDate" timestamp,
  "OwnerType" character varying(100) NULL,
  "OwnerCode" character varying(255) NULL,
  CONSTRAINT "PK_ODMappingOrderStatus" PRIMARY KEY ("Id")
);

INSERT INTO "ODMappingOrderStatus" ("Id", "SaleOrderStatus", "OneShopOrderStatus", "IsDeleted", "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate", "OwnerType", "OwnerCode") VALUES
(uuid_generate_v4(),	'OS_SO_00',	'OS_ST_00',	false, 'admin', '2024-03-29 00:00:00.022985', NULL, NULL, 'SYSTEM',	NULL),
(uuid_generate_v4(),	'OS_SO_01',	NULL,	false, 'admin', '2024-03-29 00:00:00.022985', NULL, NULL, 'SYSTEM',	NULL),
(uuid_generate_v4(),	'OS_SO_02',	'OS_ST_08',	false, 'admin', '2024-03-29 00:00:00.022985', NULL, NULL, 'SYSTEM',	NULL),
(uuid_generate_v4(),	'OS_SO_03',	'OS_ST_08',	false, 'admin', '2024-03-29 00:00:00.022985', NULL, NULL, 'SYSTEM',	NULL),
(uuid_generate_v4(),	'OS_SO_04',	'OS_ST_08',	false, 'admin', '2024-03-29 00:00:00.022985', NULL, NULL, 'SYSTEM',	NULL),
(uuid_generate_v4(),	'SO_ST_CANCEL',	'OS_ST_06',	false, 'admin', '2024-03-29 00:00:00.022985', NULL, NULL, 'SYSTEM',	NULL),
(uuid_generate_v4(),	'SO_ST_OPEN',	'OS_ST_02',	false, 'admin', '2024-03-29 00:00:00.022985', NULL, NULL, 'SYSTEM',	NULL),
(uuid_generate_v4(),	'SO_ST_WAITINGSHIPPING',	'OS_ST_03',	false, 'admin', '2024-03-29 00:00:00.022985', NULL, NULL, 'SYSTEM',	NULL),
(uuid_generate_v4(),	'SO_ST_SHIPPING',	'OS_ST_04',	false, 'admin', '2024-03-29 00:00:00.022985', NULL, NULL, 'SYSTEM',	NULL),
(uuid_generate_v4(),	'SO_ST_DELIVERED',	'OS_ST_05',	false, 'admin', '2024-03-29 00:00:00.022985', NULL, NULL, 'SYSTEM',	NULL),
(uuid_generate_v4(),	'SO_ST_PARTIALDELIVERED',	'OS_ST_05',	false, 'admin', '2024-03-29 00:00:00.022985', NULL, NULL, 'SYSTEM',	NULL);


INSERT INTO "SystemSettings" ("Id", "SettingType", "SettingKey", "SettingValue", "Description", "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate", "DeletedBy", "DeletedDate", "IsActive", "PrincipalDescription", "IsDefault") VALUES
(uuid_generate_v4(),	'OS_SO_STATUS',	'OS_SO_00',	'WaitingImport',	'Đơn hàng chờ import',	'admin',	'2024-03-29 11:25:15.11563',	NULL,	NULL,	NULL,	NULL,	't',	NULL,	't'),
(uuid_generate_v4(),	'OS_SO_STATUS',	'OS_SO_01',	'ImportSuccessfully',	'Đơn hàng import thành công',	'admin',	'2024-03-29 11:25:15.11563',	NULL,	NULL,	NULL,	NULL,	't',	NULL,	't'),
(uuid_generate_v4(),	'OS_SO_STATUS',	'OS_SO_04',	'OutOfStockBudget',	'Đơn hàng hết tồn kho và ngân sách',	'admin',	'2024-03-29 11:25:15.11563',	NULL,	NULL,	NULL,	NULL,	't',	NULL,	't'),
(uuid_generate_v4(),	'OS_SO_STATUS',	'OS_SO_03',	'OutOfBudget',	'Đơn hàng hết ngân sách',	'admin',	'2024-03-29 11:25:15.11563',	NULL,	NULL,	NULL,	NULL,	't',	NULL,	't'),
(uuid_generate_v4(),	'OS_SO_STATUS',	'OS_SO_02',	'OutOfStock',	'Đơn hàng hết tồn kho',	'admin',	'2024-03-29 11:25:15.11563',	NULL,	NULL,	NULL,	NULL,	't',	NULL,	't'),
(uuid_generate_v4(),	'OS_SO_STATUS',	'SO_ST_CANCEL',	'Đơn hàng đã huỷ',	'Đơn hàng đã huỷ',	'admin',	'2024-03-29 11:25:15.11563',	NULL,	NULL,	NULL,	NULL,	't',	NULL,	't');