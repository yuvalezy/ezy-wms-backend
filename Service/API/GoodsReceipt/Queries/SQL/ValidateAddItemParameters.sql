select Case 
    When T0.ItemCode is null Then -1 
    When T0.BarCode <> T1.CodeBars Then -2
    When T2.Code is null Then -3
    When T2.U_Status not in ('O', 'I') Then -4
    Else 0 End ValidationMessage
from (select @ID ID, @BarCode BarCode, @ItemCode ItemCode) T0
         left outer join OITM T1 on T1.ItemCode = T0.ItemCode
left outer join "@LW_YUVAL08_GRPO" T2 on T2.Code = T0.ID
