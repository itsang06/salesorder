DROP FUNCTION public.f_getlistshiptobycustomer(varchar, varchar);
CREATE OR REPLACE FUNCTION public.f_getlistshiptobycustomer(distributorcode character varying, customercode character varying)
RETURNS TABLE(
  "Id" uuid, 
  "ShiptoCode" character varying, 
  "ShiptoName" character varying, 
  "ShiptoAddress" character varying, 
  "Province" uuid, 
  "District" uuid, 
  "Wards" uuid, 
  "Country" uuid, 
  "City" uuid, 
  "Region" uuid, 
  "State" uuid,
  "ProvinceCode" character varying, 
  "DistrictCode" character varying, 
  "WardCode" character varying, 
  "CountryCode" character varying, 
  "CityCode" character varying, 
  "RegionCode" character varying, 
  "StateCode" character varying
)
LANGUAGE plpgsql
AS $function$
DECLARE 
  schemaName VARCHAR(100);
  excuteQuery TEXT;
  row_count INTEGER;
BEGIN
  -- Lấy schema riêng của distributor
  schemaName := (
    SELECT TRIM(schema."SchemaName") 
    FROM "ODDistributorSchemas" schema 
    WHERE schema."DistributorCode" = distributorcode 
    AND schema."IsDeleted" = FALSE 
    LIMIT 1
  );

  -- Truy vấn trên public trước
  excuteQuery := FORMAT(
    '
    SELECT DISTINCT
      shipto."Id"::uuid AS "Id",
      shipto."ShiptoCode"::varchar AS "ShiptoCode",
      shipto."ShiptoName"::varchar AS "ShiptoName",
      shipto."Address"::varchar AS "ShiptoAddress",
      shipto."Province"::uuid,
      shipto."District"::uuid,
      shipto."Wards"::uuid,
      shipto."Country"::uuid,
      shipto."City"::uuid,
      shipto."Region"::uuid,
      shipto."State"::uuid,
      shiptoProvinceInfo."ProvinceCode"::varchar AS "ProvinceCode",
      shiptoDistrictInfo."DistrictCode"::varchar AS "DistrictCode",
      shiptoWardInfo."WardCode"::varchar AS "WardCode",
      shiptoCountryInfo."CountryCode"::varchar AS "CountryCode",
      shiptoCityInfo."CityCode"::varchar AS "CityCode",
      shiptoRegionInfo."RegionCode"::varchar AS "RegionCode",
      shiptoStateInfo."StateCode"::varchar AS "StateCode"
    FROM "public"."CustomerShiptos" shipto
    INNER JOIN "public"."CustomerInformations" cus 
      ON cus."Id" = shipto."CustomerInfomationId"
      AND cus."DeleteFlag" = 0
      AND cus."CustomerCode" = %L
    INNER JOIN "public"."RZ_RouteZoneShiptos" rzshipto 
      ON rzshipto."ShiptoId" = shipto."Id"
      AND (now() >= rzshipto."EffectiveDate" AND (rzshipto."ValidUntil"  >= now() OR rzshipto."ValidUntil" IS NULL))
    INNER JOIN "public"."RZ_RouteZoneInfomations" rz 
      ON rz."RouteZoneCode" = rzshipto."RouteZoneCode"
      AND rz."DistributorCode" = %L
      AND rz."Status"= ''Active'' 
      AND (now() >= rz."EffectiveDate" AND (rz."ValidUntil"  >= now() OR rz."ValidUntil" IS NULL))
    LEFT JOIN "Countrys" as shiptoCountryInfo on shiptoCountryInfo."Id" = shipto."Country"
    LEFT JOIN "States" as shiptoStateInfo on shiptoStateInfo."Id" = shipto."State"
    LEFT JOIN "Provinces" as shiptoProvinceInfo on shiptoProvinceInfo."Id" = shipto."Province"
    LEFT JOIN "Districts" as shiptoDistrictInfo on shiptoDistrictInfo."Id" = shipto."District"
    LEFT JOIN "Wards" as shiptoWardInfo on shiptoWardInfo."Id" = shipto."Wards"
    LEFT JOIN "Citys" as shiptoCityInfo on shiptoCityInfo."Id" = shipto."City"
    LEFT JOIN "Regions" as shiptoRegionInfo on shiptoRegionInfo."Id" = shipto."Region"
    WHERE shipto."DeleteFlag" = 0;
    ', customercode, distributorcode
  );

  RETURN QUERY EXECUTE excuteQuery;

  IF NOT FOUND THEN
    -- Nếu không có dữ liệu trong public, truy vấn lại trong schema riêng của distributor
    IF schemaName IS NOT NULL AND LENGTH(schemaName) > 0 THEN
      IF EXISTS (SELECT schema_name FROM information_schema.schemata WHERE schema_name = schemaName) THEN
        excuteQuery := FORMAT(
          '
          SELECT DISTINCT
            shipto."Id"::uuid AS "Id",
            shipto."ShiptoCode"::varchar AS "ShiptoCode",
            shipto."ShiptoName"::varchar AS "ShiptoName",
            shipto."Address"::varchar AS "ShiptoAddress",
            shipto."Province"::uuid,
            shipto."District"::uuid,
            shipto."Wards"::uuid,
            shipto."Country"::uuid,
            shipto."City"::uuid,
            shipto."Region"::uuid,
            shipto."State"::uuid,
            shiptoProvinceInfo."ProvinceCode"::varchar AS "ProvinceCode",
            shiptoDistrictInfo."DistrictCode"::varchar AS "DistrictCode",
            shiptoWardInfo."WardCode"::varchar AS "WardCode",
            shiptoCountryInfo."CountryCode"::varchar AS "CountryCode",
            shiptoCityInfo."CityCode"::varchar AS "CityCode",
            shiptoRegionInfo."RegionCode"::varchar AS "RegionCode",
            shiptoStateInfo."StateCode"::varchar AS "StateCode"
          FROM %I."CustomerShiptos" shipto
          INNER JOIN %I."CustomerInformations" cus 
            ON cus."Id" = shipto."CustomerInfomationId"
            AND cus."DeleteFlag" = 0
            AND cus."CustomerCode" = %L
          LEFT JOIN "Countrys" as shiptoCountryInfo on shiptoCountryInfo."Id" = shipto."Country"
          LEFT JOIN "States" as shiptoStateInfo on shiptoStateInfo."Id" = shipto."State"
          LEFT JOIN "Provinces" as shiptoProvinceInfo on shiptoProvinceInfo."Id" = shipto."Province"
          LEFT JOIN "Districts" as shiptoDistrictInfo on shiptoDistrictInfo."Id" = shipto."District"
          LEFT JOIN "Wards" as shiptoWardInfo on shiptoWardInfo."Id" = shipto."Wards"
          LEFT JOIN "Citys" as shiptoCityInfo on shiptoCityInfo."Id" = shipto."City"
          LEFT JOIN "Regions" as shiptoRegionInfo on shiptoRegionInfo."Id" = shipto."Region"
          WHERE shipto."DeleteFlag" = 0;
          ', schemaName, schemaName, customercode, schemaName, schemaName
        );

        -- Debug SQL
        RAISE NOTICE 'Executing Query on Private Schema: %', excuteQuery;

        -- Thực thi truy vấn động
        RETURN QUERY EXECUTE excuteQuery;
      END IF;
    END IF;
  END IF;
END
$function$;

-- SELECT * FROM public.f_getlistshiptobycustomer('p0401241', 'C24700002');
SELECT * FROM public.f_getlistshiptobycustomer('20240710', '0000060326');