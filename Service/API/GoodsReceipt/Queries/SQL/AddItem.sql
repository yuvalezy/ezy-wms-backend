--begin tran

-- declare @ID int = 2;
-- declare @BarCode nvarchar(254) = '34567890455555';
-- declare @ItemCode nvarchar(50) = 'SCUOM';
-- declare @empID int = 1;
SET NOCOUNT ON;

--set default return value to Store in Warehouse
drop table if exists #tmp_ScannedData;

declare @ReturnValue int = 1;
declare @WhsCode nvarchar(8) = (select U_LW_Branch
                                from OHEM
                                where empID = @empID);
declare @CardCode nvarchar(50) = (select U_CardCode from "@LW_YUVAL08_GRPO" where Code = @ID);
declare @NumInBuy int = (select COALESCE("NumInBuy", 1) from OITM where "ItemCode" = @ItemCode);

declare @POEntry int;
declare @POLine int;

declare @TargetType int;
declare @TargetEntry int;
declare @TargetLine int;


--get first open purchase order in the connected branch
select top 1 @POEntry = T0."DocEntry", @POLine = T0."LineNum", @NumInBuy = Case T0.UseBaseUn When 'N' Then OpenCreQty Else 1 End
from POR1 T0
         inner join OPOR T1 on T1."DocEntry" = T0."DocEntry" and T1."DocStatus" = 'O' and T1.CardCode = @CardCode
         left outer join (
			select T0.U_POEntry DocEntry, T0.U_POLine LineNum, Sum("U_Quantity" * "U_QtyPerUnit") Quantity
			from [@LW_YUVAL08_GRPO1] T0
					 inner join [@LW_YUVAL08_GRPO] T1 on T1.Code = T0.U_ID and T1.U_WhsCode = @WhsCode and T1.U_Status not in ('C', 'F')
			where T0.U_ItemCode = @ItemCode
			Group By T0.U_POEntry, T0.U_POLine
		)T2 on T2.DocEntry = T0.DocEntry and T2.LineNum = T0.LineNum
where T0."ItemCode" = @ItemCode
  and T0."LineStatus" = 'O'
  and T0."WhsCode" = @WhsCode
  and T0."OpenInvQty" - IsNull(T2.Quantity, 0) > 0
order by T1."CreateDate";

declare @Quantity int = @NumInBuy

--insert grpo line
declare @LineID int = IsNull((select Max("U_LineID") + 1
                              from "@LW_YUVAL08_GRPO1" where "U_ID" = @ID), 0);
insert into "@LW_YUVAL08_GRPO1"(U_ID, "U_LineID", "U_ItemCode", "U_BarCode", "U_empID", "U_Date", "U_POEntry", "U_POLine", "U_Quantity", "U_QtyPerUnit")
select @ID,
       @LineID,
       @ItemCode,
       @BarCode,
       @empID,
       getdate(),
       IsNull(@POEntry, -1),
       IsNull(@POLine, -1),
       1,
       @Quantity;

--update status of grpo header to InProgress
update "@LW_YUVAL08_GRPO" set "U_Status" = 'I' where Code = @ID;

--todo Consider NumInBuy into next code
declare @TotalQuantity int
select @TotalQuantity = Sum("U_Quantity" * "U_QtyPerUnit") + (
    select T0.OnHand
    from OITW T0
    where T0.ItemCode = @ItemCode and WhsCode = @WhsCode)
from [@LW_YUVAL08_GRPO1] T0
         inner join [@LW_YUVAL08_GRPO] T1 on T1.Code = T0.U_ID and T1.U_WhsCode = @WhsCode and T1.U_Status not in ('C', 'F')
where T0.U_ItemCode = @ItemCode;

--Load target data
WITH DocData AS (
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
select T0.ID, T0.ObjType, T0.DocEntry, T0.LineNum, T0.OpenInvQty-Sum(COALESCE(T1."U_TargetQty", 0)) Quantity
into #tmp_ScannedData
from DocData T0
left outer join "@LW_YUVAL08_GRPO2" T1 on T1.U_TargetEntry = T0.DocEntry and T1.U_TargetLine = T0.LineNum and T1.U_TargetType = T0.ObjType and T1.U_TargetStatus in ('O', 'P', 'F') and T1.U_ItemCode = @ItemCode
group by T0.ID, T0.ObjType, T0.DocEntry, T0.LineNum, T0.OpenInvQty
Having T0.OpenInvQty-Sum(COALESCE(T1."U_TargetQty", 0)) > 0;

--Spread quantity into loaded target data
declare @ScanID int = (select Min(ID) from #tmp_ScannedData)
declare @ScanQty int
declare @InsertQty int
while @ScanID is not null Begin
	set @ScanQty = (select Quantity from #tmp_ScannedData where ID = @ScanID)

	--Check insert quantity
	set @InsertQty = Case When @Quantity >= @ScanQty Then @ScanQty Else @Quantity End
	--remove insert quantity from quantity
	set @Quantity = @Quantity - @InsertQty

	insert into "@LW_YUVAL08_GRPO2"(U_ID, "U_LineID", "U_ItemCode", "U_TargetType", "U_TargetEntry", "U_TargetLine", "U_TargetQty")
	select @ID, @LineID, @ItemCode, ObjType, "DocEntry", "LineNum", @InsertQty
	from #tmp_ScannedData
	where ID = @ScanID

	If @Quantity = 0 Begin
		Break
	End

	set @ScanID = (select Min(ID) from #tmp_ScannedData where ID > @ScanID)
End

select T0."U_LineID" LineID
, Sum(Case When U_TargetType in (13, 17) Then 1 Else 0 End) Fulfillment
, Sum(Case When U_TargetType = 1250000001 Then 1 Else 0 End) Showroom
, Case When IsNull(Sum(T1.U_TargetQty), 0) < @NumInBuy Then 1 Else 0 End Warehouse
, @NumInBuy NumInBuy 
from [@LW_YUVAL08_GRPO1] T0
left outer join [@LW_YUVAL08_GRPO2] T1 on T1.U_ID = T0.U_ID and T1.U_LineID = T0.U_LineID
where T0.U_ID = @ID and T0.U_LineID = @LineID
Group By T0."U_LineID"

--rollback