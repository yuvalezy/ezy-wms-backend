-- declare @ID int = 1;
-- declare @BarCode nvarchar(254) = '5678901234567';
-- declare @ItemCode nvarchar(50) = 'SCS';
-- declare @empID int = 1;
SET NOCOUNT ON;

declare @BaseCount table
                   (
                       BaseEntry int,
                       BaseLine  int,
                       Quantity  int
                   )

insert into @BaseCount
select T0.U_POEntry, T0.U_POLine, Count(1)
from [@LW_YUVAL08_GRPO1] T0
         inner join [@LW_YUVAL08_GRPO] T1 on T1.Code = T0.U_ID and T1.U_Status <> 'C'
where T0.U_ItemCode = @ItemCode and T0."U_TargetStatus" = 'O'
Group By T0.U_POEntry, T0.U_POLine

declare @TargetCount table
                     (
                         TargetType  int,
                         TargetEntry int,
                         TargetLine  int,
                         Quantity    int
                     )

insert into @TargetCount
select T0.U_TargetType, T0.U_TargetEntry, T0.U_TargetLine, Count(1)
from [@LW_YUVAL08_GRPO1] T0
         inner join [@LW_YUVAL08_GRPO] T1 on T1.Code = T0.U_ID and T1.U_Status <> 'C'
where T0.U_ItemCode = @ItemCode and T0."U_TargetStatus" = 'O'
Group By T0.U_TargetType, T0.U_TargetEntry, T0.U_TargetLine

--set default return value to Store in Warehouse
declare @ReturnValue int = 1;
declare @WhsCode nvarchar(8) = (select U_LW_Branch
                                from OHEM
                                where empID = @empID);

declare @POEntry int;
declare @POLine int;

declare @TargetType int;
declare @TargetEntry int;
declare @TargetLine int;

--get first open purchase order in the connected branch
select top 1 @POEntry = T0."DocEntry", @POLine = T0."LineNum"
from POR1 T0
         inner join OPOR T1 on T1."DocEntry" = T0."DocEntry" and T1."DocStatus" = 'O'
         left outer join @BaseCount T2 on T2.BaseEntry = T0.DocEntry and T2.BaseLine = T0.LineNum
where T0."ItemCode" = @ItemCode
  and T0."LineStatus" = 'O'
  and T0."WhsCode" = @WhsCode
  and T0."OpenQty" - IsNull(T2.Quantity, 0) > 0
order by T1."CreateDate";

--check if I have unfulfilled reserved invoice
select top 1 @TargetType = T0."ObjType", @TargetEntry = T0."DocEntry", @TargetLine = T0."LineNum"
from INV1 T0
         inner join OINV T1 on T1."DocEntry" = T0."DocEntry" and T1."DocStatus" = 'O' and T1."isIns" = 'Y'
         left outer join @TargetCount T2 on T2.TargetEntry = T0.DocEntry and T2.TargetType = T0.ObjType and T2.TargetLine = T0.LineNum
where T0."ItemCode" = @ItemCode
  and T0."InvntSttus" = 'O'
  and T0."WhsCode" = @WhsCode
  and T0."OpenInvQty" - IsNull(T2.Quantity, 0) > 0;

--set return value to Fullfillment
If @TargetType is not null
    set @ReturnValue = 2;

If @TargetEntry is null
    Begin
        --check if I have unfulfilled sales order
        select top 1 @TargetType = T0."ObjType", @TargetEntry = T0."DocEntry", @TargetLine = T0."LineNum"
        from RDR1 T0
                 inner join ORDR T1 on T1."DocEntry" = T0."DocEntry" and T1."DocStatus" = 'O'
                 left outer join @TargetCount T2 on T2.TargetEntry = T0.DocEntry and T2.TargetType = T0.ObjType and T2.TargetLine = T0.LineNum
        where T0."ItemCode" = @ItemCode
          and T0."InvntSttus" = 'O'
          and T0."WhsCode" = @WhsCode
          and T0."OpenInvQty" - IsNull(T2.Quantity, 0) > 0;

        --set return value to Fullfillment
        If @TargetType is not null
            set @ReturnValue = 2;
    End;

If @TargetEntry is null
    Begin
        --check if I have unfulfilled inventory transfer request
        select top 1 @TargetType = T0."ObjType", @TargetEntry = T0."DocEntry", @TargetLine = T0."LineNum"
        from WTQ1 T0
                 inner join OWTQ T1 on T1."DocEntry" = T0."DocEntry" and T1."DocStatus" = 'O'
                 left outer join @TargetCount T2 on T2.TargetEntry = T0.DocEntry and T2.TargetType = T0.ObjType and T2.TargetLine = T0.LineNum
        where T0."ItemCode" = @ItemCode
          and T0."InvntSttus" = 'O'
          and T0."FromWhsCod" = @WhsCode
          and T0."WhsCode" = @WhsCode
          and T0."OpenInvQty" - IsNull(T2.Quantity, 0) > 0;

        --set return value to Showroom
        If @TargetType is not null
            set @ReturnValue = 3;
    End;

declare @LineID int = IsNull((select Max("U_LineID") + 1
                              from "@LW_YUVAL08_GRPO1"), 0);
insert into "@LW_YUVAL08_GRPO1"(U_ID, "U_LineID", "U_ItemCode", "U_BarCode", "U_empID", "U_Date", "U_POEntry", "U_POLine", "U_TargetType", "U_TargetEntry", "U_TargetLine")
select @ID,
       @LineID,
       @ItemCode,
       @BarCode,
       @empID,
       getdate(),
       IsNull(@POEntry, -1),
       IsNull(@POLine, -1),
       @TargetType,
       @TargetEntry,
       @TargetLine;

update "@LW_YUVAL08_GRPO"
set "U_Status" = 'I'
where Code = @ID

select @ReturnValue ReturnValue

