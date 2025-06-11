CREATE OR REPLACE FUNCTION ODRouteZoneProductivityByProductReport (
     itemAttribute VARCHAR,
     distributorCode VARCHAR,
     routeZoneCode VARCHAR,
     status VARCHAR, 
     fromDate timestamp,
     toDate timestamp,
		 schemaName VARCHAR
) RETURNS TABLE (
     "RouteZoneID" character varying(50),
     "InventoryAttibute" character varying(50),     
     "Qty" int
) LANGUAGE plpgsql as $func$
declare
	distributorCodes TEXT[];
	statuses TEXT[];
   
begin
	distributorCodes:=  string_to_array(distributorCode, ',');
	statuses:=  string_to_array(status, ',');

	-- Set search_path to the specified schema
	EXECUTE format('SET search_path TO %I', schemaName);

	DROP TABLE IF EXISTS temp_table;

	CREATE TEMP TABLE temp_table (
	    "OrderRefNumber" character varying(50),
	    "DistributorCode" character varying(50),
	    "RouteZoneID" character varying(50),
	    "OrderDate" timestamp,
	    "IsDeleted" boolean,
	    "Status" character varying(50),
	    "OrderType" character varying(50),
	    "TerritoryValueKey" character varying(50)
	);

	INSERT INTO temp_table ("OrderRefNumber", "DistributorCode", "RouteZoneID", "OrderDate", "IsDeleted", "Status","OrderType")
	SELECT soi."OrderRefNumber", soi."DistributorCode", soi."RouteZoneID",  soi."OrderDate", soi."IsDeleted", soi."Status", soi."OrderType"
	FROM "SO_OrderInformations" soi
	WHERE 
		 CASE WHEN routeZoneCode IS NULL THEN '1' WHEN routeZoneCode = '' THEN '1' ELSE soi."RouteZoneID" END = CASE WHEN routeZoneCode IS NULL THEN '1' WHEN  routeZoneCode = '' THEN '1' ELSE routeZoneCode END
	     AND soi."OrderDate" >= fromDate
	     AND soi."OrderDate" <= toDate
	     AND NOT (soi."IsDeleted")
	     AND soi."RouteZoneID" IS NOT NULL
	     AND soi."OrderType" = 'SalesOrder'
	     AND COALESCE(soi."Status" IN (SELECT unnest(statuses)), FALSE)
	     AND COALESCE(soi."DistributorCode" IN (SELECT unnest(distributorCodes)), FALSE);
       
     RETURN QUERY
     SELECT 
        s."RouteZoneID"::character varying(50) as "RouteZoneID",
        (CASE 
            WHEN itemAttribute = 'IT01' THEN so."InventoryAttibute1"
            WHEN itemAttribute = 'IT02' THEN so."InventoryAttibute2"
            WHEN itemAttribute = 'IT03' THEN so."InventoryAttibute3"
            WHEN itemAttribute = 'IT04' THEN so."InventoryAttibute4"
            WHEN itemAttribute = 'IT05' THEN so."InventoryAttibute5"
            WHEN itemAttribute = 'IT06' THEN so."InventoryAttibute6"
            WHEN itemAttribute = 'IT07' THEN so."InventoryAttibute7"
            WHEN itemAttribute = 'IT08' THEN so."InventoryAttibute8"
            WHEN itemAttribute = 'IT09' THEN so."InventoryAttibute9"
            WHEN itemAttribute = 'IT10' THEN so."InventoryAttibute10"
        END)::character varying(50) as InventoryAttibute,         
        sum(so."ShippedBaseQuantities")::int as Qty              
     FROM temp_table s 
     INNER JOIN "SO_OrderItems" so ON s."OrderRefNumber" = so."OrderRefNumber"
     WHERE 
		 CASE WHEN routeZoneCode IS NULL THEN '1' WHEN routeZoneCode = '' THEN '1' ELSE s."RouteZoneID" END = CASE WHEN routeZoneCode IS NULL THEN '1' WHEN  routeZoneCode = '' THEN '1' ELSE routeZoneCode END 
		 AND s."OrderDate" >= fromDate
		 AND s."OrderDate" <= toDate     
		 AND NOT (so."IsFree") AND NOT (s."IsDeleted")
		 AND NOT (so."IsDeleted")
		 AND s."RouteZoneID" IS NOT NULL
		 AND s."OrderType" = 'SalesOrder'
		 AND COALESCE(s."Status" IN (SELECT unnest(statuses)), FALSE)
		 AND COALESCE(s."DistributorCode" IN (SELECT unnest(distributorCodes)), FALSE)
		 AND CASE 
		        WHEN itemAttribute = 'IT01' THEN so."InventoryAttibute1"
		        WHEN itemAttribute = 'IT02' THEN so."InventoryAttibute2"
		        WHEN itemAttribute = 'IT03' THEN so."InventoryAttibute3"
		        WHEN itemAttribute = 'IT04' THEN so."InventoryAttibute4"
		        WHEN itemAttribute = 'IT05' THEN so."InventoryAttibute5"
		        WHEN itemAttribute = 'IT06' THEN so."InventoryAttibute6"
		        WHEN itemAttribute = 'IT07' THEN so."InventoryAttibute7"
		        WHEN itemAttribute = 'IT08' THEN so."InventoryAttibute8"
		        WHEN itemAttribute = 'IT09' THEN so."InventoryAttibute9"
		        WHEN itemAttribute = 'IT10' THEN so."InventoryAttibute10"
		    END IS NOT NULL
		 GROUP BY 
		    (CASE 
		        WHEN itemAttribute = 'IT01' THEN so."InventoryAttibute1"
		        WHEN itemAttribute = 'IT02' THEN so."InventoryAttibute2"
		        WHEN itemAttribute = 'IT03' THEN so."InventoryAttibute3"
		        WHEN itemAttribute = 'IT04' THEN so."InventoryAttibute4"
		        WHEN itemAttribute = 'IT05' THEN so."InventoryAttibute5"
		        WHEN itemAttribute = 'IT06' THEN so."InventoryAttibute6"
		        WHEN itemAttribute = 'IT07' THEN so."InventoryAttibute7"
		        WHEN itemAttribute = 'IT08' THEN so."InventoryAttibute8"
		        WHEN itemAttribute = 'IT09' THEN so."InventoryAttibute9"
		        WHEN itemAttribute = 'IT10' THEN so."InventoryAttibute10"
		    END), s."RouteZoneID";
	-- Reset search_path to its default value
	EXECUTE 'RESET search_path';
END $func$;

select * 
from ODRouteZoneProductivityByProductReport('IT01', 'p0401241', null, 'SO_ST_CANCEL,SO_ST_DELIVERED', '2024-01-01', '2024-04-16', 'osp0401241');