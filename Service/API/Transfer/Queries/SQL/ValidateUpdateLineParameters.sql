-- DECLARE @ID INT = 2,
--     @LineID INT = 26,
--     @Reason INT = NULL,
--     @Quantity INT = 88;

declare @ItemCode nvarchar(50),
    @BinEntry int,
    @Type char(1);
select @ItemCode = "U_ItemCode", @BinEntry = "U_BinEntry", @Type = "U_Type"
from "@LW_YUVAL08_TRANS1"
where U_ID = @ID
  and "U_LineID" = @LineID;

select Case
           When T2."U_Status" <> 'I' Then -1
           When T3."U_LineStatus" <> 'O' Then -2
           When @Reason is not null and T4.Code is null Then -3
           When @Quantity is not null and @Type = 'S' and T3."U_BinEntry" is not null and T7.OnHandQty - T8.SourceQuantity < @Quantity Then -13
           When @Quantity is not null and @Type = 'T' and T8.SourceQuantity - T8.TargetQuantity < @Quantity Then -13
           Else 0 End ValidateMessage
from (select @ID ID, @LineID LineID) T0
         left outer join "@LW_YUVAL08_TRANS" T2 on T2.Code = T0.ID
         left outer join "@LW_YUVAL08_TRANS1" T3 on T3.U_ID = T0.ID and T3."U_LineID" = T0.LineID
         left outer join "@LW_YUVAL08_CR" T4 on T4."Code" = @Reason and T4."U_TRANS" = 'Y'
         left outer join OIBQ T7 on T7.ItemCode = T3."U_ItemCode" and T7."BinAbs" = T3."U_BinEntry"
         cross join (select COALESCE(Sum(IIF((X0.U_BinEntry = @BinEntry and @Type = 'S' or @Type = 'T') and X0."U_Type" = 'S', X0.U_Quantity, 0)), 0) SourceQuantity,
                            COALESCE(Sum(IIF(@Type = 'T' and X0."U_Type" = 'T', X0.U_Quantity, 0)), 0)                                                TargetQuantity
                     from "@LW_YUVAL08_TRANS1" X0
                     where X0.U_ID = @ID
                       and X0.U_ItemCode = @ItemCode
                       and X0.U_LineStatus <> 'C'
                       and X0."U_LineID" <> @LineID) T8
