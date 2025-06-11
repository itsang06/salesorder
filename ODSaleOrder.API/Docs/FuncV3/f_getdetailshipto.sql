DROP FUNCTION f_getdetailshipto(character varying,uuid);
CREATE OR REPLACE FUNCTION public.f_getdetailshipto(
  distributorcode character varying,
  shiptoid uuid
)
RETURNS TABLE(
  "Id" uuid,
  "ShiptoCode" character varying,
  "ShiptoName" character varying,
  "ShiptoAddress" character varying,
  "Shipto_AttributeId1" uuid,
  "Shipto_Attribute1" character varying,
  "Shipto_AttributeDesc1" character varying,
  "Shipto_AttributeId2" uuid,
  "Shipto_Attribute2" character varying,
  "Shipto_AttributeDesc2" character varying,
  "Shipto_AttributeId3" uuid,
  "Shipto_Attribute3" character varying,
  "Shipto_AttributeDesc3" character varying,
  "Shipto_AttributeId4" uuid,
  "Shipto_Attribute4" character varying,
  "Shipto_AttributeDesc4" character varying,
  "Shipto_AttributeId5" uuid,
  "Shipto_Attribute5" character varying,
  "Shipto_AttributeDesc5" character varying,
  "Shipto_AttributeId6" uuid,
  "Shipto_Attribute6" character varying,
  "Shipto_AttributeDesc6" character varying,
  "Shipto_AttributeId7" uuid,
  "Shipto_Attribute7" character varying,
  "Shipto_AttributeDesc7" character varying,
  "Shipto_AttributeId8" uuid,
  "Shipto_Attribute8" character varying,
  "Shipto_AttributeDesc8" character varying,
  "Shipto_AttributeId9" uuid,
  "Shipto_Attribute9" character varying,
  "Shipto_AttributeDesc9" character varying,
  "Shipto_AttributeId10" uuid,
  "Shipto_Attribute10" character varying,
  "Shipto_AttributeDesc10" character varying
) 
LANGUAGE plpgsql
AS $function$
DECLARE 
  schemaName VARCHAR(100);
  executeQuery TEXT;
BEGIN
  schemaName := (
    SELECT TRIM(schema."SchemaName") 
    FROM "ODDistributorSchemas" schema 
    WHERE schema."DistributorCode" = distributorcode 
    AND schema."IsDeleted" = FALSE 
    LIMIT 1
  );

  -- Kiểm tra nếu schema tồn tại
  IF schemaName IS NOT NULL AND LENGTH(schemaName) > 0 THEN
    IF EXISTS(SELECT schema_name FROM information_schema.schemata WHERE schema_name = schemaName) THEN
      executeQuery := FORMAT(
        '
        WITH AttributeMapping AS (
          SELECT DISTINCT
            dms."CustomerShiptoId",
            setting."AttributeID",
            attribute."Id" AS "AttributeValueId",
            attribute."Code" AS "AttributeValueCode",
            attribute."Description" AS "AttributeValueDescription"
          FROM app_union_public(%L, NULL::"CustomerDmsAttribute") dms
          INNER JOIN app_union_public(%L, NULL::"CustomerAttributes") attribute 
            ON attribute."Id" = dms."CustomerAttributeId"
            AND now() >= attribute."EffectiveDate" 
            AND (attribute."ValidUntil" >= now() OR attribute."ValidUntil" IS NULL)
          INNER JOIN app_union_public(%L, NULL::"CustomerSettings") setting 
            ON setting."Id" = attribute."CustomerSettingId"
        )
        
        SELECT DISTINCT
          shipto."Id"::uuid AS "Id",
          shipto."ShiptoCode"::varchar AS "ShiptoCode",
          shipto."ShiptoName"::varchar AS "ShiptoName",
          shipto."Address"::varchar AS "ShiptoAddress",
          
          COALESCE((SELECT "AttributeValueId" FROM AttributeMapping WHERE "AttributeID" = ''CUS01'' AND "CustomerShiptoId" = shipto."Id"), ''00000000-0000-0000-0000-000000000000'')::uuid AS "Shipto_AttributeId1",
          COALESCE((SELECT "AttributeValueCode" FROM AttributeMapping WHERE "AttributeID" = ''CUS01'' AND "CustomerShiptoId" = shipto."Id"), NULL)::varchar AS "Shipto_Attribute1",
          COALESCE((SELECT "AttributeValueDescription" FROM AttributeMapping WHERE "AttributeID" = ''CUS01'' AND "CustomerShiptoId" = shipto."Id"), NULL)::varchar AS "Shipto_AttributeDesc1",
          
          COALESCE((SELECT "AttributeValueId" FROM AttributeMapping WHERE "AttributeID" = ''CUS02'' AND "CustomerShiptoId" = shipto."Id"), ''00000000-0000-0000-0000-000000000000'')::uuid AS "Shipto_AttributeId2",
          COALESCE((SELECT "AttributeValueCode" FROM AttributeMapping WHERE "AttributeID" = ''CUS02'' AND "CustomerShiptoId" = shipto."Id"), NULL)::varchar AS "Shipto_Attribute2",
          COALESCE((SELECT "AttributeValueDescription" FROM AttributeMapping WHERE "AttributeID" = ''CUS02'' AND "CustomerShiptoId" = shipto."Id"), NULL)::varchar AS "Shipto_AttributeDesc2",
          
          COALESCE((SELECT "AttributeValueId" FROM AttributeMapping WHERE "AttributeID" = ''CUS03'' AND "CustomerShiptoId" = shipto."Id"), ''00000000-0000-0000-0000-000000000000'')::uuid AS "Shipto_AttributeId3",
          COALESCE((SELECT "AttributeValueCode" FROM AttributeMapping WHERE "AttributeID" = ''CUS03'' AND "CustomerShiptoId" = shipto."Id"), NULL)::varchar AS "Shipto_Attribute3",
          COALESCE((SELECT "AttributeValueDescription" FROM AttributeMapping WHERE "AttributeID" = ''CUS03'' AND "CustomerShiptoId" = shipto."Id"), NULL)::varchar AS "Shipto_AttributeDesc3",
          
          COALESCE((SELECT "AttributeValueId" FROM AttributeMapping WHERE "AttributeID" = ''CUS04'' AND "CustomerShiptoId" = shipto."Id"), ''00000000-0000-0000-0000-000000000000'')::uuid AS "Shipto_AttributeId4",
          COALESCE((SELECT "AttributeValueCode" FROM AttributeMapping WHERE "AttributeID" = ''CUS04'' AND "CustomerShiptoId" = shipto."Id"), NULL)::varchar AS "Shipto_Attribute4",
          COALESCE((SELECT "AttributeValueDescription" FROM AttributeMapping WHERE "AttributeID" = ''CUS04'' AND "CustomerShiptoId" = shipto."Id"), NULL)::varchar AS "Shipto_AttributeDesc4",
          
          COALESCE((SELECT "AttributeValueId" FROM AttributeMapping WHERE "AttributeID" = ''CUS05'' AND "CustomerShiptoId" = shipto."Id"), ''00000000-0000-0000-0000-000000000000'')::uuid AS "Shipto_AttributeId5",
          COALESCE((SELECT "AttributeValueCode" FROM AttributeMapping WHERE "AttributeID" = ''CUS05'' AND "CustomerShiptoId" = shipto."Id"), NULL)::varchar AS "Shipto_Attribute5",
          COALESCE((SELECT "AttributeValueDescription" FROM AttributeMapping WHERE "AttributeID" = ''CUS05'' AND "CustomerShiptoId" = shipto."Id"), NULL)::varchar AS "Shipto_AttributeDesc5",
          
          COALESCE((SELECT "AttributeValueId" FROM AttributeMapping WHERE "AttributeID" = ''CUS06'' AND "CustomerShiptoId" = shipto."Id"), ''00000000-0000-0000-0000-000000000000'')::uuid AS "Shipto_AttributeId6",
          COALESCE((SELECT "AttributeValueCode" FROM AttributeMapping WHERE "AttributeID" = ''CUS06'' AND "CustomerShiptoId" = shipto."Id"), NULL)::varchar AS "Shipto_Attribute6",
          COALESCE((SELECT "AttributeValueDescription" FROM AttributeMapping WHERE "AttributeID" = ''CUS06'' AND "CustomerShiptoId" = shipto."Id"), NULL)::varchar AS "Shipto_AttributeDesc6",
          
          COALESCE((SELECT "AttributeValueId" FROM AttributeMapping WHERE "AttributeID" = ''CUS07'' AND "CustomerShiptoId" = shipto."Id"), ''00000000-0000-0000-0000-000000000000'')::uuid AS "Shipto_AttributeId7",
          COALESCE((SELECT "AttributeValueCode" FROM AttributeMapping WHERE "AttributeID" = ''CUS07'' AND "CustomerShiptoId" = shipto."Id"), NULL)::varchar AS "Shipto_Attribute7",
          COALESCE((SELECT "AttributeValueDescription" FROM AttributeMapping WHERE "AttributeID" = ''CUS07'' AND "CustomerShiptoId" = shipto."Id"), NULL)::varchar AS "Shipto_AttributeDesc7",

          COALESCE((SELECT "AttributeValueId" FROM AttributeMapping WHERE "AttributeID" = ''CUS08'' AND "CustomerShiptoId" = shipto."Id"), ''00000000-0000-0000-0000-000000000000'')::uuid AS "Shipto_AttributeId8",
          COALESCE((SELECT "AttributeValueCode" FROM AttributeMapping WHERE "AttributeID" = ''CUS08'' AND "CustomerShiptoId" = shipto."Id"), NULL)::varchar AS "Shipto_Attribute8",
          COALESCE((SELECT "AttributeValueDescription" FROM AttributeMapping WHERE "AttributeID" = ''CUS08'' AND "CustomerShiptoId" = shipto."Id"), NULL)::varchar AS "Shipto_AttributeDesc8",
          
          COALESCE((SELECT "AttributeValueId" FROM AttributeMapping WHERE "AttributeID" = ''CUS09'' AND "CustomerShiptoId" = shipto."Id"), ''00000000-0000-0000-0000-000000000000'')::uuid AS "Shipto_AttributeId9",
          COALESCE((SELECT "AttributeValueCode" FROM AttributeMapping WHERE "AttributeID" = ''CUS09'' AND "CustomerShiptoId" = shipto."Id"), NULL)::varchar AS "Shipto_Attribute9",
          COALESCE((SELECT "AttributeValueDescription" FROM AttributeMapping WHERE "AttributeID" = ''CUS09'' AND "CustomerShiptoId" = shipto."Id"), NULL)::varchar AS "Shipto_AttributeDesc9",
          
          COALESCE((SELECT "AttributeValueId" FROM AttributeMapping WHERE "AttributeID" = ''CUS10'' AND "CustomerShiptoId" = shipto."Id"), ''00000000-0000-0000-0000-000000000000'')::uuid AS "Shipto_AttributeId10",
          COALESCE((SELECT "AttributeValueCode" FROM AttributeMapping WHERE "AttributeID" = ''CUS10'' AND "CustomerShiptoId" = shipto."Id"), NULL)::varchar AS "Shipto_Attribute10",
          COALESCE((SELECT "AttributeValueDescription" FROM AttributeMapping WHERE "AttributeID" = ''CUS10'' AND "CustomerShiptoId" = shipto."Id"), NULL)::varchar AS "Shipto_AttributeDesc10"
        
        FROM app_union_public(%L, NULL::"CustomerShiptos") shipto
        WHERE shipto."DeleteFlag" = 0
        AND shipto."Id" = %L
        ',
        distributorcode, distributorcode, distributorcode, distributorcode, shiptoid
      );

      RETURN QUERY EXECUTE executeQuery;
    END IF;
  END IF;
END
$function$;

SELECT * FROM  "public"."f_getdetailshipto"('20240710', '0eb8621f-5674-4612-bbbd-dd7f90d847a5');

-- SELECT * FROM public.f_getdetailshipto('p0401241', 'e43f2f1c-57d8-42d2-8987-39b67d64d4cb');
