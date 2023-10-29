-- declare @ID int = 22;
WITH Data AS (select T0."U_ItemCode"                                                                                          "ItemCode",
                     Sum(T0."U_Quantity")                                                                                     "Quantity",
                     T0."U_QtyPerUnit"                                                                                        "NumPerMsr",
                     T0."U_SourceType"                                                                                        "BaseType",
                     T0."U_SourceEntry"                                                                                       "BaseEntry",
                     T0."U_SourceLine"                                                                                        "BaseLine",
                     Case When T0."U_SourceEntry" <> -1 and COALESCE(T1."NumInBuy", 1) <> T0."U_QtyPerUnit" Then 1 Else 0 End "UseBaseUn"
              from "@LW_YUVAL08_GRPO1" T0
                       inner join OITM T1 on T1."ItemCode" = T0."U_ItemCode"
              where T0.U_ID = @ID
              group by Case T0.U_SourceEntry When -1 Then 2 Else 0 End, T0.U_SourceEntry, T0.U_SourceLine, T0."U_ItemCode", T0."U_QtyPerUnit", T1."NumInBuy", T0."U_SourceType"),
     EndData AS (select X0."ItemCode"
                      , Case
                            When Exists (select 1 from Data where "ItemCode" = X0."ItemCode" and "BaseEntry" = X0."BaseEntry" and "BaseLine" = X0."BaseLine" and "UseBaseUn" = 1)
                                Then 1
                            Else 0 End                              "UseBaseUn"
                      , Case
                            When Not Exists (select 1
                                             from Data
                                             where "ItemCode" = X0."ItemCode"
                                               and "BaseEntry" = X0."BaseEntry"
                                               and "BaseLine" = X0."BaseLine"
                                               and "UseBaseUn" = 1)
                                Then X0."Quantity"
                            Else X0."Quantity" * X0."NumPerMsr" End "Quantity"
                      , X0."BaseType"
                      , X0."BaseEntry"
                      , X0."BaseLine"
                 from Data X0)
select T0."ItemCode", Sum(T0."UseBaseUn") "UseBaseUn", Sum(T0."Quantity") "Quantity", COALESCE(T1."CardCode", T2."CardCode") "CardCode", T0."BaseType", T0."BaseEntry", T0."BaseLine"
from EndData T0
left outer join OPOR T1 on T1."DocEntry" = T0."BaseEntry" and T1."ObjType" = T0."BaseType"
left outer join OPCH T2 on T2."DocEntry" = T0."BaseEntry" and T2."ObjType" = T0."BaseType"
Group By T0."ItemCode", T0."BaseType", T0."BaseEntry", T0."BaseLine", T1."CardCode", T2."CardCode"
order by T0."ItemCode"