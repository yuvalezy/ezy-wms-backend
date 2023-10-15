-- declare @ID int = 2015;
-- declare @LineID int = 35;
select Case
           When T2."U_Status" <> 'I' Then -1
           When T3."U_LineStatus" <> 'O' Then -2
           Else 0 End ValidateMessage
from (select @ID ID, @LineID LineID) T0
         left outer join "@LW_YUVAL08_GRPO" T2 on T2.Code = T0.ID
         left outer join "@LW_YUVAL08_GRPO1" T3 on T3.U_ID = T0.ID and T3."U_LineID" = T0.LineID
