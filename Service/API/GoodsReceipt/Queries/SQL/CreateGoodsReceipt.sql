set nocount on;

-- declare @Name nvarchar(50) = N'{0}';
-- declare @EmpID int = {1};
-- declare @CardCode nvarchar(50) = N'{2}';
declare @WhsCode nvarchar(8) = (select U_LW_Branch from OHEM where empID = @empID);
DECLARE @InsertedRows TABLE (Id int);

insert into [@LW_YUVAL08_GRPO]("Name", "U_Date", "U_empID", "U_StatusDate", "U_StatusEmpID", "U_CardCode", "U_WhsCode", "U_Type")
OUTPUT inserted."Code" INTO @InsertedRows
values(@Name, getdate(), @EmpID, getdate(), @EmpID, @CardCode, @WhsCode, @Type)

select * from @InsertedRows