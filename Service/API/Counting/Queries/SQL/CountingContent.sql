-- declare @ID int = 1007;
-- declare @BinEntry int = (select AbsEntry
--                          from OBIN
--                          where BinCode = 'SM-G01-R01-P01-F05');

select T0."U_ItemCode"                                                                                                        "ItemCode"
     , T1."ItemName"
     , Sum(T0."U_Quantity")                                                                                                   "Quantity"
     , Sum(Case When T0."U_Unit" = 0 Then T0."U_Quantity" Else 0 End)                                                         "Unit"
     , Sum(Case When T0."U_Unit" = 1 Then T0."U_Quantity" / COALESCE(T1."NumInBuy", 1) Else 0 End)                            "Dozen"
     , Sum(Case When T0."U_Unit" = 2 Then T0."U_Quantity" / COALESCE(T1."NumInBuy", 1) / COALESCE("PurPackUn", 1) Else 0 End) "Pack"
from "@LW_YUVAL08_OINC1" T0
         inner join OITM T1 on T1."ItemCode" = T0."U_ItemCode"
where T0.U_ID = @ID
  and (@BinEntry is null or T0."U_BinEntry" = @BinEntry)
  and T0."U_LineStatus" <> 'C'
Group By T0."U_ItemCode", T1."ItemName", T0."U_BinEntry"