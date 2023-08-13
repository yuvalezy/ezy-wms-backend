--select case when exists(
	select 1 "Check"
	from TABLE_COLUMNS where SCHEMA_NAME = 'LW-YUVAL08-COMMON' AND TABLE_NAME = 'ServiceManager' and COLUMN_NAME = 'dbName'
/*) Then 1 Else 0 End
from dummy*/