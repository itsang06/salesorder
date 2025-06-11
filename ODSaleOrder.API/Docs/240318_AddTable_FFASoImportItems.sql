DROP TABLE IF EXISTS "FFASoImportItems";
CREATE TABLE "public"."FFASoImportItems" (
    "Id" uuid NOT NULL,
    "External_OrdNBR" character varying(100),
    "ItemCode" character varying(100),
    "ItemGroupId" character varying(100),
    "LocationID" character varying(100),
    "UOM" character varying(50),
    "QtyNeedBook" integer,
    "QtyBooked" integer,
    "IsDeleted" boolean,
    "CreatedDate" timestamp(6),
    "UpdatedDate" timestamp(6),
    "CreatedBy" character varying(100),
    "UpdatedBy" character varying(100),
    CONSTRAINT "PK_FFASoImportItems" PRIMARY KEY ("Id")
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
        'SELECT uuid_generate_v4() AS "Id", ''FFASoImportItems'' AS "TableName", ''ACTIVE'' AS "Status" ' ||
        'WHERE NOT EXISTS(SELECT "TableName" FROM "ODDistributorCommonTables" WHERE "TableName" = ''FFASoImportItems''));';
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
											table_sql := 'CREATE TABLE IF NOT EXISTS ' || quote_ident(schemaname) || '."FFASoImportItems" (
    "Id" uuid NOT NULL,
    "External_OrdNBR" character varying(100),
    "ItemCode" character varying(100),
    "ItemGroupId" character varying(100),
    "LocationID" character varying(100),
    "UOM" character varying(50),
    "QtyNeedBook" integer,
    "QtyBooked" integer,
    "IsDeleted" boolean,
    "CreatedDate" timestamp(6),
    "UpdatedDate" timestamp(6),
    "CreatedBy" character varying(100),
    "UpdatedBy" character varying(100),
    CONSTRAINT "PK_FFASoImportItems" PRIMARY KEY ("Id")
);';
											EXECUTE table_sql;
									END IF;
								EXCEPTION WHEN OTHERS THEN
										-- keep looping
								END;
            END LOOP;
    END IF;
END $$;