SELECT T0."dbName" AS "Name", T0."cmpName" AS "Desc", IFNULL(T1."Active", 'N') AS "Active" 
FROM SBOCOMMON.SRGC T0 
LEFT OUTER JOIN "ServiceManager" T1 ON T1."dbName" = T0."dbName"
where "dbUser" = CURRENT_USER
order by T0."dbName"