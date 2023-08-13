select case when exists(select 1 from "ServiceManager" where "dbName" = @dbName) Then 1 Else 0 End 
