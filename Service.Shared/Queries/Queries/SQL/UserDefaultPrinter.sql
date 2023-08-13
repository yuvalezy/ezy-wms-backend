declare @TypeID nvarchar(50) = '{0}'
declare @UserID integer = {1}
declare @Printer nvarchar(100) = '{2}'
if not exists(select '' from [@{3}] where U_TypeID = @TypeID and U_UserID = @UserID) begin 
	insert [@{3}](Code, Name, U_TypeID, U_UserID, U_Printer)
	select IsNull((select Max(Cast(Code as bigint))+1 from [@{3}]), 0), IsNull((select Max(Cast(Code as bigint))+1 from [@{3}]), 0),
	@TypeID, @UserID, @Printer
end else begin
	update [@{3}] set U_Printer = @Printer where U_TypeID = @TypeID and U_UserID = @UserID
End