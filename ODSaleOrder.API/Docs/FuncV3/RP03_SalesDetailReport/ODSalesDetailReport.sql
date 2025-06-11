DROP FUNCTION ODSalesDetailReport (
		distributorCode VARCHAR,
		routeZoneCode VARCHAR,
		status VARCHAR, 
		fromDate timestamp,
		toDate timestamp,
		schemaName VARCHAR
);
CREATE OR REPLACE FUNCTION ODSalesDetailReport (
		distributorCode VARCHAR,
		routeZoneCode VARCHAR,
		status VARCHAR, 
		fromDate timestamp,
		toDate timestamp,
		schemaName VARCHAR
) RETURNS TABLE (
     "DistributorCode" character varying(50),
     "TotalAmount" numeric,
     "VAT" numeric,
     "DiscountAmount" numeric,
     "Revenue" numeric
) LANGUAGE plpgsql as $func$
BEGIN
  -- Set search_path to the specified schema
  EXECUTE format('SET search_path TO %I', schemaName);
  RETURN QUERY
		select soi."DistributorCode"::character varying(50) as "DistributorCode",
				round(sum(soi."Shipped_Amt")::numeric, 2) as "TotalAmount",
				round(sum(soi."TotalVAT")::numeric, 2) as "VAT",
				round(sum(soi."Shipped_Disc_Amt")::numeric, 2) as "DiscountAmount",
				round(sum(soi."Shipped_Extend_Amt")::numeric, 2) as "Revenue"
		from "SO_OrderInformations" soi 
		where 
			soi."OrderDate" >= fromDate
			AND soi."OrderDate" <= toDate
			AND soi."DistributorCode" = distributorCode 
			AND NOT (soi."IsDeleted")
			AND COALESCE(soi."Status" IN (SELECT unnest(string_to_array(status, ','))), FALSE)
			AND CASE WHEN routeZoneCode IS NULL THEN '1' ELSE soi."RouteZoneID" END = CASE WHEN routeZoneCode IS NULL THEN '1' ELSE routeZoneCode END 
		group by soi."DistributorCode";
	-- Reset search_path to its default value
	EXECUTE 'RESET search_path';
END $func$;

select * from ODSalesDetailReport('p0401241', null, 'SO_ST_OPEN,SO_ST_SHIPPING,SO_ST_WAITINGSHIPPING,SO_ST_DELIVERED,SO_ST_PARTIALDELIVERED,SO_ST_FAILED,SO_ST_CONFIRM,SO_ST_COMPLETE_DRAFT','2024-01-01','2024-03-16', 'osp0401241');