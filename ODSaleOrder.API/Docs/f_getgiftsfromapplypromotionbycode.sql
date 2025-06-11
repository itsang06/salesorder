CREATE OR REPLACE FUNCTION "public"."f_getgiftsfromapplypromotionbycode"("distributorcode" varchar, "promotionlist" _varchar, "levellist" _varchar)
  RETURNS TABLE("LevelId" varchar, "LevelDesc" text, "PromotionId" varchar, "PromotionName" varchar, "PromotionType" varchar, "OrderRule" varchar, "ProductType" varchar, "OrderBy" text, "PackingUomId" varchar, "PackingUomName" text, "LevelOrderQty" int4, "LevelOrderAmount" numeric, "ProductCode" varchar, "ProductName" text, "AttId" varchar, "AttName" text, "AttCode" varchar, "AttCodeName" text, "OrderProductQty" int4, "IsGiftProduct" bool, "FreeItemType" varchar, "FreeAmountType" varchar, "RuleOfGiving" bool, "FreeAmount" numeric, "FreePercentAmount" numeric, "NumberOfFreeItem" int4, "IsDefaultProduct" bool, "AllowExchange" bool, "ExchangeRate" int4, "IsFreeProduct" bool, "IsSalesProduct" bool, "SicId" varchar, "LevelFreeQty" int4, "OnEach" float4, "DiscountType" varchar, "RequiredMinQty" bool, "MinValue" float8, "FreeSameProduct" bool, "IsApplyBudget" bool, "BudgetValueCode" varchar, "BudgetQtyCode" varchar, "IsBudgetQtyBookOver" bool, "IsBudgetValueBookOver" bool, "BaseUnit" varchar, "ConversionFactor" int4) AS $BODY$

BEGIN
	RETURN QUERY

					WITH "PromotionDetailsList" AS 
					(
							SELECT tp.* 
							FROM "public"."f_getpromotiondetailsbycode"('' || distributorcode ||'', promotionlist) tp
							WHERE tp."PromotionType" IN ('Line', 'ByOrder', 'Group') AND tp."IsFreeProduct" = 't' 
							AND CONCAT('{', tp."PromotionId", ',', tp."LevelId", '}') = ANY(levellist)
							
					),


					"GetLevelApplyBySku" AS
					(
							SELECT DISTINCT
							tp."LevelId" , tp."LevelDesc" , 
							tp."PromotionId" , tp."PromotionName" , tp."PromotionType", 
							tp."OrderRule", tp."ProductType" , tp."OrderBy" , 
							tp."PackingUomId" , tp."PackingUomName" , 
							tp."LevelOrderQty" , tp."LevelOrderAmount", 
							tp."ProductCode",
							tp."ProductName",
							tp."AttId" , tp."AttName" , 
							tp."AttCode" , tp."AttCodeName" , 
							tp."OrderProductQty" , tp."IsGiftProduct" ,
							tp."FreeItemType" ,  tp."FreeAmountType" , tp."RuleOfGiving" , 
							tp."FreeAmount" , tp."FreePercentAmount" , 
							tp."NumberOfFreeItem" , tp."IsDefaultProduct" , 
							tp."AllowExchange" , tp."ExchangeRate" , 
							tp."IsFreeProduct" , tp."IsSalesProduct" , 
							tp."SicId" , tp."LevelFreeQty" , tp."OnEach" , 
							tp."DiscountType" , tp."RequiredMinQty" , 
							tp."MinValue" , tp."FreeSameProduct" , 
							tp."IsApplyBudget" , 
							tp."BudgetValueCode" , tp."BudgetQtyCode" , 
							tp."IsBudgetQtyBookOver" , tp."IsBudgetValueBookOver" ,
							item."BaseUnit",
							(CASE
							   WHEN tp."PackingUomId" = item."BaseUnit" THEN 1::int4
								 ELSE item."ConversionFactor"::int4
							 END) AS "ConversionFactor"
							
							FROM "PromotionDetailsList" tp
							INNER JOIN "VVInventoryItems" item ON item."InventoryItemCode" = tp."ProductCode" 
							AND (item."BaseUnit" = tp."PackingUomId" OR item."FromUnit" = tp."PackingUomId") AND item."ToUnit" = item."BaseUnit" AND item."ItemType" = 'STOCK' AND item."Competitor" = 'f'
							WHERE tp."ProductType" = 'Stock'
					),

					--SELECT * FROM "GetLevelApplyBySku";


					"GetLevelApplyByItemGroup" AS
					(
							SELECT DISTINCT
							--tp.*,
							tp."LevelId" , tp."LevelDesc" , 
							tp."PromotionId" , tp."PromotionName" , tp."PromotionType", 
							tp."OrderRule", tp."ProductType" , tp."OrderBy" , 
							tp."PackingUomId" , tp."PackingUomName" , 
							tp."LevelOrderQty" , tp."LevelOrderAmount", 
							item."InventoryItemCode" as "ProductCode",
							item."ShortName" as "ProductName",
							tp."AttId" , tp."AttName" , 
							tp."AttCode" , tp."AttCodeName" , 
							tp."OrderProductQty" , tp."IsGiftProduct" ,
							tp."FreeItemType" ,  tp."FreeAmountType" , tp."RuleOfGiving" , 
							tp."FreeAmount" , tp."FreePercentAmount" , 
							tp."NumberOfFreeItem" , tp."IsDefaultProduct" , 
							tp."AllowExchange" , tp."ExchangeRate" , 
							tp."IsFreeProduct" , tp."IsSalesProduct" , 
							tp."SicId" , tp."LevelFreeQty" , tp."OnEach" , 
							tp."DiscountType" , tp."RequiredMinQty" , 
							tp."MinValue" , tp."FreeSameProduct" , 
							tp."IsApplyBudget" , 
							tp."BudgetValueCode" , tp."BudgetQtyCode" , 
							tp."IsBudgetQtyBookOver" , tp."IsBudgetValueBookOver" ,
							item."BaseUnit",
							(CASE
							   WHEN tp."PackingUomId" = item."BaseUnit" THEN 1::int4
								 ELSE item."ConversionFactor"::int4
							 END) AS "ConversionFactor"
							FROM "PromotionDetailsList" tp
							INNER JOIN "VVInventoryItems" item ON item."ItemGroupCode" = tp."ProductCode" 
							AND (item."BaseUnit" = tp."PackingUomId"  OR item."FromUnit" = tp."PackingUomId") AND item."ToUnit" = item."BaseUnit" AND item."ItemType" = 'STOCK' AND item."Competitor" = 'f'
							WHERE tp."ProductType" = 'Group'
					),


					"GetLevelApplyByAttribute" AS
					(
							SELECT DISTINCT
							--tp.*,
							tp."LevelId" , tp."LevelDesc" , 
							tp."PromotionId" , tp."PromotionName" , tp."PromotionType", 
							tp."OrderRule", tp."ProductType" , tp."OrderBy" , 
							tp."PackingUomId" , tp."PackingUomName" , 
							tp."LevelOrderQty" , tp."LevelOrderAmount", 
							item."InventoryItemCode" as "ProductCode",
							item."ShortName" as "ProductName",
							tp."AttId" , tp."AttName" , 
							tp."AttCode" , tp."AttCodeName" , 
							tp."OrderProductQty" , tp."IsGiftProduct" ,
							tp."FreeItemType" ,  tp."FreeAmountType" , tp."RuleOfGiving" , 
							tp."FreeAmount" , tp."FreePercentAmount" , 
							tp."NumberOfFreeItem" , tp."IsDefaultProduct" , 
							tp."AllowExchange" , tp."ExchangeRate" , 
							tp."IsFreeProduct" , tp."IsSalesProduct" , 
							tp."SicId" , tp."LevelFreeQty" , tp."OnEach" , 
							tp."DiscountType" , tp."RequiredMinQty" , 
							tp."MinValue" , tp."FreeSameProduct" , 
							tp."IsApplyBudget" , 
							tp."BudgetValueCode" , tp."BudgetQtyCode" , 
							tp."IsBudgetQtyBookOver" , tp."IsBudgetValueBookOver" ,
							item."BaseUnit",
							(CASE
							   WHEN tp."PackingUomId" = item."BaseUnit" THEN 1::int4
								 ELSE item."ConversionFactor"::int4
							END) AS "ConversionFactor"
							FROM "PromotionDetailsList" tp
							INNER JOIN "VVInventoryItems" item ON  
														((item."Attribute1" =  tp."AttId" AND item."AttributeCode1" = tp."AttCode")
														OR 
														(item."Attribute2" =  tp."AttId" AND item."AttributeCode2" = tp."AttCode")
														OR 
														(item."Attribute3" =  tp."AttId" AND item."AttributeCode3" = tp."AttCode")
														OR 
														(item."Attribute4" =  tp."AttId" AND item."AttributeCode4" = tp."AttCode")
														OR 
														(item."Attribute5" =  tp."AttId" AND item."AttributeCode5" = tp."AttCode")
														OR 
														(item."Attribute6" =  tp."AttId" AND item."AttributeCode6" = tp."AttCode")
														OR 
														(item."Attribute7" =  tp."AttId" AND item."AttributeCode7" = tp."AttCode")
														OR 
														(item."Attribute8" =  tp."AttId" AND item."AttributeCode8" = tp."AttCode")
														OR 
														(item."Attribute9" =  tp."AttId" AND item."AttributeCode9" = tp."AttCode")
														OR 
													 (item."Attribute10" =  tp."AttId" AND item."AttributeCode10" = tp."AttCode"))
							AND (item."BaseUnit" = tp."PackingUomId"  OR item."FromUnit" = tp."PackingUomId") AND item."ToUnit" = item."BaseUnit" AND item."ItemType" = 'STOCK' AND item."Competitor" = 'f'
							WHERE tp."ProductType" = 'Attribute'
							
					),

					"GetLevelApplyByMoney" AS 
					(
						SELECT DISTINCT
							tp."LevelId" , tp."LevelDesc" , 
							tp."PromotionId" , tp."PromotionName" , tp."PromotionType", 
							tp."OrderRule", tp."ProductType" , tp."OrderBy" , 
							tp."PackingUomId" , tp."PackingUomName" , 
							tp."LevelOrderQty" , tp."LevelOrderAmount", 
							tp."ProductCode",
							tp."ProductName",
							tp."AttId" , tp."AttName" , 
							tp."AttCode" , tp."AttCodeName" , 
							tp."OrderProductQty" , tp."IsGiftProduct" ,
							tp."FreeItemType" ,  tp."FreeAmountType" , tp."RuleOfGiving" , 
							tp."FreeAmount" , tp."FreePercentAmount" , 
							tp."NumberOfFreeItem" , tp."IsDefaultProduct" , 
							tp."AllowExchange" , tp."ExchangeRate" , 
							tp."IsFreeProduct" , tp."IsSalesProduct" , 
							tp."SicId" , tp."LevelFreeQty" , tp."OnEach" , 
							tp."DiscountType" , tp."RequiredMinQty" , 
							tp."MinValue" , tp."FreeSameProduct" , 
							tp."IsApplyBudget" , 
							tp."BudgetValueCode" , tp."BudgetQtyCode" , 
							tp."IsBudgetQtyBookOver" , tp."IsBudgetValueBookOver" ,
							NULL::VARCHAR as "BaseUnit",
							NULL::int4 as "ConversionFactor"
							FROM "PromotionDetailsList" tp
							WHERE tp."ProductType" = 'Money'

					)


					SELECT * FROM "GetLevelApplyBySku"
					UNION
					SELECT * FROM "GetLevelApplyByItemGroup"
					UNION
					SELECT * FROM "GetLevelApplyByAttribute"
					UNION 
					SELECT * FROM "GetLevelApplyByMoney"
					ORDER BY "PromotionId", "LevelId";


END
$BODY$
  LANGUAGE plpgsql VOLATILE
  COST 100
  ROWS 1000