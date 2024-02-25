-- declare @ID int = 2;
-- declare @BinEntry int = 3404;
select T0."U_ItemCode" "ItemCode", T1."ItemName", Sum(T0."U_Quantity") "Quantity"
from "@LW_YUVAL08_TRANS1" T0
         inner join OITM T1 on T1."ItemCode" = T0."U_ItemCode"
where T0.U_ID = @ID and (@BinEntry is null or T0."U_BinEntry" = @BinEntry) and T0."U_LineStatus" <> 'C' and T0."U_Type" = 'S'
Group By T0."U_ItemCode", T1."ItemName", T0."U_BinEntry"