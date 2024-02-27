-- declare @AbsEntry int = 8;
-- declare @Type int = 13;
-- declare @Entry int = 414;
WITH Items as (select T2."ItemCode",
                      T2."LocCode" "WhsCode"
               from PKL1 T1
                        inner join OILM T2 on T2.TransType = T1.BaseObject and T2.DocEntry = T1.OrderEntry and T2.DocLineNum = T1.OrderLine
               where T1."AbsEntry" = @AbsEntry
                 and T1."BaseObject" = @Type
                 and T1."OrderEntry" = @Entry)
   , Commited as (select "ItemCode", "BinEntry", Sum("Quantity") "Quantity"
                  from (select X0."U_ItemCode" "ItemCode", X0."U_BinEntry" "BinEntry", X0."U_Quantity" "Quantity"
                        from "@LW_YUVAL08_PKL1" X0
                        where X0."U_Status" in ('O', 'P')
                        union all
                        select X0."U_ItemCode", X0."U_BinEntry" "BinEntry", "U_Quantity"
                        from "@LW_YUVAL08_TRANS1" X0
                        where X0."U_LineStatus" in ('O', 'I')) CommitedData
                  Group By "ItemCode", "BinEntry")
select T0."ItemCode",
       T3."BinAbs"    "BinEntry",
       T4."BinCode",
       T3."OnHandQty" - COALESCE(T5."Quantity", 0) "Quantity"
from Items T0
         inner join OIBQ T3 on T3."ItemCode" = T0."ItemCode" and T3."WhsCode" = T0."WhsCode"
         inner join OBIN T4 on T4."AbsEntry" = T3."BinAbs"
         left outer join Commited T5 on T5."ItemCode" = T0."ItemCode" and T5."BinEntry" = T3."BinAbs"
