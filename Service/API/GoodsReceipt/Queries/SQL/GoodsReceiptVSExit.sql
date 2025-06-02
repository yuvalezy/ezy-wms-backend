-- declare @ID int = 1060;
select T0."U_TargetType"                                                        "ObjType",
       COALESCE(T1."DocNum", T3."DocNum", T5."DocNum")                          "DocNum",
       COALESCE(T1."CardName", T3."CardName", T5."CardName")                    "CardName",
       COALESCE(T1."Address2", T3."Address2", T5."Address2")                    "Address2",
       T0."U_ItemCode"                                                          "ItemCode",
       T7."ItemName",
       Cast(COALESCE(T2."OpenInvQty", T4."OpenInvQty", T6."OpenInvQty") as int) "OpenInvQty",
       Count(1)                                                                 "Quantity",
       COALESCE(T7."NumInBuy", 1)                                               "NumInBuy",
       T7."BuyUnitMsr",
       COALESCE(T7."PurPackUn", 1)                                              "PurPackUn",
       T7."PurPackMsr",
       T8."U_Unit"                                                              "Unit"
from "@LW_YUVAL08_GRPO2" T0
         left outer join ORDR T1 on T1."DocEntry" = T0."U_TargetEntry" and T1."ObjType" = T0."U_TargetType"
         left outer join RDR1 T2 on T2."DocEntry" = T0."U_TargetEntry" and T2."ObjType" = T0."U_TargetType" and T2."LineNum" = T0."U_TargetLine"
         left outer join OINV T3 on T3."DocEntry" = T0."U_TargetEntry" and T3."ObjType" = T0."U_TargetType"
         left outer join INV1 T4 on T4."DocEntry" = T0."U_TargetEntry" and T4."ObjType" = T0."U_TargetType" and T4."LineNum" = T0."U_TargetLine"
         left outer join OWTQ T5 on T5."DocEntry" = T0."U_TargetEntry" and T5."ObjType" = T0."U_TargetType"
         left outer join WTQ1 T6 on T6."DocEntry" = T0."U_TargetEntry" and T6."ObjType" = T0."U_TargetType" and T6."LineNum" = T0."U_TargetLine"
         inner join OITM T7 on T7."ItemCode" = T0."U_ItemCode"
         inner join "@LW_YUVAL08_GRPO1" T8 on T8."U_ID" = T0."U_ID" and T8."U_LineID" = T0."U_lineID"
where T0.U_ID = @ID
group by T6."OpenInvQty", T4."OpenInvQty", T2."OpenInvQty", T5."CardName", T3."CardName", T1."CardName", T7."ItemName", T0."U_ItemCode", T5."DocNum", T3."DocNum", T1."DocNum",
         T0."U_TargetType", T1."Address2", T3."Address2", T5."Address2", T7."NumInBuy", T7."BuyUnitMsr", T7."PurPackUn", T7."PurPackMsr", T8."U_Unit"
order by T0."U_TargetType", "DocNum", "ItemCode"
