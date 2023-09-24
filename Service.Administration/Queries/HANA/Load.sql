SELECT T0."dbName" AS "Name", T0."cmpName" AS "Desc"
FROM SBOCOMMON.SRGC T0 
where "dbUser" = CURRENT_USER
order by T0."dbName"