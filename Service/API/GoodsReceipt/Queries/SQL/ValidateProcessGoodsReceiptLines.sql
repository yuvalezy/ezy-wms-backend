-- declare @ID int = 1055;
With Docs AS (select "U_ObjType" "BaseType", "U_DocEntry" "BaseEntry"
              from "@LW_YUVAL08_GRPO3"
              where U_ID = @ID
              union
              select "U_SourceType", "U_SourceEntry"
              from "@LW_YUVAL08_GRPO4"
              where U_ID = @ID),
     Data AS (select T2."U_SourceType"    "BaseType",
                     T2."U_SourceEntry"   "BaseEntry",
                     T2."U_SourceLine"    "BaseLine",
                     Sum(T2."U_Quantity") "Quantity"
              from "@LW_YUVAL08_GRPO1" T0
                       inner join "@LW_YUVAL08_GRPO4" T2 on T2.U_ID = T0.U_ID and T2."U_LineID" = T0."U_LineID"
              where T0.U_ID = @ID
                and T0."U_LineStatus" <> 'C'
              group by Case T2.U_SourceEntry When -1 Then 2 Else 0 End, T2.U_SourceEntry, T2.U_SourceLine, T0."U_ItemCode", T2."U_SourceType")
select COALESCE(T1.DocNum, T2.DocNum)                                         "DocNum",
       COALESCE(T4.VisOrder, T5.VisOrder)                                     "VisOrder",
       COALESCE(T1."CardCode", T2."CardCode", T3."U_CardCode")                "CardCode",
       COALESCE(T1."CardName", T2."CardName")                                 "CardName",
       COALESCE(T4."ItemCode", T5."ItemCode")                                 "ItemCode",
       T8."ItemName",
       COALESCE(Sum(T6."Quantity"), 0)                                        "Quantity",
       COALESCE(T0."BaseType", -1)                                            "BaseType",
       COALESCE(T0."BaseEntry", -1)                                           "BaseEntry",
       COALESCE(T6."BaseLine", -1)                                            "BaseLine",
       COALESCE(T4.OpenInvQty, T5.OpenInvQty, 0) / COALESCE(T8."NumInBuy", 1) "OpenInvQty",
       COALESCE(T8."PurPackUn", 1)                                            "PackUnit",
       T8."BuyUnitMsr",
       Case
           When COALESCE(T4.OpenInvQty, T5.OpenInvQty, 0) = Sum(T6."Quantity") Then 0 --Line OK
           When COALESCE(T4.OpenInvQty, T5.OpenInvQty, 0) < Sum(T6."Quantity") Then 1 --Open Quantity is less then scanned quantity
           When COALESCE(T4.OpenInvQty, T5.OpenInvQty, 0) > Sum(T6."Quantity") Then 2 --Open Quantity is more then scanned Quantity
           When COALESCE(T4.InvntSttus, T5.InvntSttus, 'O') = 'C' Then 3 --Line is closed
           When COALESCE(Sum(T6."Quantity"), 0) = 0 Then 4
           End                                                                "LineStatus"
-- from Data T0
from Docs T0
         left outer join OPOR T1 on T1."DocEntry" = T0."BaseEntry" and T1."ObjType" = T0."BaseType"
         left outer join OPCH T2 on T2."DocEntry" = T0."BaseEntry" and T2."ObjType" = T0."BaseType"
         left outer join "@LW_YUVAL08_GRPO" T3 on T3.Code = @ID
         left outer join POR1 T4 on T4.DocEntry = T1.DocEntry and T4.ObjType = T1.ObjType
         left outer join PCH1 T5 on T5.DocEntry = T1.DocEntry and T5.ObjType = T1.ObjType
         left outer join Data T6 on T6."BaseType" = T0."BaseType" and T6."BaseEntry" = T0."BaseEntry" and T6."BaseLine" = COALESCE(T4."LineNum", T5."LineNum")
         inner join OITM T8 on T8."ItemCode" = COALESCE(T4."ItemCode", T5."ItemCode")
Group By T0."BaseType", T0."BaseEntry", T6."BaseLine", T1."CardCode", T2."CardCode", T3."U_CardCode", T4.InvntSttus, T5.InvntSttus, T4.OpenInvQty, T5.OpenInvQty, T4.VisOrder,
         T5.VisOrder, T1.DocNum, T2.DocNum, T1."CardName", T2."CardName", T4."ItemCode", T5."ItemCode", T8."ItemName", T8."PurPackUn", T8."BuyUnitMsr", T8."NumInBuy"
-- Having Sum(T6."Quantity") > 0
order by 1, 2, 3
