CREATE OR REPLACE FUNCTION public.f_getcustomerbydistributorpaging(
  distributorcode character varying,
  search_text character varying DEFAULT NULL,
  page_number integer DEFAULT 1,
  page_size integer DEFAULT 10
)
RETURNS TABLE(
  "CustomerId" uuid,
  "CustomerCode" character varying,
  "CustomerName" character varying,
  "CustomerAddress" character varying,
  "PhoneNumber" character varying,
  "Email" character varying,
  "CustomerType" character varying,
  "IsFirstTimeCustomer" boolean,
  "TotalCount" integer
) 
LANGUAGE plpgsql
AS $function$
DECLARE 
  schemaName VARCHAR(100);
  excuteQuery TEXT;
  offset_value INTEGER;
  search_filter TEXT;
BEGIN
  offset_value := (page_number - 1) * page_size;

  IF search_text IS NOT NULL AND LENGTH(search_text) > 0 THEN
    search_filter := quote_literal('%' || search_text || '%');
  ELSE
    search_filter := 'NULL';
  END IF;

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
          WITH CombinedCustomers AS (
            SELECT DISTINCT ON (cus."CustomerCode")
              cus."Id"::uuid AS "CustomerId",
              cus."CustomerCode", 
              cus."FullName"::varchar AS "CustomerName",
              cus."BusinessAddress"::varchar AS "CustomerAddress",
              cus."PhoneNumber"::varchar AS "PhoneNumber",
              cus."Email"::varchar AS "Email",
              ''OFFICIAL''::varchar AS "CustomerType",
              false::boolean AS "IsFirstTimeCustomer"
            FROM "public"."CustomerInformations" cus
            INNER JOIN "public"."CustomerShiptos" shipto 
              ON shipto."CustomerInfomationId" = cus."Id" 
              AND shipto."DeleteFlag" = 0
            INNER JOIN "public"."RZ_RouteZoneShiptos" rzshipto 
              ON rzshipto."ShiptoId" = shipto."Id"
              AND (now() >= rzshipto."EffectiveDate" AND (rzshipto."ValidUntil"  >= now() OR rzshipto."ValidUntil" IS NULL))
            INNER JOIN "public"."RZ_RouteZoneInfomations" rz 
              ON rz."RouteZoneCode" = rzshipto."RouteZoneCode"
              AND rz."Status"= ''Active''
              AND (now() >= rz."EffectiveDate" AND (rz."ValidUntil"  >= now() OR rz."ValidUntil" IS NULL))
              AND rz."DistributorCode" = %L
            WHERE cus."DeleteFlag" = 0 AND cus."Status" = 0

            UNION ALL

            SELECT DISTINCT ON (cus."CustomerCode")
              cus."Id"::uuid AS "CustomerId",
              cus."CustomerCode", 
              cus."FullName"::varchar AS "CustomerName",
              cus."BusinessAddress"::varchar AS "CustomerAddress",
              cus."PhoneNumber"::varchar AS "PhoneNumber",
              cus."Email"::varchar AS "Email",
              ''OFFICIAL''::varchar AS "CustomerType",
              false::boolean AS "IsFirstTimeCustomer"
            FROM %I."CustomerInformations" cus
            WHERE cus."DeleteFlag" = 0 AND cus."Status" = 0

            UNION ALL

            SELECT DISTINCT ON (cus."CustomerCode")
              cus."Id"::uuid AS "CustomerId",
              cus."CustomerCode", 
              cus."FullName"::varchar AS "CustomerName",
              cus."BusinessAddress"::varchar AS "CustomerAddress",
              cus."PhoneNumber"::varchar AS "PhoneNumber",
              NULL::varchar AS "Email",
              ''UNOFFICIAL''::varchar AS "CustomerType",
              true::boolean AS "IsFirstTimeCustomer"
            FROM %I."SO_FirstTimeCustomers" cus
            WHERE cus."IsDeleted" = false
          ),

          TotalCustomers AS (
            SELECT CAST(COUNT(DISTINCT cus."CustomerCode") AS INTEGER) AS Total
            FROM CombinedCustomers cus
          ),

          FilteredCustomers AS (
            SELECT DISTINCT ON (com."CustomerCode") com.*, tc.Total AS "TotalCount"
            FROM CombinedCustomers as com, TotalCustomers as tc
            WHERE (%s IS NULL OR 
            com."CustomerCode" ILIKE %s OR
            com."CustomerName" ILIKE %s OR
            com."CustomerAddress" ILIKE %s OR
            com."PhoneNumber" ILIKE %s)
          )

          SELECT * 
          FROM FilteredCustomers
          ORDER BY "CustomerCode"
          LIMIT %s OFFSET %s;
        ',
        distributorcode, schemaName, schemaName, 
        search_filter, search_filter, search_filter, search_filter, search_filter,
        page_size, offset_value
      );

      -- Debug SQL
      RAISE NOTICE 'Executing Query: %', excuteQuery;

      -- Thực thi truy vấn động
      RETURN QUERY EXECUTE excuteQuery;
    END IF;
  END IF;
END
$function$;

SELECT * FROM public.f_getcustomerbydistributorpaging('THP002', NULL, 1, 10);