DROP TABLE IF EXISTS "public"."OSOrderStatusHistories";
CREATE TABLE "public"."OSOrderStatusHistories" (
  "Id" uuid NOT NULL,
  "OrderRefNumber" VARCHAR(100),
  "External_OrdNBR" VARCHAR(100),
  "OrderDate" timestamp(6),
  "OutletCode" VARCHAR(100),
  "DistributorCode" VARCHAR(100),
  "OneShopStatus" VARCHAR(100),
  "OneShopStatusName" VARCHAR(255),
  "SOStatus" VARCHAR(100),
  "SOStatusName" VARCHAR(255),
  "CreatedBy" VARCHAR(100),
  "CreatedDate" timestamp(6),
  "UpdatedBy" VARCHAR(100),
  "UpdatedDate" timestamp(6),
  CONSTRAINT "PK_OSOrderStatusHistories" PRIMARY KEY ("Id")
);


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

    IF schema_base IS NOT NULL AND LENGTH(schema_base) > 0 
    THEN
        -- Start a transaction 
        BEGIN
            -- Insert into "ODDistributorCommonTables" with condition
            table_sql := 'INSERT INTO "ODDistributorCommonTables"("Id", "TableName", "Status") (' ||
            'SELECT uuid_generate_v4() AS "Id", ''OSOrderStatusHistories'' AS "TableName", ''ACTIVE'' AS "Status" ' ||
            'WHERE NOT EXISTS(SELECT "TableName" FROM "ODDistributorCommonTables" WHERE "TableName" = ''OSOrderStatusHistories''));';
            EXECUTE table_sql;

            -- get schemabase & schema in "ODDistributorSchemas"
            schema_list := ARRAY(SELECT distinct "SchemaName" FROM "ODDistributorSchemas" Union SELECT schema_base);
            -- add schema public to list
            schema_list := ARRAY_APPEND(schema_list, 'public');
            FOREACH schemaname IN ARRAY schema_list 
            LOOP
                schemaname := TRIM(schemaname);
                --check schemaname is existed
                IF EXISTS(SELECT schema_name
                    FROM information_schema.schemata
                    WHERE schema_name = schemaname)
                THEN
                    ---sample sql to add table
                    table_sql := 'CREATE TABLE IF NOT EXISTS ' || quote_ident(schemaname) || '."OSOrderStatusHistories" (
																								"Id" uuid NOT NULL,
																								"OrderRefNumber" VARCHAR(100),
																								"External_OrdNBR" VARCHAR(100),
																								"OrderDate" timestamp(6),
																								"OutletCode" VARCHAR(100),
																								"DistributorCode" VARCHAR(100),
																								"OneShopStatus" VARCHAR(100),
																								"SOStatus" VARCHAR(100),
																								"CreatedBy" VARCHAR(100),
																								"CreatedDate" timestamp(6),
																								"UpdatedBy" VARCHAR(100),
																								"UpdatedDate" timestamp(6),
																								CONSTRAINT "PK_OSOrderStatusHistories" PRIMARY KEY ("Id")
																							);';
                    EXECUTE table_sql;
                END IF;
            END LOOP;
        EXCEPTION WHEN others THEN
            --Rollback the transaction if an error occurs
            --- add log into StagingSyncDataHistories when not success
            ROLLBACK;
            ErrorMessage := SQLERRM;
            table_sql := 'INSERT INTO "StagingSyncDataHistories"("Id","DataType","RequestType","InsertStatus","TimeRunAdhoc","StartDate","EndDate","CreatedBy","CreatedDate","UpdatedBy","UpdatedDate","ErrorMessage") VALUES(' 
                   || 'uuid_generate_v1(), ''ADD_TABLE'', ''Insert'',''FAILED'', NOW(), NOW(), NOW(), ''Admin'' ,NOW(), ''Admin'' ,NOW(), ' || quote_literal(ErrorMessage) || ');';
            EXECUTE table_sql;  
        END;
    END IF;
END $$;

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
                    table_sql := 'ALTER TABLE ' || quote_ident(schemaname) || '."OSOrderStatusHistories" 
                                                                                ADD "OneShopStatusName" VARCHAR(255) NULL,
                                                                                ADD "SOStatusName" VARCHAR(255) NULL;';
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