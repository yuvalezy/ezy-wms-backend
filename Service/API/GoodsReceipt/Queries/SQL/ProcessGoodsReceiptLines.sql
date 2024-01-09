-- update "@LW_YUVAL08_GRPO" set U_Status = 'I' where Code = 32;
-- declare @ID int = 32;
WITH Data AS (select T0."U_ItemCode"      "ItemCode",
                     Sum(T0."U_Quantity") "Quantity",
                     T0."U_SourceType"    "BaseType",
                     T0."U_SourceEntry"   "BaseEntry",
                     T0."U_SourceLine"    "BaseLine"
              from "@LW_YUVAL08_GRPO1" T0
                       inner join OITM T1 on T1."ItemCode" = T0."U_ItemCode"
              where T0.U_ID = @ID
              group by Case T0.U_SourceEntry When -1 Then 2 Else 0 End, T0.U_SourceEntry, T0.U_SourceLine, T0."U_ItemCode", T1."PurPackUn", T0."U_SourceType")
select T0."ItemCode",
       Sum(T0."Quantity")                                      "Quantity",
       COALESCE(T1."CardCode", T2."CardCode", T3."U_CardCode") "CardCode",
       COALESCE(T0."BaseType", -1)                             "BaseType",
       COALESCE(T0."BaseEntry", -1)                            "BaseEntry",
       COALESCE(T0."BaseLine", -1)                             "BaseLine",
       COALESCE(T4.InvntSttus, T5.InvntSttus, 'O') "LineStatus"
from Data T0
         left outer join OPOR T1 on T1."DocEntry" = T0."BaseEntry" and T1."ObjType" = T0."BaseType"
         left outer join OPCH T2 on T2."DocEntry" = T0."BaseEntry" and T2."ObjType" = T0."BaseType"
         inner join "@LW_YUVAL08_GRPO" T3 on T3.Code = @ID
         left outer join POR1 T4 on T4.DocEntry = T1.DocEntry and T4.LineNum = T0.BaseLine and T4.ObjType = T1.ObjType
         left outer join PCH1 T5 on T5.DocEntry = T1.DocEntry and T5.LineNum = T0.BaseLine and T5.ObjType = T1.ObjType
Group By T0."ItemCode", T0."BaseType", T0."BaseEntry", T0."BaseLine", T1."CardCode", T2."CardCode", T3."U_CardCode", T4.InvntSttus, T5.InvntSttus
order by T0."ItemCode"