CREATE OR REPLACE FUNCTION ODProductivityReport (
    distributorCode VARCHAR,
    fromDate timestamp,
    toDate timestamp,
    schemaName VARCHAR
) RETURNS TABLE (    
     "InventoryID" character varying(50),
     "ItemDescription" character varying(250),
     "BaseUomCode" character varying(50),
     "SalesOrgID" character varying(50),
     "TerritoryStrID" character varying(50),
     "TerritoryValueKey" character varying(50),
     "OrderBaseQuantities" int,
     "ShippedBaseQuantities" int    
) LANGUAGE plpgsql as $func$
declare
    distributorCodes TEXT[];
begin
	distributorCodes:=  string_to_array(distributorCode, ',');
    -- Set search_path to the specified schema
    EXECUTE format('SET search_path TO %I', schemaName);

	DROP TABLE IF EXISTS temp_table;

	CREATE TEMP TABLE temp_table (
	    "OrderRefNumber" character varying(50),
	    "DistributorCode" character varying(50),
	    "SalesOrgID" character varying(50),
	    "TerritoryStrID" character varying(50),
	    "TerritoryValueKey" character varying(50),
	    "OrderDate" timestamp,
	    "IsDeleted" boolean,
	    "Status" character varying(50)
	);

	INSERT INTO temp_table ("OrderRefNumber", "DistributorCode", "SalesOrgID", "TerritoryStrID", "TerritoryValueKey", "OrderDate", "IsDeleted", "Status")
	SELECT soi."OrderRefNumber", soi."DistributorCode", soi."SalesOrgID", soi."TerritoryStrID", soi."TerritoryValueKey", soi."OrderDate", soi."IsDeleted", soi."Status"
	FROM "SO_OrderInformations" soi
	WHERE  soi."DistributorCode" IN (SELECT unnest(distributorCodes))
	    AND soi."OrderDate" >= fromDate
	    AND soi."OrderDate" <= toDate
	    AND NOT (soi."IsDeleted")
	    AND soi."Status" IN ('SO_ST_DELIVERED','SO_ST_PARTIALDELIVERED');
	   
    RETURN QUERY
    select DISTINCT ON (si."InventoryID")
        si."InventoryID"::character varying(50) AS "InventoryID",
        si."ItemDescription"::character varying(250) AS "ItemDescription",
        si."BaseUomCode"::character varying(50) AS "BaseUomCode",
        soi."SalesOrgID"::character varying(50) AS "SalesOrgID",
        soi."TerritoryStrID"::character varying(50) AS "TerritoryStrID",
        soi."TerritoryValueKey"::character varying(50) AS "TerritoryValueKey",
        SUM(si."OrderBaseQuantities") OVER (PARTITION BY si."InventoryID")::int AS "OrderBaseQuantities", 
        SUM(si."ShippedBaseQuantities") OVER (PARTITION BY si."InventoryID")::int AS "ShippedBaseQuantities"       

    FROM
        temp_table soi
    INNER JOIN
        "SO_OrderItems" si ON soi."OrderRefNumber" = si."OrderRefNumber"
    WHERE  soi."DistributorCode" IN (SELECT unnest(distributorCodes))
        AND soi."OrderDate" >= fromDate
        AND soi."OrderDate" <= toDate
        AND NOT (si."IsFree") AND NOT (soi."IsDeleted") AND NOT (si."IsDeleted")
        AND soi."Status" IN ('SO_ST_DELIVERED','SO_ST_PARTIALDELIVERED');
        
    -- Reset search_path to its default value
	EXECUTE 'RESET search_path';
END $func$;

select * from ODProductivityReport('p0401241','2024-01-01', '2024-03-16', 'osp0401241');