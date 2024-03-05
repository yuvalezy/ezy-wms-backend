-- declare @ID int = 4;
-- declare @WhsCode nvarchar(8) = 'SM';
declare @SystemBinEntry int = (select top 1 AbsEntry
                               from OBIN
                               where WhsCode = @WhsCode
                                 and SysBin = 'Y')
;
WITH CTE_OINC1 AS (SELECT "U_BinEntry"      "BinEntry",
                          "U_ItemCode"      "ItemCode",
                          SUM("U_Quantity") "Quantity"
                   FROM "@LW_YUVAL08_OINC1"
                   WHERE U_ID = @ID
                     AND "U_LineStatus" <> 'C'
                   GROUP BY "U_ItemCode", "U_BinEntry")
--Counted data from WMS
select "BinEntry", "ItemCode", "Quantity"
from CTE_OINC1
union all
--Remove counted data from default system bin location
select T0."BinAbs", T0."ItemCode", Case When T0."OnHandQty" - T1."Quantity" < 0 Then 0 Else T0."OnHandQty" - T1."Quantity" End "Quantity"
from OIBQ T0
         inner join (select "ItemCode", Sum("Quantity") "Quantity" from CTE_OINC1 Group By "ItemCode") T1 on T1."ItemCode" = T0."ItemCode"
    --ignore if the system default bin location was actually counted
         left outer join (select "ItemCode" from CTE_OINC1 where "BinEntry" = @SystemBinEntry) T2 on T2."ItemCode" = T0."ItemCode"
where T0.BinAbs = @SystemBinEntry
  and T0."OnHandQty" > 0
--ignore if the system default bin location was actually counted
  and T2."ItemCode" is null
union all
--Clear items from counted bin locations that where not counted in it
select "BinAbs", "ItemCode", 0 "Quantity"
from OIBQ T0
where T0."BinAbs" in (select "BinEntry" from CTE_OINC1)
  and T0."ItemCode" not in (select "ItemCode" from CTE_OINC1)
  and T0."OnHandQty" > 0
order by 1, 2