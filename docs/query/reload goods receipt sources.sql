declare @ItemCode nvarchar(50) = 'Gj1306-32';
declare @Id uniqueidentifier = '138f15a9-5bf6-47a9-d869-08ddc952eebf';
drop table if exists #lines;

select Id into #lines from GoodsReceiptLines where GoodsReceiptId = @Id;


delete GoodsReceiptSources where GoodsReceiptLineId in (select Id from #Lines)

insert into GoodsReceiptSources
select NEWID(), T1.Quantity, T0.DocEntry, T0.DocEntry, T0.LineNum, T0.ObjType, T1.Id, T1.CreatedAt, T1.CreatedByUserId, T1.UpdatedAt, T1.UpdatedByUserId, 0, null
from GoodsReceiptLines T1
inner join [MODNOV_SBO]..PDN1 T0 on T0.DocEntry = 517 and T0.ItemCode = T1.ItemCode collate database_default and T0.UseBaseUn = Case T1.Unit When 0 Then 'Y' Else 'N' End
where T1.Id in (select id from #lines)