declare @LineID int = IsNull((select Max("U_LineID") + 1
                              from "@LW_YUVAL08_TRANS1" where "U_ID" = @ID), 0);
insert into "@LW_YUVAL08_TRANS1"(U_ID, "U_LineID", "U_ItemCode", "U_BarCode", "U_empID", "U_Date", "U_BinEntry", "U_Quantity")
values(@ID, @LineID, @ItemCode, @BarCode, @empID, getdate(), @BinEntry, @Quantity);

update "@LW_YUVAL08_TRANS" set "U_Status" = 'I' where Code = @ID;

select @LineID LineID