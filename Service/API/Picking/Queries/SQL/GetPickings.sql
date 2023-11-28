-- declare @WhsCode nvarchar(8) = 'SM';
select X0."AbsEntry", X0."PickDate", X0."Status", X0."Remarks",
       Sum(Case X0."BaseObject" When 17 Then 1 Else 0 End)         "SalesOrders",
       Sum(Case X0."BaseObject" When 13 Then 1 Else 0 End)         "Invoices",
       Sum(Case X0."BaseObject" When 1250000001 Then 1 Else 0 End) "Transfers"
from (select PICKS."AbsEntry",
             PICKS."PickDate",
             PICKS."Status",
             Cast(PICKS."Remarks" as nvarchar(4000)) "Remarks",
             T1."BaseObject",
             T1."OrderEntry"
      from OPKL PICKS
               left outer join PKL1 T1 on T1."AbsEntry" = PICKS."AbsEntry"
               left outer join RDR1 T2 on T2."DocEntry" = T1."OrderEntry" and T2."LineNum" = T1."OrderLine" and T2."ObjType" = T1."BaseObject"
               left outer join INV1 T3 on T3."DocEntry" = T1."OrderEntry" and T3."LineNum" = T1."OrderLine" and T3."ObjType" = T1."BaseObject"
               left outer join WTQ1 T4 on T4."DocEntry" = T1."OrderEntry" and T4."LineNum" = T1."OrderLine" and T4."ObjType" = T1."BaseObject"
      where COALESCE(T2."WhsCode", T3."WhsCode", T4."FromWhsCod") = @WhsCode
-- {0}
      GROUP BY PICKS."AbsEntry",
               PICKS."PickDate",
               PICKS."Status",
               Cast(PICKS."Remarks" as nvarchar(4000)), T1."BaseObject", T1."OrderEntry") X0
group by X0."Status", X0."PickDate", X0."AbsEntry", X0."Remarks"
order by X0."AbsEntry" desc