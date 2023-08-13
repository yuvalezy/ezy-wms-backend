select case when exists(
	select 1 
	from [LW_YUVAL08_COMMON]..syscolumns 
	where id = (select id from [LW_YUVAL08_COMMON]..sysobjects where name = 'ServiceManager') and name = 'dbName'
) Then 1 Else 0 End [Check]