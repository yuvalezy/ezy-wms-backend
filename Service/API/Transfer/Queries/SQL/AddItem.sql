declare @LineID int = IsNull((select Max("U_LineID") + 1
                              from "@LW_YUVAL08_TRANS1" where "U_ID" = @ID), 0);
if @Type = 'T' Begin
    set @BarCode = (select top 1 "U_BarCode" from "@LW_YUVAL08_TRANS1" where U_ID = @ID and "U_ItemCode" = @ItemCode);
end
insert into "@LW_YUVAL08_TRANS1"(U_ID, "U_LineID", "U_ItemCode", "U_BarCode", "U_empID", "U_Date", "U_BinEntry", "U_Quantity", "U_Type")
values(@ID, @LineID, @ItemCode, @BarCode, @empID, getdate(), @BinEntry, @Quantity, @Type);

update "@LW_YUVAL08_TRANS" set "U_Status" = 'I' where Code = @ID;

select @LineID LineID