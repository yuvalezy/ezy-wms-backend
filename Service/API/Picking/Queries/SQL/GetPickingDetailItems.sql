-- declare @AbsEntry int = 29;
-- declare @Type int = 17;
-- declare @Entry int =542;
select T2."ItemCode",
       T5."ItemName",
       T5."NumInBuy",
       T5."BuyUnitMsr",
       T5."PurPackUn",
       T5."PurPackMsr",
       Sum(T1."RelQtty" + T1."PickQtty")               "Quantity",
       Sum(T1."PickQtty" + COALESCE(T6."Quantity", 0)) "Picked",
       Sum(T1."RelQtty" - COALESCE(T6."Quantity", 0))  "OpenQuantity"
from PKL1 T1
         inner join OILM T2 on T2.TransType = T1.BaseObject and T2.DocEntry = T1.OrderEntry and T2.DocLineNum = T1.OrderLine
         inner join OITM T5 on T5."ItemCode" = T2."ItemCode"
         left outer join (select "U_PickEntry"     "PickEntry",
                                 Sum("U_Quantity") "Quantity"
                          from [@LW_YUVAL08_PKL1]
                          where "U_AbsEntry" = @AbsEntry
                            and "U_Status" in ('O', 'P')
                          Group By "U_PickEntry") T6
                         on T6."PickEntry" = T1."PickEntry"
where T1."AbsEntry" = @AbsEntry
  and T1."BaseObject" = @Type
  and T1."OrderEntry" = @Entry
group by T5."ItemName", T2."ItemCode", T5."NumInBuy", T5."BuyUnitMsr", T5."PurPackUn", T5."PurPackMsr"
order by Case When Sum(T1."RelQtty" - COALESCE(T6."Quantity", 0)) = 0 Then 1 Else 0 End,
         T2."ItemCode"