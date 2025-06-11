DROP FUNCTION f_getemployeesbyshipto(character varying,character varying,character varying);
CREATE OR REPLACE FUNCTION public.f_getemployeesbyshipto(
	distributorcode character varying, 
	customercode character varying,
  shiptocode character varying
)
RETURNS TABLE(
	"EmployeeCode" character varying, 
	"EmployeeName" character varying, 
	"RouteZoneCode" character varying, 
	"RouteZoneDesc" character varying, 
	"RouteZoneLocation" character varying,
	"RouteZOneType" character varying,
	"DsaCode" character varying, 
	"DistributorCode" character varying, 
	"BeatPlanCode" character varying,
	"BeatPlanName" character varying,
	"FullAddress" text, 
	"JobTitle" character varying, 
	"MainPhoneNumber" character varying, 
	"DateOfBirth" timestamp without time zone, 
	"Idcard" character varying, 
	"Gender" character varying
)
LANGUAGE plpgsql
AS $function$
DECLARE 
	schemaName VARCHAR(100);
	excuteQuery text;
BEGIN
	schemaName := (SELECT schema."SchemaName" FROM "ODDistributorSchemas" schema WHERE schema."DistributorCode" = distributorcode AND schema."IsDeleted" = FALSE LIMIT 1);
	schemaname := TRIM(schemaName);
	IF schemaName IS NOT NULL AND LENGTH(schemaName) > 0 THEN
		IF EXISTS(SELECT schema_name FROM information_schema.schemata WHERE schema_name = schemaname)
      THEN
				excuteQuery := 
				'WITH "PrincipalEmployees" AS (
					SELECT 
						em."EmployeeCode", 
						em."FullName" as "EmployeeName", 
						rz."RouteZoneCode"::VARCHAR as "RouteZoneCode", 
						rz."Description"::VARCHAR as "RouteZoneDesc",
						rz."LocationCode"::VARCHAR as "RouteZoneLocation",
						rz."RouteZoneTypeCode"::VARCHAR as "RouteZOneType",
						rz."DSACode"::VARCHAR as "DsaCode", 
						''' || distributorcode ||'''::VARCHAR as "DistributorCode",
						bp."BeatPlanCode"::VARCHAR as "BeatPlanCode",
						bp."Description"::VARCHAR as "BeatPlanName",
						em."FullAddress", em."JobTitle", em."MainPhoneNumber", 
						em."DateOfBirth", em."Idcard", em."Gender"
					FROM "public"."RZ_BeatPlanEmployees" emp
					INNER JOIN "public"."PrincipleEmployees" em ON em."EmployeeCode" = emp."EmployeeCode" AND (now() >= em."StartWorkingDate" AND (em."TerminateDate"  >= now() OR em."TerminateDate" is null))
					INNER JOIN "public"."RZ_BeatPlans" bp ON bp."BeatPlanCode" = emp."BeatPlanCode" AND UPPER(bp."Status") = ''RELEASED'' AND (now() >= bp."EffectiveDate" AND (bp."ValidUntil"  >= now() OR bp."ValidUntil" is null)) AND bp."IsDeleted" = ''f''
					INNER JOIN "public"."RZ_RouteZoneInfomations" rz 
						ON rz."RouteZoneCode" = bp."RouteZoneCode" 
						AND rz."Status" = ''Active'' 
						AND (now() >= rz."EffectiveDate" AND (rz."ValidUntil"  >= now() OR rz."ValidUntil" is null)) 
						AND rz."IsDeleted" = ''f''
						AND rz."DistributorCode" = ''' || distributorcode ||'''
					INNER JOIN "public"."RZ_RouteZoneShiptos" rzshipto 
						ON rzshipto."RouteZoneCode" = rz."RouteZoneCode"
						AND (now() >= rzshipto."EffectiveDate" AND (rzshipto."ValidUntil"  >= now() OR rzshipto."ValidUntil" IS NULL))
					INNER JOIN "public"."CustomerShiptos" shipto
						ON shipto."Id" = rzshipto."ShiptoId"
						AND shipto."ShiptoCode" = ''' || shiptocode ||'''
					INNER JOIN "public"."CustomerInformations" cus 
						ON cus."Id" = shipto."CustomerInfomationId"
						AND cus."DeleteFlag" = 0
						AND cus."CustomerCode" = ''' || customercode ||'''
					WHERE emp."IsCurrent" = ''t'' AND emp."IsDeleted" = ''f''
				),

				"DistributorEmployees" AS (
					SELECT 
						em."EmployeeCode", 
						em."FullName" as "EmployeeName", 
						rz."RouteZoneCode"::VARCHAR as "RouteZoneCode", 
						rz."Description"::VARCHAR as "RouteZoneDesc",
						rz."LocationCode"::VARCHAR as "RouteZoneLocation",
						rz."RouteZoneTypeCode"::VARCHAR as "RouteZOneType",
						rz."DSACode"::VARCHAR as "DsaCode", 
						rz."DistributorCode"::VARCHAR as "DistributorCode", 
						bp."BeatPlanCode"::VARCHAR as "BeatPlanCode",
						bp."Description"::VARCHAR as "BeatPlanName",
						em."FullAddress", em."JobTitle", em."MainPhoneNumber", 
						em."DateOfBirth", em."Idcard", em."Gender"
					FROM '|| quote_ident(schemaName) ||'."RZ_BeatPlanEmployees" emp
					INNER JOIN '|| quote_ident(schemaName) ||'."PrincipleEmployees" em ON em."EmployeeCode" = emp."EmployeeCode" AND (now() >= em."StartWorkingDate" AND (em."TerminateDate"  >= now() OR em."TerminateDate" is null))
					INNER JOIN '|| quote_ident(schemaName) ||'."RZ_BeatPlans" bp ON bp."BeatPlanCode" = emp."BeatPlanCode" AND UPPER(bp."Status") = ''RELEASED'' AND (now() >= bp."EffectiveDate" AND (bp."ValidUntil"  >= now() OR bp."ValidUntil" is null))
					INNER JOIN '|| quote_ident(schemaName) ||'."RZ_RouteZoneInfomations" rz ON rz."RouteZoneCode" = bp."RouteZoneCode" AND rz."Status" = ''Active'' AND (now() >= rz."EffectiveDate" AND (rz."ValidUntil"  >= now() OR rz."ValidUntil" is null))
					INNER JOIN '|| quote_ident(schemaName) ||'."RZ_RouteZoneShiptos" rzshipto 
              ON rzshipto."RouteZoneCode" = rz."RouteZoneCode"
              AND (now() >= rzshipto."EffectiveDate" AND (rzshipto."ValidUntil"  >= now() OR rzshipto."ValidUntil" IS NULL))
					INNER JOIN '|| quote_ident(schemaName) ||'."CustomerShiptos" shipto
						ON shipto."Id" = rzshipto."ShiptoId"
						AND shipto."ShiptoCode" = ''' || shiptocode ||'''
					INNER JOIN '|| quote_ident(schemaName) ||'."CustomerInformations" cus 
						ON cus."Id" = shipto."CustomerInfomationId"
						AND cus."DeleteFlag" = 0
						AND cus."CustomerCode" = ''' || customercode ||'''
					WHERE emp."IsCurrent" = ''t'' AND emp."IsDeleted" = ''f''
				),

				"Final" AS (
					SELECT * FROM "PrincipalEmployees"
					UNION
					SELECT * from "DistributorEmployees"
				)

				SELECT DISTINCT * FROM "Final";';
			RETURN QUERY EXECUTE excuteQuery;
    END IF;
	END IF;			
END
$function$;

-- SELECT * FROM public.f_getemployeesbyshipto('20240710', '0000060326', 'S001');
SELECT * FROM public.f_getemployeesbyshipto('p0401241', 'C24700002', 'S01');