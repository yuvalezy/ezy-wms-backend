-- declare @ID int = 1007;
-- declare @BinEntry int = (select AbsEntry
--                          from OBIN
--                          where BinCode = 'SM-G01-R01-P01-F05');
SELECT c."BinCode",
       a."U_ItemCode"                                                                                                      "ItemCode",
       b."ItemName",
       Sum(Case When a."U_Unit" = 0 Then a."U_Quantity" Else 0 End)                                                        "Unit",
       Sum(Case When a."U_Unit" = 1 Then a."U_Quantity" / COALESCE(b."NumInBuy", 1) Else 0 End)                            "Dozen",
       Sum(Case When a."U_Unit" = 2 Then a."U_Quantity" / COALESCE(b."NumInBuy", 1) / COALESCE("PurPackUn", 1) Else 0 End) "Pack"
FROM "@LW_YUVAL08_OINC1" a
         inner join OITM b on b."ItemCode" = a."U_ItemCode"
         inner join OBIN c on c."AbsEntry" = a."U_BinEntry"
WHERE a.U_ID = @ID
  AND a."U_LineStatus" <> 'C'
GROUP BY c."BinCode", a."U_ItemCode", b."ItemName"
order by 1

