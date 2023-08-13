insert into "@{0}V"("Code", "Name", "U_ID", "U_VarID", "U_Value") 
select IFNULL((select Max(Cast("Code" as bigint))+1 from "@{0}V"), 0), 
  IFNULL((select Max(Cast("Code" as bigint))+1 from "@{0}V"), 0), {1}, {2}, '{3}' 
FROM DUMMY