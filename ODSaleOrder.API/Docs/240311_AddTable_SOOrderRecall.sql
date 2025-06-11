DROP TABLE IF EXISTS "public"."SOOrderRecalls";
CREATE TABLE "public"."SOOrderRecalls" (
    "Id" uuid NOT NULL,
    "Code" character varying(50) NOT NULL,
    "RequestRecallReason" character varying(255),
    "RequestRecallCode" character varying(50),
    "RecallType" character varying(50),
    "Description" character varying(255),
    "DistributorShiptoCode" character varying(50),
    "RecallLocationCode" character varying(50),
    "GiveBackLocationCode" character varying(50),
    "Status" character varying(50),
    "CreatedDate" timestamp NULL,
    "UpdatedDate" timestamp NULL,
    "CreatedBy" character varying(255) NULL,
    "UpdatedBy" character varying(255) NULL,
    "IsDeleted" boolean NOT NULL,
    "OwnerType" character varying(100) NULL,
    "OwnerCode" character varying(255) NULL,
    CONSTRAINT "PK_SOOrderRecalls" PRIMARY KEY ("Id")
);

DROP TABLE IF EXISTS "public"."SOOrderRecallOrders";
CREATE TABLE "public"."SOOrderRecallOrders" (
    "Id" uuid NOT NULL, 
    "RecallCode" character varying(50), 
    "RefDetailReqId" uuid NULL, 
    "CustomerCode" character varying(100), 
    "CustomerName" character varying(255), 
    "CustomerShiptoCode" character varying(100), 
    "CustomerShiptoName" character varying(100), 
    "DistributorCode" character varying(10), 
    "OrderCode" character varying(50), 
    "ItemCode" character varying(100), 
    "ItemDescription" character varying(80), 
    "Uom" character varying(100), 
    "RecallQty" int NOT NULL, 
    "RecallBaseQty" int NOT NULL, 
    "ItemGiveBackCode" character varying(100), 
    "ItemGiveBackDesc" character varying(80), 
    "GivBackQty" int NOT NULL, 
    "GiveBackBaseQty" int NOT NULL, 
    "GiveBackUom" character varying(100), 
    "CreatedDate" timestamp NULL, 
    "UpdatedDate" timestamp NULL, 
    "CreatedBy" character varying(255) NULL, 
    "UpdatedBy" character varying(255) NULL, 
    "IsDeleted" boolean NOT NULL, 
    "OwnerType" character varying(100) NULL, 
    "OwnerCode" character varying(255) NULL, 
    CONSTRAINT "PK_SOOrderRecallOrders" PRIMARY KEY ("Id")
);

DO $$
DECLARE
    schema_base text;
    table_sql text;
    schemaname text;
    schema_list TEXT[];
    ErrorMessage text;
BEGIN
    -- Get schema_base name form "SystemSettings"
	schema_base := (SELECT "SettingValue" FROM "SystemSettings" WHERE "SettingType" = 'ODSchema' AND "SettingKey" = 'SchemaBaseName' AND "IsActive" = 't' LIMIT 1);

    IF schema_base IS NOT NULL AND LENGTH(schema_base) > 0 THEN
        -- Insert into "ODDistributorCommonTables" with condition
        table_sql := 'INSERT INTO "ODDistributorCommonTables"("Id", "TableName", "Status") (' ||
        'SELECT uuid_generate_v4() AS "Id", ''SOOrderRecallOrders'' AS "TableName", ''ACTIVE'' AS "Status" ' ||
        'WHERE NOT EXISTS(SELECT "TableName" FROM "ODDistributorCommonTables" WHERE "TableName" = ''SOOrderRecallOrders''));';
        EXECUTE table_sql;

        -- get schemabase & schema in "ODDistributorSchemas"
        schema_list := ARRAY(SELECT distinct "SchemaName" FROM "ODDistributorSchemas" Union SELECT schema_base);
        -- add schema public to list
        schema_list := ARRAY_APPEND(schema_list, 'public');
            FOREACH schemaname IN ARRAY schema_list LOOP
							BEGIN
									schemaname := TRIM(schemaname);
									--check schemaname is existed
									IF EXISTS(SELECT schema_name
											FROM information_schema.schemata
											WHERE schema_name = schemaname)
									THEN
											---sample sql to add field
											table_sql := 'CREATE TABLE IF NOT EXISTS ' || quote_ident(schemaname) || '."SOOrderRecallOrders" (
    "Id" uuid NOT NULL, 
    "RecallCode" character varying(50), 
    "RefDetailReqId" uuid NULL, 
    "CustomerCode" character varying(100), 
    "CustomerName" character varying(255), 
    "CustomerShiptoCode" character varying(100), 
    "CustomerShiptoName" character varying(100), 
    "DistributorCode" character varying(10), 
    "OrderCode" character varying(50), 
    "ItemCode" character varying(100), 
    "ItemDescription" character varying(80), 
    "Uom" character varying(100), 
    "RecallQty" int NOT NULL, 
    "RecallBaseQty" int NOT NULL, 
    "ItemGiveBackCode" character varying(100), 
    "ItemGiveBackDesc" character varying(80), 
    "GivBackQty" int NOT NULL, 
    "GiveBackBaseQty" int NOT NULL, 
    "GiveBackUom" character varying(100), 
    "CreatedDate" timestamp NULL, 
    "UpdatedDate" timestamp NULL, 
    "CreatedBy" character varying(255) NULL, 
    "UpdatedBy" character varying(255) NULL, 
    "IsDeleted" boolean NOT NULL, 
    "OwnerType" character varying(100) NULL, 
    "OwnerCode" character varying(255) NULL, 
    CONSTRAINT "PK_SOOrderRecallOrders" PRIMARY KEY ("Id")
);';
											EXECUTE table_sql;
									END IF;
								EXCEPTION WHEN OTHERS THEN
										-- keep looping
								END;
            END LOOP;
    END IF;
END $$;