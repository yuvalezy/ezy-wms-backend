set nocount on;

-- declare @Name nvarchar(50) = N'Test';
-- declare @EmpID int = 1;
declare @WhsCode nvarchar(8) = (select U_LW_Branch from OHEM where empID = @empID);
DECLARE @InsertedRows TABLE (Id int);

insert into [@LW_YUVAL08_TRANS]("Name", "U_Date", "U_empID", "U_StatusDate", "U_StatusEmpID", "U_WhsCode", "U_Comments")
OUTPUT inserted."Code" INTO @InsertedRows
values(@Name, getdate(), @empID, getdate(), @empID, @WhsCode, @Comments)

select * from @InsertedRows