-- declare @WhsCode nvarchar(8) = 'SM';
select X0."AbsEntry",
       X0."PickDate",
       X0."Status",
       X0."Remarks",
       Sum(Case X0."BaseObject" When 17 Then 1 Else 0 End)         "SalesOrders",
       Sum(Case X0."BaseObject" When 13 Then 1 Else 0 End)         "Invoices",
       Sum(Case X0."BaseObject" When 1250000001 Then 1 Else 0 End) "Transfers",
       Sum("Quantity")                                             "Quantity",
       Sum("OpenQuantity")                                         "OpenQuantity",
       Sum("UpdateQuantity")                                       "UpdateQuantity"
from (select PICKS."AbsEntry",
             PICKS."PickDate",
             PICKS."Status",
             Cast(PICKS."Remarks" as nvarchar(4000))        "Remarks",
             T1."BaseObject",
             Sum(T1."RelQtty" + T1."PickQtty")              "Quantity",
             Sum(T1."RelQtty" - COALESCE(T6."Quantity", 0)) "OpenQuantity",
             Sum(COALESCE(T6."Quantity", 0))                "UpdateQuantity"
      from OPKL PICKS
               inner join PKL1 T1 on T1."AbsEntry" = PICKS."AbsEntry"
               inner join OILM T2 on T2.TransType = T1.BaseObject and T2.DocEntry = T1.OrderEntry and T2.DocLineNum = T1.OrderLine
               left outer join (select "U_AbsEntry"      "AbsEntry",
                                       "U_PickEntry"     "PickEntry",
                                       Sum("U_Quantity") "Quantity"
                                from [@LW_YUVAL08_PKL1]
                                where "U_Status" in ('O', 'P')
                                Group By "U_AbsEntry", "U_PickEntry") T6
                               on T6."AbsEntry" = T1."AbsEntry" and T6."PickEntry" = T1."PickEntry"
      where T2.LocCode = @WhsCode
        and PICKS."Status" in ('R', 'P', 'D')
-- {0}
      GROUP BY PICKS."AbsEntry",
               PICKS."PickDate",
               PICKS."Status",
               Cast(PICKS."Remarks" as nvarchar(4000)), T1."BaseObject", T1."OrderEntry") X0
group by X0."Status", X0."PickDate", X0."AbsEntry", X0."Remarks"
order by X0."AbsEntry" desc