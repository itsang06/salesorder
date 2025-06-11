ALTER TABLE public."SO_OrderInformations"
DROP COLUMN "CountryId",
DROP COLUMN "ProvinceId",
DROP COLUMN "DistrictId",
DROP COLUMN "WardId";

ALTER TABLE public."SO_OrderInformations"
ADD COLUMN "CusAddressCountryId" character varying(100) NULL,
ADD COLUMN "CusAddressProvinceId" character varying(100) NULL,
ADD COLUMN "CusAddressDistrictId" character varying(100) NULL,
ADD COLUMN "CusAddressWardId" character varying(100) NULL;

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
                    ---sample sql to rename field
                    table_sql := 'ALTER TABLE ' || quote_ident(schemaname) || '."OSSoOrderItems"
                                    ADD COLUMN "BudgetCheckStatus" boolean NULL,
                                    ADD COLUMN "BudgetCode" character varying(100) NULL,
                                    ADD COLUMN "BudgetType" character varying(100) NULL,
                                    ADD COLUMN "BudgetDemand" integer NULL,
                                    ADD COLUMN "BudgetBooked" integer NULL,
                                    ADD COLUMN "BudgetBook" integer NULL,
                                    ADD COLUMN "BudgetBookOver" boolean NULL,
                                    ADD COLUMN "BudgetBookOption" character varying(100) NULL,
                                    ADD COLUMN "IsBudget" boolean NULL;';
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