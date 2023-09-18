-- declare @ID int = 1;
-- declare @BarCode nvarchar(254) = '5678901234567';
-- declare @ItemCode nvarchar(50) = 'SCS';
-- declare @empID int = 1;

SET NOCOUNT ON;
--set default return value to Store in Warehouse
declare @ReturnValue int = 1;
declare @WhsCode nvarchar(8) = (select U_LW_Branch from OHEM where empID = @empID);

declare @POEntry int;
declare @POLine int;

declare @TargetType int;
declare @TargetEntry int;
declare @TargetLine int;

--get first open purchase order in the connected branch
select top 1 @POEntry = T0."DocEntry", @POLine = T0."LineNum"
from POR1 T0
         inner join OPOR T1 on T1."DocEntry" = T0."DocEntry" and T1."DocStatus" = 'O'
where T0."ItemCode" = @ItemCode and T0."LineStatus" = 'O' and T0."WhsCode" = @WhsCode and T0."OpenQty" - IsNull(T0.U_LW_SCAN_QTY, 0) > 0
order by T1."CreateDate";

--update the purchase order scanned quantity
update POR1 set U_LW_SCAN_QTY = IsNull(U_LW_SCAN_QTY, 0) + 1 where "DocEntry" = @POEntry and LineNum = @POLine;

--check if I have unfulfilled reserved invoice
select top 1 @TargetType = T0."ObjType", @TargetEntry = T0."DocEntry", @TargetLine = T0."LineNum"
from INV1 T0
         inner join OINV T1 on T1."DocEntry" = T0."DocEntry" and T1."DocStatus" = 'O' and T1."isIns" = 'Y'
where T0."ItemCode" = @ItemCode and T0."InvntSttus" = 'O' and T0."WhsCode" = @WhsCode and T0."OpenInvQty" - IsNull(T0.U_LW_SCAN_QTY, 0) > 0;

--update the unfulfilled reserved invoice
If @TargetType is not null Begin
    update INV1 set U_LW_SCAN_QTY = IsNull(U_LW_SCAN_QTY, 0) + 1 where "DocEntry" = @TargetEntry and "LineNum" = @TargetLine;
    --set return value to Fullfillment
    set @ReturnValue = 2;
end;

If @TargetEntry is null Begin
    --check if I have unfulfilled sales order
    select top 1 @TargetType = T0."ObjType", @TargetEntry = T0."DocEntry", @TargetLine = T0."LineNum"
    from RDR1 T0
             inner join ORDR T1 on T1."DocEntry" = T0."DocEntry" and T1."DocStatus" = 'O'
    where T0."ItemCode" = @ItemCode and T0."InvntSttus" = 'O' and T0."WhsCode" = @WhsCode and T0."OpenInvQty" - IsNull(T0.U_LW_SCAN_QTY, 0) > 0;

    --update the unfulfilled sales order
    If @TargetType is not null Begin
        update RDR1 set U_LW_SCAN_QTY = IsNull(U_LW_SCAN_QTY, 0) + 1 where "DocEntry" = @TargetEntry and "LineNum" = @TargetLine;
        --set return value to Fullfillment
        set @ReturnValue = 2;
    end;
End;

If @TargetEntry is null Begin
    --check if I have unfulfilled inventory transfer request
    select top 1 @TargetType = T0."ObjType", @TargetEntry = T0."DocEntry", @TargetLine = T0."LineNum"
    from WTQ1 T0
             inner join OWTQ T1 on T1."DocEntry" = T0."DocEntry" and T1."DocStatus" = 'O'
    where T0."ItemCode" = @ItemCode and T0."InvntSttus" = 'O' and T0."FromWhsCod" = @WhsCode and T0."WhsCode" = @WhsCode and T0."OpenInvQty" - IsNull(T0.U_LW_SCAN_QTY, 0) > 0;

    --update the unfulfilled inventory transfer request
    If @TargetType is not null Begin
        update WTQ1 set U_LW_SCAN_QTY = IsNull(U_LW_SCAN_QTY, 0) + 1 where "DocEntry" = @TargetEntry and "LineNum" = @TargetLine;
        --set return value to Showroom
        set @ReturnValue = 3;
    end;
End;

declare @LineID int = IsNull((select Max("U_LineID") + 1 from "@LW_YUVAL08_GRPO1"), 0);
insert into "@LW_YUVAL08_GRPO1"(U_ID, "U_LineID", "U_ItemCode", "U_BarCode", "U_empID", "U_Date", "U_POEntry", "U_POLine", "U_TargetType", "U_TargetEntry", "U_TargetLine")
select @ID, @LineID, @ItemCode, @BarCode, @empID, getdate(), IsNull(@POEntry, -1), IsNull(@POLine, -1), @TargetType, @TargetEntry, @TargetLine;

update "@LW_YUVAL08_GRPO" set "U_Status" = 'I' where Code = @ID

select @ReturnValue ReturnValue