CREATE OR REPLACE FUNCTION "public"."f_get_promotion_attribute_summary_v5"("p_distributor_code" text, "p_promotion_codes" _text)
  RETURNS TABLE("levelid" varchar, "leveldesc" text, "promotionid" varchar, "promotionname" varchar, "promotiontype" varchar, "orderrule" varchar, "levelorderqty" int4, "levelorderamount" numeric, "attcode" varchar, "attcodename" varchar, "orderby" text, "isapplybudget" bool, "budgetqtycode" varchar, "budgetvaluecode" varchar, "minvalue" numeric, "conversionfactor" int4, "packinguomid" varchar, "baseunit" varchar, "ruletype" varchar, "productcodes" text, "totalrequiredbaseunit" int4, "uomtype" varchar, "skufullcase" bool, "productinfo" text) AS $BODY$
BEGIN
  RETURN QUERY

  WITH PromotionDetailsList AS (
    SELECT tp.*
    FROM public.f_getpromotiondetailsbydistributor(p_distributor_code, p_promotion_codes) tp
    WHERE tp."PromotionType" IN ('Line', 'Group') AND tp."IsSalesProduct" = true
  ),
  GetLevelApplyByAttribute AS (
    SELECT DISTINCT
      tp."LevelId"::varchar,
      tp."LevelDesc"::text,
      tp."PromotionId"::varchar,
      tp."PromotionName"::varchar,
      tp."PromotionType"::varchar,
      tp."OrderRule"::varchar,
      tp."LevelOrderQty",
      tp."LevelOrderAmount",
      tp."AttCode"::varchar,
      tp."AttCodeName"::varchar,
      tp."OrderBy"::text,
      tp."IsApplyBudget",
      tp."BudgetQtyCode"::varchar,
      tp."BudgetValueCode"::varchar,
      tp."RequiredMinQty",
      tp."MinValue"::numeric,
      CASE 
        WHEN tp."UomType" = 'PurchaseUnit' THEN item."PurchaseUnit"
        WHEN tp."UomType" = 'SalesUnit' THEN item."SalesUnit"
        ELSE item."BaseUnit"
      END::varchar AS "PackingUomId",
      item."BaseUnit"::varchar,
      item."ConversionFactor"::int4 AS "ConversionFactor",
      item."InventoryItemCode"::varchar AS "ProductCode",
      tp."UomType"::varchar,
      tp."SkuFullCase"
    FROM PromotionDetailsList tp
    INNER JOIN public."VVInventoryItems" item ON (
      (item."Attribute1" = tp."AttId" AND item."AttributeCode1" = tp."AttCode") OR
      (item."Attribute2" = tp."AttId" AND item."AttributeCode2" = tp."AttCode") OR
      (item."Attribute3" = tp."AttId" AND item."AttributeCode3" = tp."AttCode") OR
      (item."Attribute4" = tp."AttId" AND item."AttributeCode4" = tp."AttCode") OR
      (item."Attribute5" = tp."AttId" AND item."AttributeCode5" = tp."AttCode") OR
      (item."Attribute6" = tp."AttId" AND item."AttributeCode6" = tp."AttCode") OR
      (item."Attribute7" = tp."AttId" AND item."AttributeCode7" = tp."AttCode") OR
      (item."Attribute8" = tp."AttId" AND item."AttributeCode8" = tp."AttCode") OR
      (item."Attribute9" = tp."AttId" AND item."AttributeCode9" = tp."AttCode") OR
      (item."Attribute10" = tp."AttId" AND item."AttributeCode10" = tp."AttCode")
    )
    WHERE tp."ProductType" = 'Attribute'
      AND item."FromUnit" = (
        CASE 
          WHEN tp."UomType" = 'PurchaseUnit' THEN item."PurchaseUnit"
          WHEN tp."UomType" = 'SalesUnit' THEN item."SalesUnit"
          ELSE item."BaseUnit"
        END)
      AND item."ToUnit" = item."BaseUnit"
      AND item."Competitor" = false
      AND item."ItemType" = 'STOCK'
  ),
  promo_classification AS (
    SELECT "PromotionId", MAX(CASE WHEN "RequiredMinQty" IS TRUE THEN 1 ELSE 0 END) AS has_required_min
    FROM GetLevelApplyByAttribute
    GROUP BY "PromotionId"
  ),
  product_info_nomins AS (
    SELECT 
      "PromotionId", 
      "LevelId", 
      "OrderRule",
      "AttCode",
      ('[' || STRING_AGG(DISTINCT FORMAT('("%s","%s",%s)', "ProductCode", "PackingUomId", "ConversionFactor"), ',') || ']')::text AS "ProductInfoNoMin"
    FROM GetLevelApplyByAttribute
    WHERE "RequiredMinQty" IS NOT TRUE
    GROUP BY "PromotionId", "LevelId", "OrderRule", "AttCode"
  ),
  group_has_required_min AS (
    SELECT
      b."LevelId"::varchar,
      b."LevelDesc"::text,
      A."PromotionId"::varchar,
      b."PromotionName"::varchar,
      b."PromotionType"::varchar,
      b."OrderRule"::varchar,
      b."LevelOrderQty",
      b."LevelOrderAmount",
      b."AttCode"::varchar,
      b."AttCodeName"::varchar,
      b."OrderBy"::text,
      b."IsApplyBudget",
      b."BudgetQtyCode"::varchar,
      b."BudgetValueCode"::varchar,
      MIN(b."MinValue") AS "MinValue",
      MAX(b."ConversionFactor") AS "ConversionFactor",
      STRING_AGG(DISTINCT b."PackingUomId", ',')::varchar AS "PackingUomId",
      MAX(b."BaseUnit")::varchar AS "BaseUnit",
      'RequireMin'::varchar AS "RuleType",
      array_to_string(array_agg(DISTINCT b."ProductCode"), ',')::text AS "ProductCodes",
      ROUND(MIN(b."MinValue") * MAX(b."ConversionFactor"))::int AS "TotalRequiredBaseUnit",
      MAX(b."UomType")::varchar AS "UomType",
      BOOL_OR(b."SkuFullCase") AS "SkuFullCase",
      COALESCE(
        nonmin."ProductInfoNoMin",
        ('[' || STRING_AGG(DISTINCT FORMAT('("%s","%s",%s)', allp."ProductCode", allp."PackingUomId", allp."ConversionFactor"), ',') || ']')::text
      ) AS "ProductInfo"
    FROM promo_classification A
    JOIN GetLevelApplyByAttribute b ON A."PromotionId" = b."PromotionId"
    LEFT JOIN GetLevelApplyByAttribute allp ON allp."PromotionId" = b."PromotionId" AND allp."LevelId" = b."LevelId"
      AND allp."OrderRule" = b."OrderRule" AND allp."AttCode" = b."AttCode"
    LEFT JOIN product_info_nomins nonmin ON nonmin."PromotionId" = b."PromotionId"
      AND nonmin."LevelId" = b."LevelId"
      AND nonmin."OrderRule" = b."OrderRule"
      AND nonmin."AttCode" = b."AttCode"
    WHERE A.has_required_min = 1 AND b."RequiredMinQty" IS TRUE
    GROUP BY A."PromotionId", b."LevelId", b."OrderRule", b."AttCode",
      b."LevelDesc", b."PromotionName", b."PromotionType", b."LevelOrderQty", b."LevelOrderAmount",
      b."AttCodeName", b."OrderBy", b."IsApplyBudget", b."BudgetQtyCode", b."BudgetValueCode",
      nonmin."ProductInfoNoMin"
  ),
  group_no_required_min AS (
    SELECT
      b."LevelId"::varchar,
      b."LevelDesc"::text,
      A."PromotionId"::varchar,
      b."PromotionName"::varchar,
      b."PromotionType"::varchar,
      b."OrderRule"::varchar,
      b."LevelOrderQty",
      b."LevelOrderAmount",
      b."AttCode"::varchar,
      b."AttCodeName"::varchar,
      b."OrderBy"::text,
      b."IsApplyBudget",
      b."BudgetQtyCode"::varchar,
      b."BudgetValueCode"::varchar,
      NULL::numeric AS "MinValue",
      MAX(b."ConversionFactor") AS "ConversionFactor",
      STRING_AGG(DISTINCT b."PackingUomId", ',')::varchar AS "PackingUomId",
      MAX(b."BaseUnit")::varchar AS "BaseUnit",
      'NoRequireMin'::varchar AS "RuleType",
      array_to_string(array_agg(DISTINCT b."ProductCode"), ',')::text AS "ProductCodes",
      NULL::int AS "TotalRequiredBaseUnit",
      MAX(b."UomType")::varchar AS "UomType",
      BOOL_OR(b."SkuFullCase") AS "SkuFullCase",
      ('[' || STRING_AGG(DISTINCT FORMAT('("%s","%s",%s)', allp."ProductCode", allp."PackingUomId", allp."ConversionFactor"), ',') || ']')::text AS "ProductInfo"
    FROM promo_classification A
    JOIN GetLevelApplyByAttribute b ON A."PromotionId" = b."PromotionId"
    LEFT JOIN GetLevelApplyByAttribute allp ON allp."PromotionId" = b."PromotionId" AND allp."LevelId" = b."LevelId"
      AND allp."OrderRule" = b."OrderRule" AND allp."AttCode" = b."AttCode"
    LEFT JOIN product_info_nomins nonmin ON nonmin."PromotionId" = b."PromotionId"
      AND nonmin."LevelId" = b."LevelId"
      AND nonmin."OrderRule" = b."OrderRule"
      AND nonmin."AttCode" = b."AttCode"
    WHERE A.has_required_min = 0
    GROUP BY A."PromotionId", b."LevelId", b."OrderRule", b."AttCode",
      b."LevelDesc", b."PromotionName", b."PromotionType", b."LevelOrderQty", b."LevelOrderAmount",
      b."AttCodeName", b."OrderBy", b."IsApplyBudget", b."BudgetQtyCode", b."BudgetValueCode"
  )

  SELECT * FROM group_has_required_min
  UNION ALL
  SELECT * FROM group_no_required_min;

END;
$BODY$
  LANGUAGE plpgsql VOLATILE
  COST 100
  ROWS 1000