--begin tran

-- declare @ID int = 1;
-- declare @BinEntry int = 3500;
-- declare @BarCode nvarchar(254) = '34567890455555';
-- declare @ItemCode nvarchar(50) = 'SCUOM';
-- declare @empID int = 1;
-- declare @Quantity int = 12;
SET NOCOUNT ON;

--set default return value to Store in Warehouse
drop table if exists #tmp_ScannedData;

--insert grpo line
declare @LineID int = IsNull((select Max("U_LineID") + 1
                              from "@LW_YUVAL08_OINC1" where "U_ID" = @ID), 0);
insert into "@LW_YUVAL08_OINC1"(U_ID, "U_LineID", "U_ItemCode", "U_BarCode", "U_empID", "U_Date", "U_BinEntry", "U_Quantity")
values(@ID, @LineID, @ItemCode, @BarCode, @empID, getdate(), @BinEntry, @Quantity);

--update status of counting header to InProgress
update "@LW_YUVAL08_OINC" set "U_Status" = 'I' where Code = @ID;

--rollback