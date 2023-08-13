--select case when exists(
	select 1 "Check"
	from TABLE_COLUMNS where SCHEMA_NAME = 'LW_YUVAL08_COMMON' AND TABLE_NAME = 'ServiceManager' and COLUMN_NAME = 'dbName'
/*) Then 1 Else 0 End
from dummy*/