declare @LineID int = IsNull((select Max("U_LineID") + 1
                              from "@LW_YUVAL08_OINC1"
                              where "U_ID" = @ID), 0);
If @Unit > 0
    Begin
        select @Quantity = @Quantity * COALESCE("NumInBuy", 1) * Case When @Unit = 2 Then COALESCE("PurPackUn", 1) Else 1 End
        from OITM
        where "ItemCode" = @ItemCode;
    end
insert into "@LW_YUVAL08_OINC1"(U_ID, "U_LineID", "U_ItemCode", "U_BarCode", "U_empID", "U_Date", "U_BinEntry", "U_Quantity", "U_Unit")
values (@ID, @LineID, @ItemCode, @BarCode, @empID, getdate(), @BinEntry, @Quantity, @Unit);

update "@LW_YUVAL08_OINC"
set "U_Status" = 'I'
where Code = @ID;

select @LineID                  LineID,
       COALESCE("NumInBuy", 1)  "NumInBuy",
       "BuyUnitMsr",
       COALESCE("PurPackUn", 1) "PurPackUn",
       "PurPackMsr"
from OITM
where "ItemCode" = @ItemCode;
