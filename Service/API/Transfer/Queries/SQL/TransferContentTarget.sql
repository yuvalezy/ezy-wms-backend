-- declare @ID int = 2;
-- declare @ItemCode nvarchar(50);

select T1."ItemCode",
       T1."ItemName",
       Sum(IIF(T0."U_Type" = 'S', T0."U_Quantity", 0))                                                         "Quantity",
       Sum(IIF(T0."U_Type" = 'S', T0."U_Quantity", 0)) - Sum(IIF(T0."U_Type" = 'T', T0."U_Quantity", 0))       "OpenQuantity",
       Sum(IIF(T0."U_Type" = 'T', T0."U_Quantity", 0)) * 100 / Sum(IIF(T0."U_Type" = 'S', T0."U_Quantity", 0)) "Progress"
from "@LW_YUVAL08_TRANS1" T0
         inner join OITM T1 on T1."ItemCode" = T0."U_ItemCode"
where T0.U_ID = @ID
  and T0."U_LineStatus" <> 'C'
  and (@ItemCode is null or T0."U_ItemCode" = @ItemCode)
group by T0."U_ItemCode", T1."ItemCode", T1."ItemName"
-- Having Sum(IIF(T0."U_Type" = 'S', T0."U_Quantity", 0)) <> Sum(IIF(T0."U_Type" = 'T', T0."U_Quantity", 0))
