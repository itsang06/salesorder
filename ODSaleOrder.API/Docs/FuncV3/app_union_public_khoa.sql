-- DROP FUNCTION public.app_union_public(varchar, anyelement);

CREATE OR REPLACE FUNCTION public.app_union_public(distributorcode character varying, tbl anyelement)
 RETURNS SETOF anyelement
 LANGUAGE plpgsql
AS $function$
DECLARE
    schema_name_var text;
    tablename text := pg_typeof(tbl)::text;
		tablename1 text := replace(pg_typeof(tbl)::text, '"', '');
    query text := '';
BEGIN
    -- Loop through each schema in the ODDistributorSchemas table
    FOR schema_name_var IN
        SELECT ods."SchemaName" as "SchemaName"
        FROM "ODDistributorSchemas" ods
				inner join information_schema.tables tb1 on ods."SchemaName"=tb1."table_schema" and tb1."table_name"=tablename1
				WHERE ods."DistributorCode" = distributorcode AND ods."IsDeleted" = false
				UNION 
				select 'public' as "SchemaName"
    LOOP
        -- Append the SELECT query for the current schema without quotes
        query := query || format('SELECT * FROM %s.%s UNION ALL ', schema_name_var, tablename);
    END LOOP;

    -- Remove the trailing ' UNION ALL '
    query := left(query, length(query) - length(' UNION ALL '));

    -- Log the constructed query for debugging purposes
    RAISE NOTICE 'Constructed Query: %', query;

    -- Execute the constructed query
    RETURN QUERY EXECUTE query;
END;
$function$
;
