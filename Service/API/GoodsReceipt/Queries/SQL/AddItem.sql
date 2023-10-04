-- declare @ID int = 1;
-- declare @BarCode nvarchar(254) = '5678901234567';
-- declare @ItemCode nvarchar(50) = 'SCS';
-- declare @empID int = 1;
SET NOCOUNT ON;

--set default return value to Store in Warehouse
declare @ReturnValue int = 1;
declare @WhsCode nvarchar(8) = (select U_LW_Branch
                                from OHEM
                                where empID = @empID);
declare @CardCode nvarchar(50) = (select U_CardCode from "@LW_YUVAL08_GRPO" where Code = @ID);

declare @POEntry int;
declare @POLine int;

declare @TargetType int;
declare @TargetEntry int;
declare @TargetLine int;

--get first open purchase order in the connected branch
select top 1 @POEntry = T0."DocEntry", @POLine = T0."LineNum"
from POR1 T0
         inner join OPOR T1 on T1."DocEntry" = T0."DocEntry" and T1."DocStatus" = 'O' and T1.CardCode = @CardCode
         left outer join (
    select T0.U_POEntry DocEntry, T0.U_POLine LineNum, Count(1) Quantity
    from [@LW_YUVAL08_GRPO1] T0
             inner join [@LW_YUVAL08_GRPO] T1 on T1.Code = T0.U_ID and T1.U_WhsCode = @WhsCode and T1.U_Status not in ('C', 'F')
    where T0.U_ItemCode = @ItemCode
    Group By T0.U_POEntry, T0.U_POLine
)T2 on T2.DocEntry = T0.DocEntry and T2.LineNum = T0.LineNum
where T0."ItemCode" = @ItemCode
  and T0."LineStatus" = 'O'
  and T0."WhsCode" = @WhsCode
  and T0."OpenQty" - IsNull(T2.Quantity, 0) > 0
order by T1."CreateDate";


declare @LineID int = IsNull((select Max("U_LineID") + 1
                              from "@LW_YUVAL08_GRPO1"), 0);
insert into "@LW_YUVAL08_GRPO1"(U_ID, "U_LineID", "U_ItemCode", "U_BarCode", "U_empID", "U_Date", "U_POEntry", "U_POLine")
select @ID,
       @LineID,
       @ItemCode,
       @BarCode,
       @empID,
       getdate(),
       IsNull(@POEntry, -1),
       IsNull(@POLine, -1);

update "@LW_YUVAL08_GRPO"
set "U_Status" = 'I'
where Code = @ID


declare @TotalQuantity int
select @TotalQuantity = Count(1) + (
    select T0.OnHand
    from OITW T0
    where T0.ItemCode = @ItemCode and WhsCode = @WhsCode)
from [@LW_YUVAL08_GRPO1] T0
         inner join [@LW_YUVAL08_GRPO] T1 on T1.Code = T0.U_ID and T1.U_WhsCode = @WhsCode and T1.U_Status not in ('C', 'F')
where T0.U_ItemCode = @ItemCode;

WITH X0 AS (
    select ROW_NUMBER() OVER(Order by [Priority], [DocDate]) ID, *
    from (
             --reserved invoice
             select 1 [Priority], T0."ObjType", T1.DocDate, T0."DocEntry",T0."LineNum", T0."OpenInvQty"
             from INV1 T0
                      inner join OINV T1 on T1."DocEntry" = T0."DocEntry" and T1."DocStatus" = 'O' and T1."isIns" = 'Y'
             where T0."ItemCode" = @ItemCode
               and T0."InvntSttus" = 'O'
               and T0."WhsCode" = @WhsCode
             --open sales orders
             union
             select 2, T0."ObjType", T1.DocDate, T0."DocEntry", T0."LineNum", T0."OpenInvQty"
             from RDR1 T0
                      inner join ORDR T1 on T1."DocEntry" = T0."DocEntry" and T1."DocStatus" = 'O'
             where T0."ItemCode" = @ItemCode
               and T0."InvntSttus" = 'O'
               and T0."WhsCode" = @WhsCode
             union
             --open transfer request
             select 3, T0."ObjType", T1.DocDate, T0."DocEntry", T0."LineNum", T0."OpenInvQty"
             from WTQ1 T0
                      inner join OWTQ T1 on T1."DocEntry" = T0."DocEntry" and T1."DocStatus" = 'O'
             where T0."ItemCode" = @ItemCode
               and T0."InvntSttus" = 'O'
               and T0."FromWhsCod" = @WhsCode
               and T0."WhsCode" = @WhsCode
         ) T0
)
select top 1 @ReturnValue = Case X0.ObjType When 1250000001 Then 3 When 13 Then 2 When 17 Then 2 End
from X0
         left outer join X0 X1 on X1.ID <= X0.ID
Group By X0.ID, X0.ObjType, X0.DocDate, X0.DocEntry, X0.LineNum
Having Sum(X1.OpenInvQty) >= @TotalQuantity;

select @ReturnValue ReturnValue
