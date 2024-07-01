-- declare @ID int = 1045;
WITH Data AS (select T0."U_ItemCode"      "ItemCode",
                     T1."ItemName",
                     Sum(T2."U_Quantity") "Quantity",
                     T2."U_SourceType"    "BaseType",
                     T2."U_SourceEntry"   "BaseEntry",
                     T2."U_SourceLine"    "BaseLine"
              from "@LW_YUVAL08_GRPO1" T0
                       inner join OITM T1 on T1."ItemCode" = T0."U_ItemCode"
                       inner join "@LW_YUVAL08_GRPO4" T2 on T2.U_ID = T0.U_ID and T2."U_LineID" = T0."U_LineID"
              where T0.U_ID = @ID
              group by Case T2.U_SourceEntry When -1 Then 2 Else 0 End, T2.U_SourceEntry, T2.U_SourceLine, T0."U_ItemCode", T1."PurPackUn", T2."U_SourceType", T1."ItemName")
select COALESCE(T1.DocNum, T2.DocNum)                          "DocNum",
       COALESCE(T4.VisOrder, T5.VisOrder)                      "VisOrder",
       COALESCE(T1."CardCode", T2."CardCode", T3."U_CardCode") "CardCode",
       COALESCE(T1."CardName", T2."CardName")                  "CardName",
       T0."ItemCode",
       T0."ItemName",
       Sum(T0."Quantity")                                      "Quantity",
       COALESCE(T0."BaseType", -1)                             "BaseType",
       COALESCE(T0."BaseEntry", -1)                            "BaseEntry",
       COALESCE(T0."BaseLine", -1)                             "BaseLine",
       COALESCE(T4.OpenInvQty, T5.OpenInvQty, 0)               "OpenInvQty",
       Case
           When COALESCE(T4.OpenInvQty, T5.OpenInvQty, 0) = Sum(T0."Quantity") Then 0 --Line OK
           When COALESCE(T4.OpenInvQty, T5.OpenInvQty, 0) < Sum(T0."Quantity") Then 1 --Open Quantity is less then scanned quantity
           When COALESCE(T4.OpenInvQty, T5.OpenInvQty, 0) > Sum(T0."Quantity") Then 2 --Open Quantity is more then scanned Quantity
           When COALESCE(T4.InvntSttus, T5.InvntSttus, 'O') = 'C' Then 3 --Line is closed
           End                                                 "LineStatus"
from Data T0
         left outer join OPOR T1 on T1."DocEntry" = T0."BaseEntry" and T1."ObjType" = T0."BaseType"
         left outer join OPCH T2 on T2."DocEntry" = T0."BaseEntry" and T2."ObjType" = T0."BaseType"
         inner join "@LW_YUVAL08_GRPO" T3 on T3.Code = @ID
         left outer join POR1 T4 on T4.DocEntry = T1.DocEntry and T4.LineNum = T0.BaseLine and T4.ObjType = T1.ObjType
         left outer join PCH1 T5 on T5.DocEntry = T1.DocEntry and T5.LineNum = T0.BaseLine and T5.ObjType = T1.ObjType
Group By T0."ItemCode", T0."BaseType", T0."BaseEntry", T0."BaseLine", T1."CardCode", T2."CardCode", T3."U_CardCode", T4.InvntSttus, T5.InvntSttus, T4.OpenInvQty, T5.OpenInvQty, T4.VisOrder,
         T5.VisOrder, T1.DocNum, T2.DocNum, T1."CardName", T2."CardName", T0."ItemName"
Having Sum(T0."Quantity") > 0
order by 1, 2, 3
