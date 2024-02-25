-- DECLARE @ID INT = 2,
--     @ItemCode NVARCHAR(50) = N'0000002',
--     @BarCode NVARCHAR(254) = NULL,
--     @empID INT = 1,
--     @BinEntry INT = 3430,
--     @Quantity INT = 1,
--     @Type char(1) = 'T';

declare @WhsCode nvarchar(8) = (select U_LW_Branch
                                from OHEM
                                where empID = @empID);
select Case
           When T1.ItemCode is null Then -1
           When T0.BarCode <> T1.CodeBars and T3.BcdCode is null Then -2
           When T2.Code is null Then -3
           When T2.U_Status not in ('O', 'I') Then -4
           When T1.InvntItem = 'N' Then -8
           When T4.WhsCode is null Then -9
           When @BinEntry is not null and T5.AbsEntry is null Then -10
           When @BinEntry is not null and T5.WhsCode <> @WhsCode Then -11
           When @BinEntry is null and T6.BinActivat = 'Y' Then -12
           When @BinEntry is not null and @Type = 'S' and COALESCE(T7.OnHandQty, 0) - T8.SourceQuantity < @Quantity Then -13
           When @BinEntry is not null and @Type = 'T' and T8.SourceQuantity - T8.TargetQuantity < @Quantity Then -13
           Else 0 End ValidationMessage
from (select @ID ID, @BarCode BarCode, @ItemCode ItemCode) T0
         left outer join OITM T1 on T1.ItemCode = T0.ItemCode
         left outer join "@LW_YUVAL08_TRANS" T2 on T2.Code = T0.ID
         left outer join OBCD T3 on T3.ItemCode = T0.ItemCode and T3.BcdCode = @BarCode
         left outer join OITW T4 on T4.ItemCode = T1.ItemCode and T4.WhsCode = @WhsCode
         left outer join OBIN T5 on T5.AbsEntry = @BinEntry
         left outer join OWHS T6 on T6.WhsCode = @WhsCode
         left outer join OIBQ T7 on T7.ItemCode = T0.ItemCode and T7.BinAbs = @BinEntry
         cross join (select COALESCE(Sum(IIF((X0.U_BinEntry = @BinEntry and @Type = 'S' or @Type = 'T') and X0."U_Type" = 'S', X0.U_Quantity, 0)), 0) SourceQuantity,
                            COALESCE(Sum(IIF(@Type = 'T' and X0."U_Type" = 'T', X0.U_Quantity, 0)), 0)                                                TargetQuantity
                     from "@LW_YUVAL08_TRANS1" X0
                     where X0.U_ID = @ID
                       and X0.U_ItemCode = @ItemCode
                       and X0.U_LineStatus <> 'C') T8;