-- declare @ID int = 2;
select T0."U_ItemCode"      "ItemCode",
       T0."U_Type"          "Type",
       T0."U_BinEntry"      "BinEntry",
       Sum(T0."U_Quantity") "Quantity"
from "@LW_YUVAL08_TRANS1" T0
where T0.U_ID = @ID
  and T0."U_LineStatus" <> 'C'
group by T0."U_ItemCode", T0."U_Type", T0."U_BinEntry"