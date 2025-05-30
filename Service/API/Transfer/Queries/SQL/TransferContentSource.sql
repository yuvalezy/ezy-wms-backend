-- declare @ID int = 3012;
-- declare @BinEntry int = 3412;
select T0."U_ItemCode"                                              "ItemCode",
       T1."ItemName",
       T0."U_Unit" "Unit",
       T1."NumInBuy",
       T1."BuyUnitMsr",
       T1."PurPackUn",
       T1."PurPackMsr",
       Sum(T0."U_Quantity")
           / Case When T0."U_Unit" > 0 Then T1."NumInBuy" Else 1 End
           / Case T0."U_Unit" When 2 Then T1."PurPackUn" else 1 End "Quantity"
from "@LW_YUVAL08_TRANS1" T0
         inner join OITM T1 on T1."ItemCode" = T0."U_ItemCode"
where T0.U_ID = @ID
  and (@BinEntry is null or T0."U_BinEntry" = @BinEntry)
  and T0."U_LineStatus" <> 'C'
  and T0."U_Type" = 'S'
Group By T0."U_ItemCode", T1."ItemName", T0."U_BinEntry", T0."U_Unit", T1."BuyUnitMsr", T1."NumInBuy", T1."PurPackMsr", T1."PurPackUn"