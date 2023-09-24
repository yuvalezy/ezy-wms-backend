select T0.dbName [Name], T0.cmpName [Desc]
from [SBO-COMMON]..SRGC T0
where T0.dbUser = 'dbo'
order by T0.[dbName]
