-- declare @ID int = 5;
-- declare @SourceType int = 17;
-- declare @SourceEntry int = 528;
-- declare @ItemCode nvarchar(50) = 'SCSIW';
-- declare @empID int = 1;

declare @WhsCode nvarchar(8) = (select U_LW_Branch from OHEM where empID = @empID);
select top 1 T0."PickEntry", Case When @Quantity > T0."RelQtty" - COALESCE(T5.Quantity, 0) Then -7 Else 0 End
from PKL1 T0
         inner join OILM T1 on T1.TransType = T0.BaseObject and T1.DocEntry = T0.OrderEntry and T1.DocLineNum = T0.OrderLine and T1.ItemCode = @ItemCode and T1.LocCode = @WhsCode
         left outer join (
    select "U_PickEntry" "PickEntry", Sum("U_Quantity") "Quantity"
    from "@LW_YUVAL08_PKL1"
    where U_AbsEntry = @ID and "U_Status" in ('O', 'P')
    Group By "U_PickEntry"
) T5 on T5."PickEntry" = T0."PickEntry"
where T0."AbsEntry" = @ID and T0."BaseObject" = @SourceType and T0."OrderEntry" = @SourceEntry
