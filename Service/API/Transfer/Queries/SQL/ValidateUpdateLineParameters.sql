-- declare @ID int = 2;
-- declare @LineID int = 13;
-- declare @Reason int = 2;
-- declare @Quantity int = 67;

declare @ItemCode nvarchar(50);
declare @BinEntry int;
select @ItemCode = "U_ItemCode", @BinEntry = "U_BinEntry" from "@LW_YUVAL08_TRANS1" where U_ID = @ID and "U_LineID" = @LineID;

select Case
           When T2."U_Status" <> 'I' Then -1
           When T3."U_LineStatus" <> 'O' Then -2
            When @Reason is not null and T4.Code is null Then -3
           When @Quantity is not null and T3."U_BinEntry" is not null and T7.OnHandQty - T8.SelectedQty < @Quantity Then -13
           Else 0 End ValidateMessage,
       T7.OnHandQty - T8.SelectedQty 
from (select @ID ID, @LineID LineID) T0
         left outer join "@LW_YUVAL08_TRANS" T2 on T2.Code = T0.ID
         left outer join "@LW_YUVAL08_TRANS1" T3 on T3.U_ID = T0.ID and T3."U_LineID" = T0.LineID
         left outer join "@LW_YUVAL08_CR" T4 on T4."Code" = @Reason and T4."U_TRANS" = 'Y'
         left outer join OIBQ T7 on T7.ItemCode = T3."U_ItemCode" and T7."BinAbs" = T3."U_BinEntry"
         cross join (select COALESCE(Sum(X0.U_Quantity), 0) "SelectedQty"
                     from "@LW_YUVAL08_TRANS1" X0
                     where X0.U_ID = @ID
                       and X0.U_BinEntry = @BinEntry
                       and X0.U_ItemCode = @ItemCode
                       and X0."U_LineID" <> @LineID
                       and X0.U_LineStatus <> 'C') T8
