CREATE OR REPLACE FUNCTION "public"."f_getpromotiondetailsbydistributor"("distributorcode" varchar, "promotioncodes" _varchar)
  RETURNS TABLE("LevelId" varchar, "LevelDesc" text, "PromotionId" varchar, "PromotionName" varchar, "PromotionType" varchar, "OrderRule" varchar, "ProductType" varchar, "OrderBy" text, "PackingUomId" varchar, "PackingUomName" text, "LevelOrderQty" int4, "LevelOrderAmount" numeric, "ProductCode" varchar, "ProductName" text, "AttId" varchar, "AttName" text, "AttCode" varchar, "AttCodeName" text, "OrderProductQty" int4, "IsGiftProduct" bool, "FreeItemType" varchar, "FreeAmountType" varchar, "RuleOfGiving" bool, "FreeAmount" numeric, "FreePercentAmount" numeric, "NumberOfFreeItem" int4, "IsDefaultProduct" bool, "AllowExchange" bool, "ExchangeRate" int4, "IsFreeProduct" bool, "IsSalesProduct" bool, "SicId" varchar, "LevelFreeQty" int4, "OnEach" float4, "DiscountType" varchar, "RequiredMinQty" bool, "MinValue" float4, "FreeSameProduct" bool, "IsApplyBudget" bool, "BudgetValueCode" varchar, "BudgetQtyCode" varchar, "IsBudgetQtyBookOver" bool, "IsBudgetValueBookOver" bool) AS $BODY$
BEGIN
	RETURN QUERY
	-- colelct sp bán
	SELECT 
	tpds."LevelCode",
	CAST((tpds."LevelName") AS TEXT) AS LevelDesc,
	tpds."PromotionCode",
	tp."FullName"::varchar as "PromotionName",
	(case
		when tp."PromotionType" = '01' then 'Line'
		when tp."PromotionType" = '02' then 'Group'
		when tp."PromotionType" = '03' then 'Bundle'
		else NULL
	end)::varchar as "PromotionType",
	(CASE WHEN tp."RuleOfGivingByValue" = TRUE THEN 'AccordingPassLevel'
    WHEN tp."RuleOfGivingByValue" = FALSE THEN 'AccordingBoxCarton'
    ELSE NULL
    END)::varchar AS "OrderRule",
	cast((case when cast(tpds."ProductTypeForSale" as integer) = 1 then 'Stock'::CHARACTER VARYING
	when cast(tpds."ProductTypeForSale" as integer) = 2 then 'Group'::CHARACTER VARYING
	when cast(tpds."ProductTypeForSale" as integer) = 3 then 'Attribute'::CHARACTER VARYING
	else null
	end) as character varying) as ProductType,
	CAST((CASE WHEN tp."PromotionCheckBy"  = TRUE THEN 'Quantity'
    WHEN tp."PromotionCheckBy" = FALSE THEN 'Value'
    ELSE ''
    END) AS TEXT) AS OrderBy,
	tpdpfs."Packing",
	uom."Description"::TEXT, -- UOM name
	CASE 
	WHEN tpds."QuantityPurchased" > 0 THEN
		tpds."QuantityPurchased"
	ELSE
		null
	END as "QuantityPurchased",
	CASE 
	WHEN tpds."ValuePurchased" > 0 THEN
		tpds."ValuePurchased"
	ELSE
		null
END as "ValuePurchased",
CASE 
	WHEN CAST((tpdpfs."ItemHierarchyLevelForSale") as character varying) is null or CAST((tpdpfs."ItemHierarchyLevelForSale") as character varying) ='' THEN
		tpdpfs."ProductCode"
	ELSE
		null
END as "ProductCode",
	cast((case when cast(tpds."ProductTypeForSale" as integer) = 1 and (CAST((tpdpfs."ItemHierarchyLevelForSale") as character varying) is null or  CAST((tpdpfs."ItemHierarchyLevelForSale") as character varying) ='') then item."ShortName"
	when cast(tpds."ProductTypeForSale" as integer) = 2 and (CAST((tpdpfs."ItemHierarchyLevelForSale") as character varying) is null or  CAST((tpdpfs."ItemHierarchyLevelForSale") as character varying) ='')  then itg."Description"
	else ''
	end) as text) as "ProductName",
	CAST((tpdpfs."ItemHierarchyLevelForSale") as character varying) as AttId,
	it."Description"::text as "AttName", --attributeid name
CASE 
	WHEN CAST((tpdpfs."ItemHierarchyLevelForSale") as character varying) is not null and CAST((tpdpfs."ItemHierarchyLevelForSale") as character varying) <>'' THEN
		tpdpfs."ProductCode"
	ELSE
		null
END as "AttCode",
ita."ShortName"::text as "AttCodeName",
	
	CASE 
	WHEN tpdpfs."SellNumber" >0 THEN
		tpdpfs."SellNumber"
	ELSE
		null
END as "SellNumber",
	NULL::BOOLEAN, --IsGiftProduct
	NULL::CHARACTER VARYING, --FreeItemType
	NULL::CHARACTER VARYING, --FreeAmountType
	NULL::BOOLEAN, --RuleOfGiving
	NULL::NUMERIC, --FreeAmount
	NULL::NUMERIC, -- FreePercentAmount
	NULL::INTEGER, --NumberOfFreeItem
	NULL::BOOLEAN, --IsDefaultProduct
	NULL::BOOLEAN, --AllowExchange
	NULL::INTEGER, --ExchangeRate
	FALSE::BOOLEAN, --IsFreeProduct
	TRUE::BOOLEAN,  --IsSalesProduct
	tp."SicCode", --SicId
	NULL::INTEGER, --LevelFreeQty
	CAST((CASE WHEN tp."PromotionCheckBy"  = TRUE THEN tpds."OnEachQuantity"
    WHEN tp."PromotionCheckBy" = FALSE THEN tpds."OnEachValue"
    ELSE null
    END) AS REAL) AS "OnEach",
	NULL::CHARACTER VARYING, --DiscountType
	CASE 
	WHEN tpdpfs."SalesItemMinValue" is not null THEN
		TRUE
	ELSE
		null
END ::BOOLEAN as "RequiredMinQty",

CASE 
	WHEN tpdpfs."SalesItemMinValue" >0 THEN
		tpdpfs."SalesItemMinValue"
	ELSE
		null
END as "MinValue",
	tpds."IsGiveSameProductSale"::boolean, --FreeSameProduct
--	tpds."IsApplyBudget",
	CASE 
	WHEN tpds."GiftApplyBudgetCode" is not null or tpds."DonateApplyBudgetCode" is not null THEN
		TRUE
	ELSE
		FALSE
END ::boolean as "IsApplyBudget",
	tpds."DonateApplyBudgetCode",	
	tpds."GiftApplyBudgetCode",	
	
	bgq."FlagOverBudget" as "IsBudgetQtyBookOver",
	bgv."FlagOverBudget" as "IsBudgetValueBookOver"
	FROM app_union_public('' || distributorcode ||'', null::"TpPromotionDefinitionStructures") as tpds 
	join app_union_public('' || distributorcode ||'', null::"TpPromotions") as tp ON tp."Code" = tpds."PromotionCode"
	join app_union_public('' || distributorcode ||'', null::"TpPromotionDefinitionProductForSales") as tpdpfs ON tpdpfs."PromotionCode" = tp."Code"  and tpds."LevelCode"=tpdpfs."LevelCode"
	left join public."Uoms" uom on tpdpfs."Packing"=uom."UomId"
	left join public."InventoryItems" item on tpdpfs."ProductCode"=item."InventoryItemId"
	left join public."ItemGroups" itg on tpdpfs."ProductCode"=itg."Code"
	left join public."ItemAttributes" ita on ita."ItemAttributeMaster"=tpdpfs."ItemHierarchyLevelForSale" and ita."ItemAttributeCode"=tpdpfs."ProductCode"
	left join app_union_public('' || distributorcode ||'', null::"TpBudgets") bgq on  bgq."Code"=tpds."GiftApplyBudgetCode"
	left join app_union_public('' || distributorcode ||'', null::"TpBudgets") bgv on  bgv."Code"=tpds."DonateApplyBudgetCode"
	left join public."ItemSettings" it on it."AttributeId"=tpdpfs."ItemHierarchyLevelForSale" 
	where tpds."LevelCode" = tpdpfs."LevelCode"
	and tp."Status"='03'
	and tp."DeleteFlag"=0
	and tpds."DeleteFlag"=0
	and tpdpfs."DeleteFlag"=0
	--and tp."EffectiveDateFrom"::date <= now()::date 
	and (tp."ValidUntil"::date >= now() or tp."ValidUntil" is null)
	and tp."Code" =  ANY($2)
	
	UNION
	
	
	-- collect sp tặng
	SELECT 
	tpds."LevelCode",
	CAST((tpds."LevelName") AS TEXT) AS LevelDesc,
	tpds."PromotionCode",
	tp."FullName"::varchar as "PromotionName",
	(case
		when tp."PromotionType" = '01' then 'Line'
		when tp."PromotionType" = '02' then 'Group'
		when tp."PromotionType" = '03' then 'Bundle'
		else NULL
	end)::varchar as "PromotionType",
	(CASE WHEN tp."RuleOfGivingByValue" = TRUE THEN 'AccordingPassLevel'
    WHEN tp."RuleOfGivingByValue" = FALSE THEN 'AccordingBoxCarton'
    ELSE NULL
    END)::varchar AS "OrderRule",
	cast((case when cast(tpds."ProductTypeForGift" as integer) = 1 then 'Stock'::CHARACTER VARYING
	when cast(tpds."ProductTypeForGift" as integer) = 2 then 'Group'::CHARACTER VARYING
	when cast(tpds."ProductTypeForGift" as integer) = 3 then 'Attribute'::CHARACTER VARYING
	else null
	end) as character varying) as ProductType,
	CAST((CASE WHEN tp."PromotionCheckBy"  = TRUE THEN 'Quantity'
    WHEN tp."PromotionCheckBy" = FALSE THEN 'Value'
    ELSE ''
    END) AS TEXT) AS OrderBy,
	tpdpfg."Packing",
	uom."Description"::TEXT, -- UOM name
	null as "QuantityPurchased",
	null as "ValuePurchased",
CASE 
	WHEN CAST((tpdpfg."ItemHierarchyLevelForGift") as character varying) is null or CAST((tpdpfg."ItemHierarchyLevelForGift") as character varying)=''   THEN
		tpdpfg."ProductCode"
	ELSE
		null
END as "ProductCode",
		cast((case when cast(tpds."ProductTypeForGift" as integer) = 1 and (CAST((tpdpfg."ItemHierarchyLevelForGift") as character varying) is null or  CAST((tpdpfg."ItemHierarchyLevelForGift") as character varying) ='') then item."ShortName"
	when cast(tpds."ProductTypeForGift" as integer) = 2 and (CAST((tpdpfg."ItemHierarchyLevelForGift") as character varying) is null or  CAST((tpdpfg."ItemHierarchyLevelForGift") as character varying) ='')  then itg."Description"
	else ''
	end) as text) as "ProductName",
	CAST((tpdpfg."ItemHierarchyLevelForGift") as character varying) as AttId,
	it."Description"::text as "AttName", --attributeid name
CASE 
	WHEN CAST((tpdpfg."ItemHierarchyLevelForGift") as character varying) is not null and CAST((tpdpfg."ItemHierarchyLevelForGift") as character varying) <>'' THEN
		tpdpfg."ProductCode"
	ELSE
		null
END as "AttCode",
ita."ShortName"::text as "AttCodeName",
	
	NULL::INTEGER "SellNumber",

	NULL::BOOLEAN, --IsGiftProduct
	'True'::CHARACTER VARYING as "FreeItemType", --FreeItemType
	NULL::CHARACTER VARYING, --FreeAmountType
--	tpds."RuleOfGiving"::BOOLEAN,
	CASE 
	WHEN tpds."RuleOfGiving"=TRUE THEN
		FALSE
	WHEN tpds."RuleOfGiving"=FALSE THEN
		TRUE
	ELSE
		null
END ::BOOLEAN as "RuleOfGiving",

	NULL::NUMERIC, --FreeAmount
	NULL::NUMERIC, --FreePercentAmount
	
	
	CASE 
	WHEN tpdpfg."NumberOfGift" >0 THEN
		tpdpfg."NumberOfGift" 
	ELSE
		null
END as "NumberOfFreeItem",
	tpdpfg."IsDefaultProduct",
	CAST((CASE WHEN tpdpfg."Exchange" = 0 THEN FALSE
    WHEN tpdpfg."Exchange" > 0 THEN TRUE
    ELSE NULL END) as BOOLEAN) as AllowExchange,
		
	CASE 
	WHEN tpdpfg."Exchange" > 0 THEN
		tpdpfg."Exchange"
	ELSE
		null
END as "ExchangeRate",

	TRUE::BOOLEAN, --IsFreeProduct
	FALSE::BOOLEAN, --IsSalesProduct
	tp."SicCode",
	CASE 
	WHEN tpds."RuleOfGivingByProductQuantity" > 0 THEN
		tpds."RuleOfGivingByProductQuantity"
	ELSE
		null
	END as "LevelFreeQty", 

	NULL::REAL AS "OnEach",
	NULL::CHARACTER VARYING, --DiscountType
	NULL::BOOLEAN, --RequiredMinQty
	NULL::REAL, --MinValue
	tpds."IsGiveSameProductSale"::boolean, --FreeSameProduct
	CASE 
	WHEN tpds."GiftApplyBudgetCode" is not null or tpds."DonateApplyBudgetCode" is not null THEN
		TRUE
	ELSE
		FALSE
END ::boolean as "IsApplyBudget",
tpds."DonateApplyBudgetCode",
	tpds."GiftApplyBudgetCode",	
	
	bgq."FlagOverBudget" as "IsBudgetQtyBookOver",
	bgv."FlagOverBudget" as "IsBudgetValueBookOver"

	FROM app_union_public('' || distributorcode ||'', null::"TpPromotionDefinitionStructures") as tpds 
	JOIN app_union_public('' || distributorcode ||'', null::"TpPromotions") as tp ON tp."Code" = tpds."PromotionCode" 
	join app_union_public('' || distributorcode ||'', null::"TpPromotionDefinitionProductForGifts") as tpdpfg ON tpdpfg."PromotionCode" = tp."Code" and tpds."LevelCode"=tpdpfg."LevelCode" 
	LEFT JOIN public."Uoms" uom on tpdpfg."Packing"=uom."UomId"
	LEFT JOIN public."InventoryItems" item on tpdpfg."ProductCode"=item."InventoryItemId"
	LEFT JOIN public."ItemGroups" itg on tpdpfg."ProductCode"=itg."Code"
	LEFT JOIN public."ItemAttributes" ita on ita."ItemAttributeMaster"=tpdpfg."ItemHierarchyLevelForGift" and ita."ItemAttributeCode"=tpdpfg."ProductCode"
	LEFT JOIN public."ItemSettings" it on it."AttributeId"=tpdpfg."ItemHierarchyLevelForGift" 
	LEFT JOIN app_union_public('' || distributorcode ||'', null::"TpBudgets") bgq on  bgq."Code"=tpds."GiftApplyBudgetCode"
	LEFT JOIN app_union_public('' || distributorcode ||'', null::"TpBudgets") bgv on  bgv."Code"=tpds."DonateApplyBudgetCode"
	where tpdpfg."LevelCode" = tpds."LevelCode"  
	and tpds."IsGiftProduct"=true
		and tp."Status"='03'
	and tp."DeleteFlag"=0
	and tpds."DeleteFlag"=0
	and tpdpfg."DeleteFlag"=0
--	and tp."EffectiveDateFrom"::date <= now()::date 
	and (tp."ValidUntil"::date >= now()::date or tp."ValidUntil" is null)
	and tp."Code" =  ANY($2)
	
	UNION
	
	
	-- colelct tặng tiền
	SELECT 
	tpds."LevelCode",
	CAST((tpds."LevelName") AS TEXT) AS LevelDesc,
	tpds."PromotionCode",
	tp."FullName"::varchar as "PromotionName",
	(case
		when tp."PromotionType" = '01' then 'Line'
		when tp."PromotionType" = '02' then 'Group'
		when tp."PromotionType" = '03' then 'Bundle'
		else NULL
	end)::varchar as "PromotionType",
	(CASE WHEN tp."RuleOfGivingByValue" = TRUE THEN 'AccordingPassLevel'
    WHEN tp."RuleOfGivingByValue" = FALSE THEN 'AccordingBoxCarton'
    ELSE NULL
    END)::varchar AS "OrderRule",
	'Money'::CHARACTER VARYING,
	CAST((CASE WHEN tp."PromotionCheckBy"  = TRUE THEN 'Quantity'
    WHEN tp."PromotionCheckBy" = FALSE THEN 'Value'
    ELSE ''
    END) AS TEXT) AS OrderBy,
	NULL::CHARACTER VARYING,
	NULL::TEXT,
	NULL::INTEGER,
	NULL::NUMERIC,
	NULL::CHARACTER VARYING,
	NULL::TEXT,
	NULL::CHARACTER VARYING,
	NULL::TEXT,
	NULL::CHARACTER VARYING,
	NULL::TEXT,
	NULL::INTEGER,
	NULL::BOOLEAN,
	NULL::CHARACTER VARYING,
	CAST((CASE WHEN tpds."IsFixMoney" = true THEN 'Amount'
    WHEN tpds."IsFixMoney" = false THEN 'Percent'
    ELSE ''
    END) AS CHARACTER VARYING) AS FreeAmountType,
	NULL::boolean,
	CASE 
	WHEN tpds."IsFixMoney" = TRUE THEN
		tpds."AmountOfDonation"
	ELSE
		null
END as "FreeAmount",
		CASE 
	WHEN tpds."IsFixMoney" = FALSE THEN
		CAST((tpds."PercentageOfAmount") as numeric)
	ELSE
		null
END as "FreePercentAmount",
	NULL::INTEGER,
	NULL::BOOLEAN,
	NULL::BOOLEAN,
	NULL::INTEGER,
	TRUE::BOOLEAN,
	FALSE::BOOLEAN,
	tp."SicCode",
	CASE 
	WHEN tpds."RuleOfGivingByProductQuantity" >0 THEN
		tpds."RuleOfGivingByProductQuantity"
	ELSE
		null
END as "LevelFreeQty", 
NULL::REAL AS "OnEach",

CAST((CASE WHEN tpds."IsDonateAllowance"  = TRUE THEN 'Discount'
    WHEN tpds."IsDonateAllowance" = FALSE THEN 'Donate'
    ELSE ''
    END) AS TEXT) AS "DiscountType",
	NULL::BOOLEAN, --RequiredMinQty
	NULL::REAL, --MinValue
	NULL::boolean, --FreeSameProduct
CASE 
	WHEN tpds."GiftApplyBudgetCode" is not null or tpds."DonateApplyBudgetCode" is not null THEN
		TRUE
	ELSE
		FALSE
END ::boolean as "IsApplyBudget",
tpds."DonateApplyBudgetCode",
tpds."GiftApplyBudgetCode",	
	
	bgq."FlagOverBudget" as "IsBudgetQtyBookOver",
	bgv."FlagOverBudget" as "IsBudgetValueBookOver"
	FROM app_union_public('' || distributorcode ||'', null::"TpPromotionDefinitionStructures") as tpds 
	JOIN app_union_public('' || distributorcode ||'', null::"TpPromotions") as tp ON tp."Code" = tpds."PromotionCode"
	LEFT JOIN app_union_public('' || distributorcode ||'', null::"TpBudgets") bgq on  bgq."Code"=tpds."GiftApplyBudgetCode"
	LEFT JOIN app_union_public('' || distributorcode ||'', null::"TpBudgets") bgv on  bgv."Code"=tpds."DonateApplyBudgetCode"
	WHERE 
  	tp."DeleteFlag"=0
	and tpds."DeleteFlag"=0
	and tp."Status"='03'
	--and tpds."IsGiftProduct"=false
	--and	tp."EffectiveDateFrom"::date <= now()::date 
	and (tp."ValidUntil"::date >= now()::date or tp."ValidUntil" is null)
--	and tpds."IsFixMoney" = true and tpds."AmountOfDonation" = 0
	and (tpds."AmountOfDonation" > 0 or tpds."PercentageOfAmount">0)
	and tp."Code" = ANY($2);

END
$BODY$
  LANGUAGE plpgsql VOLATILE
  COST 100
  ROWS 1000