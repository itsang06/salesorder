DROP FUNCTION ODSalesSynthesisReport (
	distributorCode VARCHAR,
	routeZoneCode VARCHAR,
	reportType VARCHAR,
	fromDate timestamp,
	toDate timestamp,
    schemaName VARCHAR
);
CREATE OR REPLACE FUNCTION ODSalesSynthesisReport (
    distributorCode VARCHAR,
    routeZoneCode VARCHAR,
    reportType VARCHAR,
    fromDate timestamp,
    toDate timestamp,
    schemaName VARCHAR
) RETURNS TABLE (
    "DistributorCode" character varying(50),
    "RouteZoneID" character varying(50),
    "OrderDate" date,
    "Amount" numeric
) LANGUAGE plpgsql AS $func$
BEGIN
    -- Set search_path to the specified schema
    EXECUTE format('SET search_path TO %I', schemaName);
    RETURN QUERY
    SELECT
        soi."DistributorCode"::character varying(50) as "DistributorCode",
        soi."RouteZoneID"::character varying(50) as "RouteZoneID",
        soi."OrderDate"::date as "OrderDate",
        CASE 
            WHEN reportType = 'Revenue' THEN 
                SUM(CASE WHEN soi."OrderType" = 'SalesOrder' THEN soi."Shipped_Extend_Amt" ELSE 0 END) - 
                SUM(CASE WHEN soi."OrderType" = 'ReturnOrder' THEN soi."Ord_Extend_Amt" ELSE 0 END)::numeric
            ELSE
                SUM(CASE WHEN soi."OrderType" = 'SalesOrder' THEN soi."Shipped_Amt" ELSE 0 END) - 
                SUM(CASE WHEN soi."OrderType" = 'ReturnOrder' THEN soi."Ord_Amt" ELSE 0 END)::numeric
        END as "Amount"
    FROM
        "SO_OrderInformations" soi
    WHERE
        (soi."Status" IN ('SO_ST_DELIVERED', 'SO_ST_PARTIALDELIVERED') OR 
        (soi."OrderType" = 'ReturnOrder' AND soi."Status" = 'SO_ST_CONFIRM'))
        AND COALESCE(soi."DistributorCode" IN (SELECT UNNEST(string_to_array(distributorCode, ','))), FALSE) 
        AND COALESCE(soi."RouteZoneID" = routeZoneCode, routeZoneCode IS NULL)
        AND soi."OrderDate" >= fromDate
	    AND soi."OrderDate" <= toDate
        AND NOT (soi."IsDeleted")
    GROUP BY
        soi."DistributorCode", soi."RouteZoneID", soi."OrderDate";

    -- Reset search_path to its default value
	EXECUTE 'RESET search_path';
END $func$;

select * from ODSalesSynthesisReport('p0401241', null, 'Revenue', '2024-01-01', '2024-03-16', 'osp0401241');