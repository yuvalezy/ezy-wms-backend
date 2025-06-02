If @Unit > 0
    Begin
        select @Quantity = @Quantity * COALESCE("NumInBuy", 1) * Case When @Unit = 2 Then COALESCE("PurPackUn", 1) Else 1 End
        from OITM
        where "ItemCode" = @ItemCode;
    end
insert into "@LW_YUVAL08_PKL1"("U_AbsEntry", "U_PickEntry", "U_Status", "U_ErrorMessage", "U_Quantity", "U_empID", "U_ItemCode", "U_BinEntry", "U_Unit")
values(@AbsEntry, @PickEntry, 'O', null, @Quantity, @empID, @ItemCode, @BinEntry, @Unit)