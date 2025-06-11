CREATE OR REPLACE FUNCTION ProductivityByDayReportWithRouteZone (
    distributorCode VARCHAR,
    routeZoneCode VARCHAR,
    fromDate timestamp,
    toDate timestamp
) RETURNS TABLE (        
     "InventoryID" character varying(50),
     "OrderDate" date,
     "RouteZoneID" character varying(50),
     "ItemDescription" character varying(250),
     "BaseUomCode" character varying(50),
     "ShippedBaseQuantities" int
) LANGUAGE plpgsql as $func$
declare
    distributorCodes TEXT[];
begin
	distributorCodes:=  string_to_array(distributorCode, ',');

	DROP TABLE IF EXISTS temp_table;

	CREATE TEMP TABLE temp_table (
	    "OrderRefNumber" character varying(50),
	    "DistributorCode" character varying(50),
	    "RouteZoneID" character varying(50),
	    "OrderDate" timestamp,
	    "IsDeleted" boolean,
	    "Status" character varying(50)
	);

	INSERT INTO temp_table ("OrderRefNumber", "DistributorCode", "RouteZoneID", "OrderDate", "IsDeleted", "Status")
	SELECT soi."OrderRefNumber", soi."DistributorCode", soi."RouteZoneID",  soi."OrderDate", soi."IsDeleted", soi."Status"
	FROM "SO_OrderInformations" soi
	WHERE 
		COALESCE(soi."DistributorCode" IN (SELECT unnest(distributorCodes)))
        AND CASE WHEN routeZoneCode IS NULL THEN '1' WHEN routeZoneCode = '' THEN '1' ELSE soi."RouteZoneID" END = CASE WHEN routeZoneCode IS NULL THEN '1' WHEN routeZoneCode = '' THEN '1' ELSE routeZoneCode END
        AND soi."OrderDate" >= fromDate
        AND soi."OrderDate" <= toDate
        AND NOT (soi."IsDeleted")
        AND soi."Status" IN ('SO_ST_DELIVERED','SO_ST_PARTIALDELIVERED');
       
    RETURN QUERY
    SELECT DISTINCT ON (si."InventoryID", soi."OrderDate", soi."RouteZoneID")
        si."InventoryID"::character varying(50) AS "InventoryID",
        soi."OrderDate"::date as "OrderDate",    
        soi."RouteZoneID"::character varying(50) as "RouteZoneID",
        si."ItemDescription"::character varying(250) AS "ItemDescription",
        si."BaseUomCode"::character varying(50) AS "BaseUomCode",    
        SUM(si."ShippedBaseQuantities") OVER (PARTITION BY si."InventoryID", soi."OrderDate", soi."RouteZoneID")::int AS "ShippedBaseQuantities"
    FROM
        temp_table soi
    INNER JOIN
        "SO_OrderItems" si ON soi."OrderRefNumber" = si."OrderRefNumber" 
    WHERE COALESCE(soi."DistributorCode" IN (SELECT unnest(string_to_array(distributorCode, ','))), FALSE)
        AND CASE WHEN routeZoneCode IS NULL THEN '1' WHEN routeZoneCode = '' THEN '1' ELSE soi."RouteZoneID" END = CASE WHEN routeZoneCode IS NULL THEN '1' WHEN routeZoneCode = '' THEN '1' ELSE routeZoneCode END
        AND soi."OrderDate" >= fromDate
        AND soi."OrderDate" <= toDate
        AND NOT (si."IsFree") AND NOT (soi."IsDeleted") AND NOT (si."IsDeleted")
        AND soi."Status" IN ('SO_ST_DELIVERED','SO_ST_PARTIALDELIVERED');
END $func$;

select * from ProductivityByDayReportWithRouteZone('1000122',null, '2023-10-01 00:00:00','2023-12-31 23:59:59');