select case when exists(
	select 1 
	from [LW-YUVAL08-COMMON]..syscolumns 
	where id = (select id from [LW-YUVAL08-COMMON]..sysobjects where name = 'ServiceManager') and name = 'dbName'
) Then 1 Else 0 End [Check]