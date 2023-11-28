-- declare @AbsEntry int = 5;
-- declare @Type int = 17;
-- declare @Entry int = 528;
select COALESCE(T2."ItemCode", T3."ItemCode", T4."ItemCode") "ItemCode",
       T5."ItemName",
       T1."RelQtty" + T1."PickQtty"                          "Quantity",
       T1."PickQtty"                                         "Picked",
       T1."RelQtty"                                          "OpenQuantity"
from PKL1 T1
         left outer join RDR1 T2 on T2."DocEntry" = T1."OrderEntry" and T2."LineNum" = T1."OrderLine" and T2."ObjType" = T1."BaseObject"
         left outer join INV1 T3 on T3."DocEntry" = T1."OrderEntry" and T3."LineNum" = T1."OrderLine" and T3."ObjType" = T1."BaseObject"
         left outer join WTQ1 T4 on T4."DocEntry" = T1."OrderEntry" and T4."LineNum" = T1."OrderLine" and T4."ObjType" = T1."BaseObject"
         inner join OITM T5 on T5."ItemCode" = COALESCE(T2."ItemCode", T3."ItemCode", T4."ItemCode")
where T1."AbsEntry" = @AbsEntry
  and T1."BaseObject" = @Type
  and T1."OrderEntry" = @Entry