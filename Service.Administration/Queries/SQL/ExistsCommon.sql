select case when exists(
	select 1 
	from [{0}]..syscolumns 
	where id = (select id from [{0}]..sysobjects where name = '@LW_YUVAL08_COMMON') and name = 'U_ServiceVersion'
) Then 1 Else 0 End [Check]