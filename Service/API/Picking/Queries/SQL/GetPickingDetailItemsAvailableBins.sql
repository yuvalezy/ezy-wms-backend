-- declare @AbsEntry int = 29;
-- declare @Type int = 17;
-- declare @Entry int =542;
-- declare @BinEntry int = null; -- Optional parameter to filter by BinEntry

WITH Items as (select DISTINCT T2."ItemCode",
                      T2."LocCode" "WhsCode"
               from PKL1 T1
                        inner join OILM T2 on T2.TransType = T1.BaseObject and T2.DocEntry = T1.OrderEntry and T2.DocLineNum = T1.OrderLine
               where T1."AbsEntry" = @AbsEntry
                 and T1."BaseObject" = @Type
                 and T1."OrderEntry" = @Entry)
        ,
     Commited as (select "ItemCode", "BinEntry", Sum("Quantity") "Quantity"
                  from (select X0."U_ItemCode" "ItemCode", X0."U_BinEntry" "BinEntry", X0."U_Quantity" "Quantity"
                        from "@LW_YUVAL08_PKL1" X0
                        where X0."U_Status" in ('O', 'P')
                        union all
                        select X0."U_ItemCode", X0."U_BinEntry" "BinEntry", "U_Quantity"
                        from "@LW_YUVAL08_TRANS1" X0
                        where X0."U_LineStatus" in ('O', 'I')) CommitedData
                  Group By "ItemCode", "BinEntry"),
     PickQuantity as (select T3."ItemCode", T1."BinAbs", Sum(T1."PickQtty") "PickQty"
                      from OPKL T0
                               inner join PKL2 T1 on T1."AbsEntry" = T0."AbsEntry"
                               inner join PKL1 T2 on T2."AbsEntry" = T0."AbsEntry" and T2."PickEntry" = T1."PickEntry"
                               inner join OILM T3 on T3.TransType = T2.BaseObject and T3.DocEntry = T2.OrderEntry and T3.DocLineNum = T2.OrderLine
                      where T0."U_LW_YUVAL08_READY" = 'Y'
                        and T0."Status" in ('Y', 'P')
                      group by T3."ItemCode", T1."BinAbs")
select T0."ItemCode",
       T3."BinAbs"                                                             "BinEntry",
       T4."BinCode",
       T3."OnHandQty" - COALESCE(T5."Quantity", 0) - COALESCE(T6."PickQty", 0) "Quantity"
from Items T0
         inner join OIBQ T3 on T3."ItemCode" = T0."ItemCode" and T3."WhsCode" = T0."WhsCode"
         inner join OBIN T4 on T4."AbsEntry" = T3."BinAbs"
         left outer join Commited T5 on T5."ItemCode" = T0."ItemCode" and T5."BinEntry" = T3."BinAbs"
         left outer join PickQuantity T6 on T6."ItemCode" = T0."ItemCode" and T6."BinAbs" = T3."BinAbs"
where (@BinEntry is null or T3."BinAbs" = @BinEntry)
  and T3."OnHandQty" - COALESCE(T5."Quantity", 0) - COALESCE(T6."PickQty", 0) > 0;
