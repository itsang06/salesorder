DROP FUNCTION SalesSynthesisReport (
	distributorCode VARCHAR,
	routeZoneCode VARCHAR,
	reportType VARCHAR,
	fromDate timestamp,
	toDate timestamp
);
CREATE OR REPLACE FUNCTION SalesSynthesisReport (
    distributorCode VARCHAR,
    routeZoneCode VARCHAR,
    reportType VARCHAR,
    fromDate timestamp,
    toDate timestamp
) RETURNS TABLE (
    "DistributorCode" character varying(50),
    "RouteZoneID" character varying(50),
    "OrderDate" date,
    "Amount" numeric
) LANGUAGE plpgsql AS $func$
BEGIN
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
END $func$;

select * from SalesSynthesisReport('1000122', null, 'Revenue', '2023-10-01', '2023-12-31');