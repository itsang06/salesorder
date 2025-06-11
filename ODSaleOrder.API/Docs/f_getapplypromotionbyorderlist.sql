CREATE OR REPLACE FUNCTION "public"."f_getapplypromotionbyorderlist"("distributorcode" varchar, "promotionlist" _varchar, "productlist" _varchar)
  RETURNS TABLE("LevelId" varchar, "LevelDesc" text, "PromotionId" varchar, "PromotionName" varchar, "PromotionType" varchar, "OrderRule" varchar, "ProductType" varchar, "OrderBy" text, "PackingUomId" varchar, "PackingUomName" text, "LevelOrderQty" int4, "LevelOrderAmount" numeric, "ProductCode" varchar, "ProductName" text, "AttId" varchar, "AttName" text, "AttCode" varchar, "AttCodeName" text, "OrderProductQty" int4, "IsGiftProduct" bool, "FreeItemType" varchar, "FreeAmountType" varchar, "RuleOfGiving" bool, "FreeAmount" numeric, "FreePercentAmount" numeric, "NumberOfFreeItem" int4, "IsDefaultProduct" bool, "AllowExchange" bool, "ExchangeRate" int4, "IsFreeProduct" bool, "IsSalesProduct" bool, "SicId" varchar, "LevelFreeQty" int4, "OnEach" float4, "DiscountType" varchar, "RequiredMinQty" bool, "MinValue" float4, "FreeSameProduct" bool, "IsApplyBudget" bool, "BudgetValueCode" varchar, "BudgetQtyCode" varchar, "IsBudgetQtyBookOver" bool, "IsBudgetValueBookOver" bool, "BaseUnit" varchar, "ConversionFactor" int4) AS $BODY$
BEGIN
	RETURN QUERY


					WITH "PromotionDetailsList" AS 
					(
							SELECT tp.* 
							FROM "public"."f_getpromotiondetailsbydistributor"('' || distributorcode ||'', promotionlist) tp
							WHERE tp."PromotionType" = 'Line'
					),

					"ProductList" AS (
						 SELECT item.* 
						 FROM "public"."VVInventoryItems" item
						 WHERE item."InventoryItemCode" = ANY($3)

					),

					"GetLevelApplyBySku" AS
					(
							SELECT tp.*, 
							item."BaseUnit",
							item."ConversionFactor"::int4 as "ConversionFactor"
							FROM "PromotionDetailsList" tp
							INNER JOIN "ProductList" item ON item."InventoryItemCode" = tp."ProductCode" 
							AND (item."FromUnit" = item."PurchaseUnit" OR item."FromUnit" = tp."PackingUomId") AND item."ToUnit" = item."BaseUnit"
							WHERE tp."ProductType" = 'Stock' AND tp."IsSalesProduct" = 't'
					),

					--SELECT * FROM "GetLevelApplyBySku";


					"GetLevelApplyByItemGroup" AS
					(
							SELECT tp.*,
							item."BaseUnit",
							item."ConversionFactor"::int4 as "ConversionFactor"
							FROM "PromotionDetailsList" tp
							INNER JOIN "ProductList" item ON item."ItemGroupCode" = tp."ProductCode" 
							AND (item."FromUnit" = item."PurchaseUnit" OR item."FromUnit" = tp."PackingUomId") AND item."ToUnit" = item."BaseUnit"
							WHERE tp."ProductType" = 'Group' AND tp."IsSalesProduct" = 't'
					),

					--SELECT * FROM "GetLevelApplyByItemGroup";

					"GetLevelApplyByAttribute" AS
					(
							SELECT tp.*,
							item."BaseUnit",
							item."ConversionFactor"::int4 as "ConversionFactor"
							FROM "PromotionDetailsList" tp
							INNER JOIN "ProductList" item ON item."InventoryItemCode" = tp."ProductCode" 
							AND (item."FromUnit" = item."PurchaseUnit" OR item."FromUnit" = tp."PackingUomId") AND item."ToUnit" = item."BaseUnit"
							WHERE tp."ProductType" = 'Attribute' AND tp."IsSalesProduct" = 't'
							AND 
							(
									(SELECT ARRAY_AGG(val ORDER BY val) FROM unnest(ARRAY[item."Attribute1", item."AttributeCode1"]) val) =
									(SELECT ARRAY_AGG(val ORDER BY val) FROM unnest(ARRAY[tp."AttId", tp."AttCode"]) val)
								OR 
									(SELECT ARRAY_AGG(val ORDER BY val) FROM unnest(ARRAY[item."Attribute2", item."AttributeCode2"]) val) =
									(SELECT ARRAY_AGG(val ORDER BY val) FROM unnest(ARRAY[tp."AttId", tp."AttCode"]) val)
								OR 
									(SELECT ARRAY_AGG(val ORDER BY val) FROM unnest(ARRAY[item."Attribute3", item."AttributeCode3"]) val) =
									(SELECT ARRAY_AGG(val ORDER BY val) FROM unnest(ARRAY[tp."AttId", tp."AttCode"]) val)
								OR 
									(SELECT ARRAY_AGG(val ORDER BY val) FROM unnest(ARRAY[item."Attribute4", item."AttributeCode4"]) val) =
									(SELECT ARRAY_AGG(val ORDER BY val) FROM unnest(ARRAY[tp."AttId", tp."AttCode"]) val)
								OR 
									(SELECT ARRAY_AGG(val ORDER BY val) FROM unnest(ARRAY[item."Attribute5", item."AttributeCode5"]) val) =
									(SELECT ARRAY_AGG(val ORDER BY val) FROM unnest(ARRAY[tp."AttId", tp."AttCode"]) val)
								OR 
									(SELECT ARRAY_AGG(val ORDER BY val) FROM unnest(ARRAY[item."Attribute6", item."AttributeCode6"]) val) =
									(SELECT ARRAY_AGG(val ORDER BY val) FROM unnest(ARRAY[tp."AttId", tp."AttCode"]) val)
								OR 
									(SELECT ARRAY_AGG(val ORDER BY val) FROM unnest(ARRAY[item."Attribute7", item."AttributeCode7"]) val) =
									(SELECT ARRAY_AGG(val ORDER BY val) FROM unnest(ARRAY[tp."AttId", tp."AttCode"]) val)
								OR 
									(SELECT ARRAY_AGG(val ORDER BY val) FROM unnest(ARRAY[item."Attribute8", item."AttributeCode8"]) val) =
									(SELECT ARRAY_AGG(val ORDER BY val) FROM unnest(ARRAY[tp."AttId", tp."AttCode"]) val)
								OR 
									(SELECT ARRAY_AGG(val ORDER BY val) FROM unnest(ARRAY[item."Attribute9", item."AttributeCode9"]) val) =
									(SELECT ARRAY_AGG(val ORDER BY val) FROM unnest(ARRAY[tp."AttId", tp."AttCode"]) val)
								OR 
									(SELECT ARRAY_AGG(val ORDER BY val) FROM unnest(ARRAY[item."Attribute10", item."AttributeCode10"]) val) =
									(SELECT ARRAY_AGG(val ORDER BY val) FROM unnest(ARRAY[tp."AttId", tp."AttCode"]) val)
							)
							
					)

					SELECT * FROM "GetLevelApplyBySku"
					UNION
					SELECT * FROM "GetLevelApplyByAttribute"
					UNION
					SELECT * FROM "GetLevelApplyByItemGroup";

END
$BODY$
  LANGUAGE plpgsql VOLATILE
  COST 100
  ROWS 1000

