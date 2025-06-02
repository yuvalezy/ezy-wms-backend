select case when exists(
	select 1 
	from TABLE_COLUMNS where SCHEMA_NAME = '{0}' AND TABLE_NAME = '@LW_YUVAL08_COMMON' and COLUMN_NAME = 'U_ServiceVersion'
) Then 1 Else 0 End "Check"
from dummy