CREATE OR REPLACE FUNCTION ReportShippingStatus (
    distributorCode VARCHAR,
    itemGroupCode VARCHAR, 
    fromDate timestamp,
    toDate timestamp
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
END $func$;

select * from ReportShippingStatus('1000122', null, '2023-10-01','2023-12-31');
select * from ReportShippingStatus('1000122', '0235P1T2V2', '2023-10-01', '2023-12-31');