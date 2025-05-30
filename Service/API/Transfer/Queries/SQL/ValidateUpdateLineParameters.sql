-- DECLARE @ID INT = 2,
--     @LineID INT = 26,
--     @Reason INT = NULL,
--     @Quantity INT = 88;

declare @ItemCode nvarchar(50),
    @BinEntry int,
    @Type char(1);
select @ItemCode = T0."U_ItemCode",
       @BinEntry = T0."U_BinEntry",
       @Type = T0."U_Type",
       @Quantity = @Quantity
           * Case When t0."U_Unit" > 0 Then T1."NumInBuy" Else 1 End
           * Case When t0."U_Unit" = 2 Then T1."PurPackUn" Else 1 End
from "@LW_YUVAL08_TRANS1" T0
         inner join OITM T1 on T1."ItemCode" = T0."U_ItemCode"
where T0.U_ID = @ID
  and T0."U_LineID" = @LineID;
-- If @Unit > 0
--     Begin
--         select @Quantity = @Quantity * COALESCE("NumInBuy", 1) * Case When @Unit = 2 Then COALESCE("PurPackUn", 1) Else 1 End
--         from OITM
--         where "ItemCode" = @ItemCode;
--     end

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
