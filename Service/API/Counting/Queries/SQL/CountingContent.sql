select T0."U_ItemCode" "ItemCode", T1."ItemName", Sum(T0."U_Quantity") "Quantity"
from "@LW_YUVAL08_OINC1" T0
         inner join OITM T1 on T1."ItemCode" = T0."U_ItemCode"
where T0.U_ID = @ID and (@BinEntry is null or T0."U_BinEntry" = @BinEntry) and T0."U_LineStatus" <> 'C'
Group By T0."U_ItemCode", T1."ItemName"