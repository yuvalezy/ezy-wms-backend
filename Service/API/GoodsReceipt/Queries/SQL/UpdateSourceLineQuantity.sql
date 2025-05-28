--begin tran

-- declare @ID int = 1060;
-- declare @LineID int = 0
-- declare @Quantity int = 2
-- declare @UserSign int = 1

-- stop test variables

drop table if exists #tmp_ScannedData;

declare @PurPackUn int;
declare @NumInBuy int;
declare @ItemCode nvarchar(50);
declare @empID int;
declare @CardCode nvarchar(50);
declare @Type char(1);
declare @Unit smallint
select @PurPackUn = COALESCE(T2.PurPackUn, 1),
       @NumInBuy = COALESCE(T2.NumInBuy, 1),
       @Quantity = @Quantity * Case When T0."U_Unit" >= 1 Then COALESCE(T2."NumInBuy", 1) Else 1 End * Case When T0."U_Unit" = 2 Then COALESCE(T2."PurPackUn", 1) Else 1 End,
       @CardCode = T1.U_CardCode, @Type = T1.U_Type,
       @ItemCode = T0.U_ItemCode, @empID = T0.U_empID,
       @Unit = COALESCE(T0."U_Unit", 0)
from "@LW_YUVAL08_GRPO1" T0
         inner join "@LW_YUVAL08_GRPO" T1 on T1.Code = @ID
         inner join OITM T2 on T2.ItemCode = T0.U_ItemCode
where T0.U_ID = @ID and T0.U_LineID = @LineID;

update "@LW_YUVAL08_GRPO1" set "U_Quantity" = @Quantity 
                             , "U_StatusUserSign" = @UserSign, "U_StatusTimeStamp" = getdate()
where U_ID = @ID and "U_LineID" = @LineID

delete from "@LW_YUVAL08_GRPO2" where U_ID = @ID and U_LineID = @LineID
delete from "@LW_YUVAL08_GRPO4" where U_ID = @ID and U_LineID = @LineID

SET NOCOUNT ON;

--set source data

drop table if exists #tmp_SourceData;

declare @tmp_ScannedDataSourceDocs table(ID int identity(1, 1), ObjType int, DocEntry int, LineNum int, OpenQuantity numeric(19, 6))

declare @WhsCode nvarchar(8) = (select U_LW_Branch
                                from OHEM
                                where empID = @empID);

select T2.U_SourceType ObjType, T2.U_SourceEntry DocEntry, T2.U_SourceLine LineNum, Sum(T2."U_Quantity") Quantity
into #tmp_SourceData
from [@LW_YUVAL08_GRPO1] T0
         inner join [@LW_YUVAL08_GRPO] T1 on T1.Code = T0.U_ID and T1.U_WhsCode = @WhsCode and T1.U_Status not in ('C', 'F')
         inner join [@LW_YUVAL08_GRPO4] T2 on T2.U_ID = T0.U_ID and T2.U_LineID = T0.U_LineID
where T0.U_ItemCode = @ItemCode
Group By T2.U_SourceType, T2.U_SourceEntry, T2.U_SourceLine

If @Type <> 'R' BEGIN
    insert into @tmp_ScannedDataSourceDocs(ObjType, DocEntry, LineNum, OpenQuantity)
    select T0."ObjType", T0."DocEntry", T0."LineNum", T0."OpenInvQty" - IsNull(T2.Quantity, 0) OpenQuantity
    from POR1 T0
             inner join OPOR T1 on T1."DocEntry" = T0."DocEntry" and T1."DocStatus" = 'O' and
                                   (
                                       @Type = 'A' and (T1.CardCode = @CardCode or @CardCode is null)
                                           or @Type = 'S'
                                       )
             left outer join #tmp_SourceData T2 on T2.ObjType = T0.ObjType and T2.DocEntry = T0.DocEntry and T2.LineNum = T0.LineNum
             left outer join "@LW_YUVAL08_GRPO3" T3 on T3.U_ID = @ID and T3."U_DocEntry" = T0."DocEntry" and T3."U_ObjType" = T0."ObjType"
    where T0."ItemCode" = @ItemCode
      and T0."LineStatus" = 'O'
      and T0."WhsCode" = @WhsCode
      and T0."OpenInvQty" - IsNull(T2.Quantity, 0) > 0
      and (@Type = 'A' or @Type = 'S' and T3.Code is not null)
      and (@Unit != 0 and T0."UseBaseUn" = 'N' or @Unit = 0 and T0."UseBaseUn" = 'Y')
    order by T1."CreateDate", T1.CreateTS;
End Else Begin
    insert into @tmp_ScannedDataSourceDocs(ObjType, DocEntry, LineNum, OpenQuantity)
    select T0."ObjType", T0."DocEntry", T0."LineNum", T0."InvQty" - IsNull(T2.Quantity, 0) OpenQuantity
    from PDN1 T0
             inner join OPDN T1 on T1."DocEntry" = T0."DocEntry" and T1."DocStatus" = 'O' 
             left outer join #tmp_SourceData T2 on T2.ObjType = T0.ObjType and T2.DocEntry = T0.DocEntry and T2.LineNum = T0.LineNum
             left outer join "@LW_YUVAL08_GRPO3" T3 on T3.U_ID = @ID and T3."U_DocEntry" = T0."DocEntry" and T3."U_ObjType" = T0."ObjType"
    where T0."ItemCode" = @ItemCode
      and T0."WhsCode" = @WhsCode
      and T0."InvQty" - IsNull(T2.Quantity, 0) > 0
      and T3.Code is not null
      and (@Unit != 0 and T0."UseBaseUn" = 'N' or @Unit = 0 and T0."UseBaseUn" = 'Y')
    order by T1."CreateDate", T1.CreateTS;
end

insert into @tmp_ScannedDataSourceDocs(ObjType, DocEntry, LineNum, OpenQuantity)
select T0."ObjType", T0."DocEntry", T0."LineNum", T0."OpenInvQty" - IsNull(T2.Quantity, 0) OpenQuantity
from PCH1 T0
         inner join OPCH T1 on T1."DocEntry" = T0."DocEntry" and (
             @Type <> 'R' and T1."DocStatus" = 'O' and T1."isIns" = 'Y'
             or @Type = 'R' and T1."CANCELED" not in ('C', 'Y') and T1."isIns" = 'N'
    ) and
                               (
                                   @Type = 'A' and (T1.CardCode = @CardCode or @CardCode is null)
                                       or @Type in ('S', 'R')
                                   )
         left outer join #tmp_SourceData T2 on T2.ObjType = T0.ObjType and T2.DocEntry = T0.DocEntry and T2.LineNum = T0.LineNum
         left outer join "@LW_YUVAL08_GRPO3" T3 on T3.U_ID = @ID and T3."U_DocEntry" = T0."DocEntry" and T3."U_ObjType" = T0."ObjType"
where T0."ItemCode" = @ItemCode
  and (@Type <> 'R' and T0."LineStatus" = 'O' or @Type = 'R')
  and T0."WhsCode" = @WhsCode
  and Case When @Type <> 'R' Then T0."OpenInvQty" Else T0."InvQty" End - IsNull(T2.Quantity, 0) > 0
  and (@Type = 'A' or @Type in ('S', 'R') and T3.Code is not null)
  and (@Unit != 0 and T0."UseBaseUn" = 'N' or @Unit = 0 and T0."UseBaseUn" = 'Y')
order by T1."CreateDate", T1.CreateTS;

declare @i int = 1
while @i is not null Begin
    declare @iQty numeric(19, 6) = (select OpenQuantity from @tmp_ScannedDataSourceDocs where ID = @i)
    If @iQty <= @Quantity Begin
        set @Quantity = @Quantity - @iQty
        If @Quantity = 0 Begin
            delete @tmp_ScannedDataSourceDocs where ID > @i
            Break
        End
    End Else Begin
        update @tmp_ScannedDataSourceDocs set OpenQuantity = @Quantity where ID = @i
        delete @tmp_ScannedDataSourceDocs where ID > @i
        set @Quantity = 0
        break
    End
    set @i = (select ID from @tmp_ScannedDataSourceDocs where ID = @i + 1)
End

If @Quantity > 0 Begin
    If exists(select 1 from @tmp_ScannedDataSourceDocs) Begin
        update @tmp_ScannedDataSourceDocs set OpenQuantity = OpenQuantity + @Quantity where ID = (select Max(ID) from @tmp_ScannedDataSourceDocs)
    End Else Begin
        insert into @tmp_ScannedDataSourceDocs(ObjType, DocEntry, LineNum, OpenQuantity)
        select top 1 T1."U_SourceType", T1."U_SourceEntry", T1."U_SourceLine", @Quantity
        from "@LW_YUVAL08_GRPO1" T0
                 inner join "@LW_YUVAL08_GRPO4" T1 on T1.U_ID = T0.U_ID and T1.U_LineID = T0.U_LineID
        where T0.U_ID = @ID and T1."U_SourceType" in (20, 22) and T0."U_ItemCode" = @ItemCode
        order by Case T1.U_SourceType When 20 Then 'A' When 22 Then 'B' Else 'C' End, T1.U_LineID desc;
    End
    set @Quantity = 0
End

insert into "@LW_YUVAL08_GRPO4"(U_ID, "U_LineID", "U_SourceType", "U_SourceEntry", "U_SourceLine", "U_Quantity")
select @ID, @LineID, ObjType, DocEntry, LineNum, OpenQuantity from @tmp_ScannedDataSourceDocs

--set target data

select @Quantity = T0.U_Quantity
from "@LW_YUVAL08_GRPO1" T0
where T0.U_ID = @ID and T0.U_LineID = @LineID;


--todo Consider PurPackUn into next code
declare @TotalQuantity int
select @TotalQuantity = Sum("U_Quantity") + (
    select T0.OnHand
    from OITW T0
    where T0.ItemCode = @ItemCode and WhsCode = @WhsCode)
from [@LW_YUVAL08_GRPO1] T0
         inner join [@LW_YUVAL08_GRPO] T1 on T1.Code = T0.U_ID and T1.U_WhsCode = @WhsCode and T1.U_Status not in ('C', 'F')
where T0.U_ItemCode = @ItemCode;

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
     , Case When IsNull(Sum(T1.U_TargetQty), 0) < @PurPackUn Then 1 Else 0 End Warehouse
     , @PurPackUn PurPackUn
from [@LW_YUVAL08_GRPO1] T0
         left outer join [@LW_YUVAL08_GRPO2] T1 on T1.U_ID = T0.U_ID and T1.U_LineID = T0.U_LineID
where T0.U_ID = @ID and T0.U_LineID = @LineID
Group By T0."U_LineID"

--rollback