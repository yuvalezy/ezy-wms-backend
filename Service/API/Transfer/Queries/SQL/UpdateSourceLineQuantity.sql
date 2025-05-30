update T0
set "U_Quantity" = @Quantity
    * Case When T0."U_Unit" > 0 Then COALESCE("NumInBuy", 1) Else 1 End
    * Case When T0."U_Unit" = 2 Then COALESCE("PurPackUn", 1) Else 1 End
from "@LW_YUVAL08_TRANS1" T0
         inner join OITM T1 on T1."ItemCode" = T0."U_ItemCode"
where T0.U_ID = @ID
  and T0."U_LineID" = @LineID;