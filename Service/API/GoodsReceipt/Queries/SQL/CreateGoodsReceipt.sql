set nocount on;

declare @Name nvarchar(50) = N'{0}';
declare @EmpID int = {1};
DECLARE @InsertedRows TABLE (Id int);

insert into [@LW_YUVAL08_GRPO]("Name", "U_Date", "U_empID", "U_StatusDate", "U_StatusEmpID")
OUTPUT inserted."Code" INTO @InsertedRows
values(@Name, getdate(), @EmpID, getdate(), @EmpID)

select * from @InsertedRows