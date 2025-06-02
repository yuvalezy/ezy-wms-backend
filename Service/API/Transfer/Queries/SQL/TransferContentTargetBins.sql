-- declare @ID int = 2;
-- declare @ItemCode nvarchar(50) = '0000002';

select T0."U_ItemCode" "ItemCode", T1."AbsEntry" "Entry", T1."BinCode" "Code", Sum(T0."U_Quantity") "Quantity"
from "@LW_YUVAL08_TRANS1" T0
inner join OBIN T1 on T1."AbsEntry" = T0."U_BinEntry"
where T0.U_ID = @ID and T0."U_LineStatus" <> 'C' and (@ItemCode is null or T0."U_ItemCode" = @ItemCode) and T0."U_Type" = 'T'
group by T1."AbsEntry", T1."BinCode", T0."U_ItemCode"

-- select T1."ItemCode", T1."ItemName", Sum(IIF(T0."U_Type" = 'S', T0."U_Quantity", 0)) - Sum(IIF(T0."U_Type" = 'T', T0."U_Quantity", 0)) "Quantity", Sum(IIF(T0."U_Type" = 'T', T0."U_Quantity", 0)) * 100 / Sum(IIF(T0."U_Type" = 'S', T0."U_Quantity", 0)) "Progress"
-- from "@LW_YUVAL08_TRANS1" T0
--          inner join OITM T1 on T1."ItemCode" = T0."U_ItemCode"
-- where T0.U_ID = @ID and T0."U_LineStatus" <> 'C'
-- group by T0."U_ItemCode", T0."U_Type", T1."ItemCode", T1."ItemName"
-- Having Sum(IIF(T0."U_Type" = 'S', T0."U_Quantity", 0)) <> Sum(IIF(T0."U_Type" = 'T', T0."U_Quantity", 0))
