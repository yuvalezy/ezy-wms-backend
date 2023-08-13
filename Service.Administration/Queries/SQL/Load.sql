select T0.dbName [Name], T0.cmpName [Desc], IsNull(T1.Active, 'N') Active
from [SBO-COMMON]..SRGC T0
left outer join ServiceManager T1 on T1.dbName = T0.dbName collate database_default
where T0.dbUser = 'dbo'
order by T0.[dbName]
