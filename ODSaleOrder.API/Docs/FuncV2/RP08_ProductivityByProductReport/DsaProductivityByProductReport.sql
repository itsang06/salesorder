CREATE OR REPLACE FUNCTION public.DsaProductivityByProductReport(
	itemAttribute character varying, 
	distributorCode character varying, 
	status character varying, 
	fromDate timestamp, 
	toDate timestamp
) RETURNS TABLE(
	DSAID character varying, 
	InventoryAttibute character varying, 
	Qty integer) LANGUAGE plpgsql AS $func$
	
declare
	distributorCodes TEXT[];
	statuses TEXT[];
begin
	distributorCodes:=  string_to_array(distributorCode, ',');
	statuses:=  string_to_array(status, ',');

	DROP TABLE IF EXISTS temp_table;
	CREATE TEMP TABLE temp_table (
	    "OrderRefNumber" character varying(50),
	    "DistributorCode" character varying(50),
	    "DSAID" character varying(50),
	    "OrderDate" timestamp,
	    "IsDeleted" boolean,
	    "Status" character varying(50)
	);

	INSERT INTO temp_table ("OrderRefNumber", "DistributorCode", "DSAID", "OrderDate", "IsDeleted", "Status")
	SELECT soi."OrderRefNumber", soi."DistributorCode", soi."DSAID", soi."OrderDate", soi."IsDeleted", soi."Status"
	FROM "SO_OrderInformations" soi
	WHERE 
		soi."OrderDate" >= fromDate
     AND soi."OrderDate" <= toDate
     AND soi."DSAID" IS NOT NULL
     AND NOT (soi."IsDeleted")
     AND COALESCE(soi."Status" IN (SELECT unnest(statuses)), FALSE)
     AND COALESCE(soi."DistributorCode" IN (SELECT unnest(distributorCodes)), FALSE);
       
     RETURN QUERY
     SELECT 
        s."DSAID"::character varying(50) as DSAID,
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
     WHERE s."OrderDate" >= fromDate
     AND s."OrderDate" <= toDate
     AND s."DSAID" IS NOT NULL
     AND NOT (so."IsFree") AND NOT (s."IsDeleted")
     AND NOT (so."IsDeleted")
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
        END), s."DSAID";
END $func$;

select * from DsaProductivityByProductReport('IT02','1000122', 'SO_ST_CANCEL,SO_ST_DELIVERED','2023-01-01 00:00:00','2023-12-31 23:59:59');