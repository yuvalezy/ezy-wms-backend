-- declare @ID int = 1;
-- declare @BarCode nvarchar(255) = 'xxx1234567890123';
-- declare @ItemCode nvarchar(50) = 'SCSIW';
-- declare @empID int = 1;
declare @WhsCode nvarchar(8) = (select U_LW_Branch from OHEM where empID = @empID);
-- declare @BinEntry int = 3500;
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
    Else 0 End ValidationMessage
from (select @ID ID, @BarCode BarCode, @ItemCode ItemCode) T0
         left outer join OITM T1 on T1.ItemCode = T0.ItemCode
left outer join "@LW_YUVAL08_OINC" T2 on T2.Code = T0.ID
         left outer join OBCD T3 on T3.ItemCode = T0.ItemCode and T3.BcdCode = @BarCode
left outer join OITW T4 on T4.ItemCode = T1.ItemCode and T4.WhsCode = @WhsCode
left outer join OBIN T5 on T5.AbsEntry = @BinEntry
left outer join OWHS T6 on T6.WhsCode = @WhsCode
