-- begin tran
-- 
-- declare @ID int = 1073;
-- declare @ItemCode nvarchar(50) = N'BOX';
-- declare @BarCode nvarchar(254) = N'box';
-- declare @empID int = 1;
-- declare @Unit smallint = 0;
SET NOCOUNT ON;

--set default return value to Store in Warehouse
drop table if exists #tmp_ScannedData;
drop table if exists #tmp_SourceData;

declare @tmp_ScannedDataSourceDocs table
                                   (
                                       ID           int identity (1, 1),
                                       ObjType      int,
                                       DocEntry     int,
                                       LineNum      int,
                                       OpenQuantity numeric(19, 6)
                                   )

declare @WhsCode nvarchar(8) = (select U_LW_Branch
                                from OHEM
                                where empID = @empID);
declare @CardCode nvarchar(50);
declare @Type char(1);
select @CardCode = U_CardCode, @Type = U_Type
from "@LW_YUVAL08_GRPO"
where Code = @ID;

declare @NumInBuy int;
declare @BuyUnitMsr nvarchar(50);
declare @PurPackUn int;
declare @PurPackMsr nvarchar(50);
select @NumInBuy = COALESCE("NumInBuy", 1),
       @BuyUnitMsr = "BuyUnitMsr",
       @PurPackUn = COALESCE("PurPackUn", 1),
       @PurPackMsr = "PurPackMsr"
from OITM
where "ItemCode" = @ItemCode;

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
END Else Begin
    insert into @tmp_ScannedDataSourceDocs(ObjType, DocEntry, LineNum, OpenQuantity)
    select T0."ObjType", T0."DocEntry", T0."LineNum", T0."InvQty" - IsNull(T2.Quantity, 0) OpenQuantity
    from PDN1 T0
             inner join OPDN T1 on T1."DocEntry" = T0."DocEntry" and T1."CANCELED" not in ('C', 'Y')
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
select T0."ObjType", T0."DocEntry", T0."LineNum", Case When @Type <> 'R' Then T0."OpenInvQty" Else T0."InvQty" End - IsNull(T2.Quantity, 0) OpenQuantity
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

declare @i        int = 1
declare @Quantity numeric(19, 6) = Case @Unit When 0 Then 1 When 1 Then @NumInBuy When 2 Then @NumInBuy * @PurPackUn End
while @i is not null Begin
    declare @iQty numeric(19, 6) = (select OpenQuantity from @tmp_ScannedDataSourceDocs where ID = @i)
    If @iQty <= @Quantity
        Begin
            set @Quantity = @Quantity - @iQty
            If @Quantity = 0
                Begin
                    delete @tmp_ScannedDataSourceDocs where ID > @i
                    Break
                End
        End
    Else
        Begin
            update @tmp_ScannedDataSourceDocs set OpenQuantity = @Quantity where ID = @i
            delete @tmp_ScannedDataSourceDocs where ID > @i
            set @Quantity = 0
            break
        End
    set @i = (select ID from @tmp_ScannedDataSourceDocs where ID = @i + 1)
End

If @Quantity > 0
    Begin
        If exists(select 1 from @tmp_ScannedDataSourceDocs)
            Begin
                update @tmp_ScannedDataSourceDocs set OpenQuantity = OpenQuantity + @Quantity where ID = (select Max(ID) from @tmp_ScannedDataSourceDocs)
            End
        Else
            Begin
                insert into @tmp_ScannedDataSourceDocs(ObjType, DocEntry, LineNum, OpenQuantity)
                select top 1 T1."U_SourceType", T1."U_SourceEntry", T1."U_SourceLine", @Quantity
                from "@LW_YUVAL08_GRPO1" T0
                         inner join "@LW_YUVAL08_GRPO4" T1 on T1.U_ID = T0.U_ID and T1.U_LineID = T0.U_LineID
                where T0.U_ID = @ID
                  and T1."U_SourceType" in (20, 22)
                  and T0."U_ItemCode" = @ItemCode
                order by Case T1.U_SourceType When 20 Then 'A' When 22 Then 'B' Else 'C' End, T1.U_LineID desc;
            End
        set @Quantity = 0
    End

If not exists(select 1
              from @tmp_ScannedDataSourceDocs)
    Begin
        declare @Error nvarchar(100) = 'No valid source found for item ' + @ItemCode
        RAISERROR (@Error,16,1);
    End


set @Quantity = Case @Unit When 0 Then 1 When 1 Then @NumInBuy When 2 Then @NumInBuy * @PurPackUn End

----insert grpo line
declare @LineID int = IsNull((select Max("U_LineID") + 1
                              from "@LW_YUVAL08_GRPO1"
                              where "U_ID" = @ID), 0);
insert into "@LW_YUVAL08_GRPO1"(U_ID, "U_LineID", "U_ItemCode", "U_BarCode", "U_empID", "U_Date", "U_Quantity", "U_Unit")
select @ID,
       @LineID,
       @ItemCode,
       @BarCode,
       @empID,
       getdate(),
       @Quantity,
       @Unit;

insert into "@LW_YUVAL08_GRPO4"(U_ID, "U_LineID", "U_SourceType", "U_SourceEntry", "U_SourceLine", "U_Quantity")
select @ID, @LineID, ObjType, DocEntry, LineNum, OpenQuantity
from @tmp_ScannedDataSourceDocs

--update status of grpo header to InProgress
update "@LW_YUVAL08_GRPO"
set "U_Status" = 'I'
where Code = @ID;

--todo Consider PurPackUn into next code
declare @TotalQuantity int
select @TotalQuantity = Sum("U_Quantity") + (select T0.OnHand
                                             from OITW T0
                                             where T0.ItemCode = @ItemCode
                                               and WhsCode = @WhsCode)
from [@LW_YUVAL08_GRPO1] T0
         inner join [@LW_YUVAL08_GRPO] T1 on T1.Code = T0.U_ID and T1.U_WhsCode = @WhsCode and T1.U_Status not in ('C', 'F')
where T0.U_ItemCode = @ItemCode;

--Load target data
WITH DocData AS (select ROW_NUMBER() OVER (Order by [Priority], [DocDate]) ID, *
                 from (
                          --reserved invoice
                          select 1 [Priority], T0."ObjType", T1.DocDate, T0."DocEntry", T0."LineNum", T0."OpenInvQty"
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
                            and T0."WhsCode" = @WhsCode) T0)
select T0.ID, T0.ObjType, T0.DocEntry, T0.LineNum, T0.OpenInvQty - Sum(COALESCE(T1."U_TargetQty", 0)) Quantity
into #tmp_ScannedData
from DocData T0
         left outer join "@LW_YUVAL08_GRPO2" T1
                         on T1.U_TargetEntry = T0.DocEntry and T1.U_TargetLine = T0.LineNum and T1.U_TargetType = T0.ObjType and T1.U_TargetStatus in ('O', 'P', 'F') and T1.U_ItemCode = @ItemCode
group by T0.ID, T0.ObjType, T0.DocEntry, T0.LineNum, T0.OpenInvQty
Having T0.OpenInvQty - Sum(COALESCE(T1."U_TargetQty", 0)) > 0;

--Spread quantity into loaded target data
declare @ScanID    int = (select Min(ID)
                          from #tmp_ScannedData)
declare @ScanQty   int
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

    If @Quantity = 0
        Begin
            Break
        End

    set @ScanID = (select Min(ID) from #tmp_ScannedData where ID > @ScanID)
End

select T0."U_LineID"                                                           LineID
     , Sum(Case When U_TargetType in (13, 17) Then 1 Else 0 End)               Fulfillment
     , Sum(Case When U_TargetType = 1250000001 Then 1 Else 0 End)              Showroom
     , Case When IsNull(Sum(T1.U_TargetQty), 0) < @PurPackUn Then 1 Else 0 End Warehouse
     , @NumInBuy                                                               "NumInBuy"
     , @BuyUnitMsr                                                             "BuyUnitMsr"
     , @PurPackUn                                                              "PurPackUn"
     , @PurPackMsr                                                             "PurPackMsr"
from [@LW_YUVAL08_GRPO1] T0
         left outer join [@LW_YUVAL08_GRPO2] T1 on T1.U_ID = T0.U_ID and T1.U_LineID = T0.U_LineID
where T0.U_ID = @ID
  and T0.U_LineID = @LineID
Group By T0."U_LineID"

-- select * from "@LW_YUVAL08_GRPO1" where U_ID = @ID;
-- select * from "@LW_YUVAL08_GRPO2" where U_ID = @ID;
-- select * from "@LW_YUVAL08_GRPO3" where U_ID = @ID;
-- select * from "@LW_YUVAL08_GRPO4" where U_ID = @ID;
-- 
-- rollback