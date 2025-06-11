ALTER TABLE public."SO_OrderInformations"
ADD COLUMN "OwnerType" character varying(100) NULL,
ADD COLUMN "OwnerCode" character varying(255) NULL,
ADD COLUMN "CountryId" uuid NULL,
ADD COLUMN "ProvinceId" uuid NULL,
ADD COLUMN "DistrictId" uuid NULL,
ADD COLUMN "WardId" uuid NULL;

ALTER TABLE public."SO_OrderItems"
ADD COLUMN "OwnerType" character varying(100) NULL,
ADD COLUMN "OwnerCode" character varying(255) NULL;

ALTER TABLE public."SO_FirstTimeCustomers"
ADD COLUMN "OwnerType" character varying(100) NULL,
ADD COLUMN "OwnerCode" character varying(255) NULL;

ALTER TABLE public."SO_PRO_ProgramCustomerDetailsItems"
ADD COLUMN "OwnerType" character varying(100) NULL,
ADD COLUMN "OwnerCode" character varying(255) NULL;

ALTER TABLE public."SO_PRO_ProgramCustomerItemsGroup"
ADD COLUMN "OwnerType" character varying(100) NULL,
ADD COLUMN "OwnerCode" character varying(255) NULL;

ALTER TABLE public."SO_PRO_ProgramCustomers"
ADD COLUMN "OwnerType" character varying(100) NULL,
ADD COLUMN "OwnerCode" character varying(255) NULL;

ALTER TABLE public."SO_PRO_ProgramCustomersDetails"
ADD COLUMN "OwnerType" character varying(100) NULL,
ADD COLUMN "OwnerCode" character varying(255) NULL;

ALTER TABLE public."SO_Reasons"
ADD COLUMN "OwnerType" character varying(100) NULL,
ADD COLUMN "OwnerCode" character varying(255) NULL;

ALTER TABLE public."SO_SalesOrderSettings"
ADD COLUMN "OwnerType" character varying(100) NULL,
ADD COLUMN "OwnerCode" character varying(255) NULL;

ALTER TABLE public."SO_SumPickingListDetails"
ADD COLUMN "OwnerType" character varying(100) NULL,
ADD COLUMN "OwnerCode" character varying(255) NULL;

ALTER TABLE public."SO_SumPickingListHeaders"
ADD COLUMN "OwnerType" character varying(100) NULL,
ADD COLUMN "OwnerCode" character varying(255) NULL;

ALTER TABLE public."SO_SalesOrderSettings"
ADD COLUMN "DeliveryLeadDate" integer DEFAULT 1;

--
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
         -- get schemabase & schema in "ODDistributorSchemas"
        schema_list := ARRAY(SELECT distinct "SchemaName" FROM "ODDistributorSchemas" Union SELECT schema_base);
        -- add schema public to list
        schema_list := ARRAY_APPEND(schema_list, 'public');
        -- Start a transaction 
        BEGIN
            FOREACH schemaname IN ARRAY schema_list 
            LOOP
                schemaname := TRIM(schemaname);
                --check schemaname is existed
                IF EXISTS(SELECT schema_name
                    FROM information_schema.schemata
                    WHERE schema_name = schemaname)
                THEN
                    ---sample sql to add field
                    table_sql := 'ALTER TABLE ' || quote_ident(schemaname) || '."SO_SalesOrderSettings"
                                                                                ADD COLUMN "DeliveryLeadDate" integer DEFAULT 1;';
                    EXECUTE table_sql;
                END IF;
            END LOOP;
        EXCEPTION WHEN others THEN
            --Rollback the transaction if an error occurs
            --- add log into StagingSyncDataHistories when not success
            ROLLBACK;
            ErrorMessage := SQLERRM;
            table_sql := 'INSERT INTO "StagingSyncDataHistories"("Id","DataType","RequestType","InsertStatus","TimeRunAdhoc","StartDate","EndDate","CreatedBy","CreatedDate","UpdatedBy","UpdatedDate","ErrorMessage") VALUES(' 
                   || 'uuid_generate_v1(), ''ADD_FIELD'', ''Insert'',''FAILED'', NOW(), NOW(), NOW(), ''Admin'' ,NOW(), ''Admin'' ,NOW(), ' || quote_literal(ErrorMessage) || ');';
            EXECUTE table_sql;
        END;
    END IF;
END $$;


INSERT INTO "Services" ("Id", "Code", "Name", "URL", "CreatedDate", "UpdatedDate", "CreatedBy", "UpdatedBy", "APIKind", "InternetType", "Versions", "ECRURL", "ECRVersion") VALUES
(uuid_generate_v4(),	'ODSaleOrderAPI',	'OD Sales Order API',	'https://principlecode-saleorder-api.vndigitech.com/api/v1/',	'2024-03-08 00:00:00.022985',	NULL,	'admin',	NULL,	1,	1,	'v1',	'test',	'1.0.1');