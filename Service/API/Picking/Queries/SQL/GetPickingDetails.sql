-- declare @AbsEntry int = 5;
-- declare @Type int = 17;
-- declare @Entry int = 528;
select T0."BaseObject"                                       "Type",
       T0."OrderEntry"                                       "Entry",
       COALESCE(T2."DocNum", T3."DocNum", T4."DocNum")       "DocNum",
       COALESCE(T2."DocDate", T3."DocDate", T4."DocDate")    "DocDate",
       COALESCE(T2."CardCode", T3."CardCode", T4."CardCode") "CardCode",
       COALESCE(T2."CardName", T3."CardName", T4."CardName") "CardName",
       Sum(T0."RelQtty" + T0."PickQtty")                     "TotalItems",
       Sum(T0."RelQtty" - COALESCE(T6."Quantity", 0))        "TotalOpenItems"
from PKL1 T0
         left outer join ORDR T2 on T2."DocEntry" = T0."OrderEntry" and T2."ObjType" = T0."BaseObject"
         left outer join OINV T3 on T3."DocEntry" = T0."OrderEntry" and T3."ObjType" = T0."BaseObject"
         left outer join OWTQ T4 on T4."DocEntry" = T0."OrderEntry" and T4."ObjType" = T0."BaseObject"
         left outer join (select "U_PickEntry"     "PickEntry",
                                 Sum("U_Quantity") "Quantity"
                          from [@LW_YUVAL08_PKL1]
                          where "U_AbsEntry" = @AbsEntry and "U_Status" in ('O', 'P')
                          Group By "U_PickEntry") T6
                         on T6."PickEntry" = T0."PickEntry"
where T0."AbsEntry" = @AbsEntry
  and (@Type is null or T0."BaseObject" = @Type)
  and (@Entry is null or T0."OrderEntry" = @Entry)
group by T4."CardName", T3."CardName", T2."CardName", T4."CardCode", T3."CardCode", T2."CardCode", T4."DocNum", T3."DocNum", T2."DocNum", T0."OrderEntry", T0."BaseObject",
         T4."DocDate", T3."DocDate", T2."DocDate"
