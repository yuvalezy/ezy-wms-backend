-- declare @ID int = 6;
WITH Data AS (select T0."U_ItemCode"                                                                         "ItemCode",
                     Sum(T0."U_Quantity")                                                                    "Quantity",
                     T0."U_QtyPerUnit"                                                                       "NumPerMsr",
                     T0."U_POEntry"                                                                          "BaseEntry",
                     T0."U_POLine"                                                                           "BaseLine",
                     Case When T0."U_POEntry" <> -1 and COALESCE(T1."NumInBuy", 1) <> T0."U_QtyPerUnit" Then 1 Else 0 End "UseBaseUn"
              from "@LW_YUVAL08_GRPO1" T0
                       inner join OITM T1 on T1."ItemCode" = T0."U_ItemCode"
              where T0.U_ID = @ID
              group by Case T0.U_POEntry When -1 Then 2 Else 0 End, T0.U_POEntry, T0.U_POLine, T0."U_ItemCode", T0."U_QtyPerUnit", T1."NumInBuy"),
     EndData AS (select X0."ItemCode"
                      , Case
                            When Exists (select 1 from Data where "ItemCode" = X0."ItemCode" and "BaseEntry" = X0."BaseEntry" and "BaseLine" = X0."BaseLine" and "UseBaseUn" = 1)
                                Then 1
                            Else 0 End                              "UseBaseUn"
                      , Case
                            When Not Exists (select 1
                                             from Data
                                             where "ItemCode" = X0."ItemCode" and "BaseEntry" = X0."BaseEntry" and "BaseLine" = X0."BaseLine" and "UseBaseUn" = 1)
                                Then X0."Quantity"
                            Else X0."Quantity" * X0."NumPerMsr" End "Quantity"
                      , X0."BaseEntry"
                      , X0."BaseLine"
                 from Data X0)
select "ItemCode", Sum("UseBaseUn") "UseBaseUn", Sum("Quantity") "Quantity", "BaseEntry", "BaseLine"
from EndData
Group By "ItemCode", "BaseEntry", "BaseLine"
order by "ItemCode"