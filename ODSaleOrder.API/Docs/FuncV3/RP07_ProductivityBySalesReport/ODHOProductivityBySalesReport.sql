CREATE OR REPLACE FUNCTION ODHOProductivityBySalesReport (
     itemAttribute VARCHAR,
     distributorCode VARCHAR,
     status VARCHAR, 
     fromDate timestamp,
     toDate timestamp,
     schemaName VARCHAR
) RETURNS TABLE (
     "InventoryAttibute" character varying(50),
     "TerritoryValueKey" character varying(50),
     "Qty" int
) LANGUAGE plpgsql as $func$
declare
	distributorCodes TEXT[];
	statuses TEXT[];
begin
	distributorCodes:= string_to_array(distributorCode, ',');
	statuses:= string_to_array(status, ',');
	
    -- Set search_path to the specified schema
	EXECUTE format('SET search_path TO %I', schemaName);

	DROP TABLE IF EXISTS temp_table;
	CREATE TEMP TABLE temp_table (
	    "OrderRefNumber" character varying(50),
	    "DistributorCode" character varying(50),
	    "TerritoryValueKey" character varying(50),
	    "OrderDate" timestamp,
	    "IsDeleted" boolean,
	    "Status" character varying(50)
	);
	
	INSERT INTO temp_table ("OrderRefNumber", "DistributorCode", "TerritoryValueKey", "OrderDate", "IsDeleted", "Status")
	SELECT s."OrderRefNumber", s."DistributorCode", s."TerritoryValueKey",  s."OrderDate", s."IsDeleted", s."Status"
	FROM "SO_OrderInformations" s
	WHERE s."OrderDate" >= fromDate
	     AND s."OrderDate" <= toDate
	     AND NOT (s."IsDeleted")
         AND s."TerritoryValueKey" IS NOT NULL
	     AND s."Status" IN (SELECT unnest(statuses))
	     AND s."DistributorCode" IN (SELECT unnest(distributorCodes));
	    
     RETURN QUERY
     SELECT 
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
        END)::character varying(50) as "InventoryAttibute", 
        s."TerritoryValueKey"::character varying(50) as "TerritoryValueKey",
        sum(so."ShippedBaseQuantities")::int as "Qty"              
     FROM temp_table s 
     INNER JOIN "SO_OrderItems" so ON s."OrderRefNumber" = so."OrderRefNumber"
     WHERE s."OrderDate" >= fromDate
     AND s."OrderDate" <= toDate
     AND NOT (so."IsFree") 
     AND NOT (s."IsDeleted")
     AND NOT (so."IsDeleted")
     AND s."TerritoryValueKey" IS NOT NULL
     AND s."Status" IN (SELECT unnest(statuses))
     AND s."DistributorCode" IN (SELECT unnest(distributorCodes))
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
        END), s."TerritoryValueKey";
    -- Reset search_path to its default value
	EXECUTE 'RESET search_path';
END $func$;

select * 
from ODHOProductivityBySalesReport('IT01', 'p0401241', 'SO_ST_OPEN,SO_ST_SHIPPING,SO_ST_WAITINGSHIPPING,SO_ST_DELIVERED,SO_ST_PARTIALDELIVERED,SO_ST_FAILED,SO_ST_CONFIRM,SO_ST_COMPLETE_DRAFT', '2024-01-01', '2024-03-16', 'osp0401241');