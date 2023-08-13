do
begin
	declare TypeID nvarchar(50) = '{0}';
	declare UserID integer = {1};
	declare Printer nvarchar(100) = '{2}';
	declare Chk char(1);
	select case when exists(select '' from "@{3}" where "U_TypeID" = :TypeID and "U_UserID" = :UserID) Then 'Y' Else 'N' End into Chk FROM DUMMY;
	if :Chk = 'N' THEN 
		insert into "@{3}"("Code", "Name", "U_TypeID", "U_UserID", "U_Printer")
		select IFNULL((select Max(Cast("Code" as bigint))+1 from "@{3}"), 0), IFNULL((select Max(Cast("Code" as bigint))+1 from "@{3}"), 0),
		:TypeID, :UserID, :Printer
		FROM DUMMY;
	ELSE
		update "@{3}" set "U_Printer" = :Printer where "U_TypeID" = :TypeID and "U_UserID" = :UserID;
	End If;
end;