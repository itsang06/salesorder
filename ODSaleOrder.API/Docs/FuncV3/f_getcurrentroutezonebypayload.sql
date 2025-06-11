CREATE OR REPLACE FUNCTION public.f_getroutezonebasicbypayload(
  distributorcode character varying,
  customercode character varying,
  shiptocode character varying,
  dsacode character varying
)
RETURNS TABLE(
  "RouteZoneCode" character varying,
  "RouteZoneDesc" character varying
) 
LANGUAGE plpgsql
AS $function$
DECLARE 
  schemaName VARCHAR(100);
  excuteQuery TEXT;
BEGIN
  schemaName := (
    SELECT TRIM(schema."SchemaName") 
    FROM "ODDistributorSchemas" schema 
    WHERE schema."DistributorCode" = distributorcode 
    AND schema."IsDeleted" = FALSE 
    LIMIT 1
  );

  -- Kiểm tra nếu schema tồn tại
  IF schemaName IS NOT NULL AND LENGTH(schemaName) > 0 THEN
    IF EXISTS(SELECT schema_name FROM information_schema.schemata WHERE schema_name = schemaName) THEN
      excuteQuery := FORMAT(
        '
        SELECT
          rz."RouteZoneCode"::VARCHAR as "RouteZoneCode", 
					rz."Description"::VARCHAR as "RouteZoneDesc"
        FROM app_union_public(%L, NULL::"RZ_RouteZoneInfomations") rz
        INNER JOIN app_union_public(%L, NULL::"RZ_RouteZoneShiptos") rzshipto 
          ON rzshipto."RouteZoneCode" = rz."RouteZoneCode"
          AND (now() >= rzshipto."EffectiveDate" AND (rzshipto."ValidUntil"  >= now() OR rzshipto."ValidUntil" is null))
        INNER JOIN app_union_public(%L, NULL::"CustomerShiptos") shipto
          ON shipto."Id" = rzshipto."ShiptoId"
          AND shipto."ShiptoCode" = %L
        INNER JOIN app_union_public(%L, NULL::"CustomerInformations") cus 
          ON cus."Id" = shipto."CustomerInfomationId"
          AND cus."DeleteFlag" = 0
          AND cus."CustomerCode" = %L
        WHERE
          rz."DSACode" = %L
          AND rz."Status"= ''Active'' AND (now() >= rz."EffectiveDate" AND (rz."ValidUntil"  >= now() OR rz."ValidUntil" is null))
          AND "IsDeleted" = ''f''
        ORDER BY rz."EffectiveDate" DESC
        LIMIT 1
        ',
        distributorcode, distributorcode, distributorcode, shiptocode, distributorcode, customercode, dsacode
      );

      -- Debug SQL
      RAISE NOTICE 'Executing Query: %', excuteQuery;

      -- Thực thi truy vấn động
      RETURN QUERY EXECUTE excuteQuery;
    END IF;
  END IF;
END
$function$;

-- check private
SELECT * FROM public.f_getroutezonebasicbypayload('p0401241', 'C24700002', 'S002', 'DEV0301');
-- check public
SELECT * FROM public.f_getroutezonebasicbypayload('p0401241', 'C234241', 'S01', 'DEV0301');