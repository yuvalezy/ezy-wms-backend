-- declare @AbsEntry int = 8;
-- declare @Type int = 13;
-- declare @Entry int = 414;
select T2."ItemCode",
       T5."ItemName",
       T1."RelQtty" + T1."PickQtty"               "Quantity",
       T1."PickQtty" + COALESCE(T6."Quantity", 0) "Picked",
       T1."RelQtty" - COALESCE(T6."Quantity", 0)  "OpenQuantity"
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
order by Case When T1."RelQtty" - COALESCE(T6."Quantity", 0) = 0 Then 1 Else 0 End, T2."ItemCode"