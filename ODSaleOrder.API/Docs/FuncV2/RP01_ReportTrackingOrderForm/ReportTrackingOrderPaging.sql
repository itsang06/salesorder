DROP FUNCTION ReportTrackingOrderPaging (
    distributorCode VARCHAR,
    salesRepID VARCHAR, 
    status VARCHAR, 
    fromDate timestamp,
    toDate timestamp,
    pageNumber INT,
    pageSize INT
);
CREATE OR REPLACE FUNCTION ReportTrackingOrderPaging (
    distributorCode VARCHAR,
    salesRepID VARCHAR, 
    status VARCHAR, 
    fromDate TIMESTAMP,
    toDate TIMESTAMP,
    pageNumber INT,
    pageSize INT
) RETURNS TABLE (
     "DistributorCode" VARCHAR(50),
     "OrderRefNumber" VARCHAR(50),
     "Status" VARCHAR(50),
     "SalesRepID" VARCHAR(50),
     "CustomerId" VARCHAR(50),
     "CustomerName" VARCHAR(250),
     "OrderDate" DATE,
     "TotalBaseQty" INT,
     "Amount" NUMERIC,
     "TotalPages" INT,
     "TotalCount" INT
) LANGUAGE plpgsql AS $func$
DECLARE
    offset_value INT;
    total_count INT;
    total_pages INT;
BEGIN
    offset_value := (pageNumber - 1) * pageSize;

    -- Count the total number of rows without pagination
    SELECT COUNT(*) INTO total_count
    FROM "SO_OrderInformations" soi 
    WHERE soi."OrderType" = 'SalesOrder'
        AND soi."DistributorCode" = distributorCode
        AND COALESCE(soi."Status" IN (SELECT unnest(string_to_array(status, ','))), FALSE)
        AND CASE 
                WHEN salesRepID IS NULL THEN '1' 
                ELSE soi."SalesRepID" 
            END = CASE 
                      WHEN salesRepID IS NULL THEN '1' 
                      ELSE salesRepID 
                  END
        AND soi."OrderDate" >= fromDate
        AND soi."OrderDate" <= toDate
        AND NOT (soi."IsDeleted");

    -- Calculate the total number of pages
    total_pages := CEIL(total_count::NUMERIC / pageSize);

    -- Return the main query result and total pages
    RETURN QUERY    
    SELECT 
        soi."DistributorCode"::VARCHAR(50),
        soi."OrderRefNumber"::VARCHAR(50),
        soi."Status"::VARCHAR(50),
        soi."SalesRepID"::VARCHAR(50),
        soi."CustomerId"::VARCHAR(50),
        soi."CustomerName"::VARCHAR(250),
        soi."OrderDate"::DATE,
        CASE 
            WHEN soi."Status" = 'SO_ST_DELIVERED' OR soi."Status" = 'SO_ST_PARTIALDELIVERED' 
            THEN soi."Shipped_Qty" 
            ELSE soi."Ord_Qty" 
        END::INT AS "TotalBaseQty",
        CASE 
            WHEN soi."Status" = 'SO_ST_DELIVERED' OR soi."Status" = 'SO_ST_PARTIALDELIVERED' 
            THEN soi."Shipped_Amt" 
            ELSE soi."Ord_Amt" 
        END::NUMERIC AS "Amount",
        total_pages,
        total_count
    FROM "SO_OrderInformations" soi 
    WHERE soi."OrderType" = 'SalesOrder'
        AND soi."DistributorCode" = distributorCode
        AND COALESCE(soi."Status" IN (SELECT unnest(string_to_array(status, ','))), FALSE)
        AND CASE 
                WHEN salesRepID IS NULL THEN '1' 
                ELSE soi."SalesRepID" 
            END = CASE 
                      WHEN salesRepID IS NULL THEN '1' 
                      ELSE salesRepID 
                  END
        AND soi."OrderDate" >= fromDate
        AND soi."OrderDate" <= toDate
        AND NOT (soi."IsDeleted")
    ORDER BY soi."OrderDate" DESC
    LIMIT pageSize
    OFFSET offset_value;
END $func$;

select * from ReportTrackingOrderPaging ('1000122',null, 'SO_ST_OPEN,SO_ST_SHIPPING,SO_ST_WAITINGSHIPPING,SO_ST_DELIVERED,SO_ST_PARTIALDELIVERED,SO_ST_FAILED,SO_ST_CONFIRM,SO_ST_COMPLETE_DRAFT','2023-10-01','2023-12-31', 2, 10);