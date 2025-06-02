declare @LineID int = IsNull((select Max("U_LineID") + 1
                              from "@LW_YUVAL08_TRANS1" where "U_ID" = @ID), 0);
if @Type = 'T' Begin
    set @BarCode = (select top 1 "U_BarCode" from "@LW_YUVAL08_TRANS1" where U_ID = @ID and "U_ItemCode" = @ItemCode);
end
If @Unit > 0
    Begin
        select @Quantity = @Quantity * COALESCE("NumInBuy", 1) * Case When @Unit = 2 Then COALESCE("PurPackUn", 1) Else 1 End
        from OITM
        where "ItemCode" = @ItemCode;
    end
insert into "@LW_YUVAL08_TRANS1"(U_ID, "U_LineID", "U_ItemCode", "U_BarCode", "U_empID", "U_Date", "U_BinEntry", "U_Quantity", "U_Type", "U_Unit")
values(@ID, @LineID, @ItemCode, @BarCode, @empID, getdate(), @BinEntry, @Quantity, @Type, @Unit);

update "@LW_YUVAL08_TRANS" set "U_Status" = 'I' where Code = @ID;

select @LineID                  LineID,
       COALESCE("NumInBuy", 1)  "NumInBuy",
       "BuyUnitMsr",
       COALESCE("PurPackUn", 1) "PurPackUn",
       "PurPackMsr"
from OITM
where "ItemCode" = @ItemCode;
