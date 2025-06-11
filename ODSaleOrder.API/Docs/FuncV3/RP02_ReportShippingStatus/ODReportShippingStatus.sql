CREATE OR REPLACE FUNCTION ODReportShippingStatus (
    distributorCode VARCHAR,
    itemGroupCode VARCHAR, 
    fromDate timestamp,
    toDate timestamp,
    schemaName VARCHAR
) RETURNS TABLE (
    "DistributorCode" character varying(50),
    "InventoryID" character varying(50),
    "ItemDescription" character varying(250),
    "UOM" character varying(50),
    "OrderQTY" numeric,
    "ActualShippedQTY" numeric
) LANGUAGE plpgsql AS $func$
DECLARE
    distributorCodes TEXT[];
BEGIN
    distributorCodes := string_to_array(distributorCode, ',');

    -- Set search_path to the specified schema
    EXECUTE format('SET search_path TO %I', schemaName);

    DROP TABLE IF EXISTS temp_table;

    CREATE TEMP TABLE temp_table (
        "OrderRefNumber" character varying(50),
        "DistributorCode" character varying(50),
        "OrderDate" timestamp,
        "IsDeleted" boolean,
        "Status" character varying(50)
    );

    INSERT INTO temp_table ("OrderRefNumber", "DistributorCode", "OrderDate", "IsDeleted", "Status")
    SELECT soi."OrderRefNumber", soi."DistributorCode", soi."OrderDate", soi."IsDeleted", soi."Status"
    FROM "SO_OrderInformations" soi
    WHERE  
        soi."OrderDate" >= fromDate
        AND soi."OrderDate" <= toDate
        AND soi."Status" IN ('SO_ST_DELIVERED', 'SO_ST_PARTIALDELIVERED', 'SO_ST_WAITINGSHIPPING', 'SO_ST_SHIPPING')
        AND NOT (soi."IsDeleted")
        AND soi."DistributorCode" IN (SELECT unnest(distributorCodes));

    RETURN QUERY
    SELECT
        soi."DistributorCode"::character varying(50) as "DistributorCode",
        si."InventoryID"::character varying(50) as "InventoryID",
        MAX(si."ItemDescription")::character varying(250) as "ItemDescription",
        si."UOM"::character varying(50) as "UOM", 
        SUM(si."OrderBaseQuantities")::numeric as "OrderQTY",
        SUM(si."ShippedBaseQuantities")::numeric as "ActualShippedQTY"
    FROM
        temp_table soi
    INNER JOIN
        "SO_OrderItems" si ON soi."OrderRefNumber" = si."OrderRefNumber"
    WHERE
        (itemGroupCode IS NULL OR si."ItemGroupCode" = itemGroupCode)
        AND soi."OrderDate" >= fromDate
        AND soi."OrderDate" <= toDate
        AND soi."Status" IN ('SO_ST_DELIVERED', 'SO_ST_PARTIALDELIVERED', 'SO_ST_WAITINGSHIPPING', 'SO_ST_SHIPPING')
        AND NOT (soi."IsDeleted")
        AND NOT (si."IsDeleted")
        AND soi."DistributorCode" IN (SELECT unnest(distributorCodes))
    GROUP BY
        soi."DistributorCode",
        si."InventoryID",
        si."UOM";
    
    -- Reset search_path to its default value
    EXECUTE 'RESET search_path';
END $func$;

select * from ODReportShippingStatus('p0401241', null, '2024-01-01','2024-03-16', 'osp0401241');