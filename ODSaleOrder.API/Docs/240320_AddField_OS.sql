ALTER TABLE public."OSOrderInformations"
ADD COLUMN "SOStatus" character varying(100);

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
            FOREACH schemaname IN ARRAY schema_list LOOP
							BEGIN
									schemaname := TRIM(schemaname);
									--check schemaname is existed
									IF EXISTS(SELECT schema_name
											FROM information_schema.schemata
											WHERE schema_name = schemaname)
									THEN
											---sample sql to add field
											table_sql := 'ALTER TABLE ' || quote_ident(schemaname) || '."OSOrderInformations"
                                                                                                        ADD COLUMN "SOStatus" character varying(100);';
											EXECUTE table_sql;
									END IF;
								EXCEPTION WHEN OTHERS THEN
										-- keep looping
								END;
            END LOOP;
    END IF;
END $$;