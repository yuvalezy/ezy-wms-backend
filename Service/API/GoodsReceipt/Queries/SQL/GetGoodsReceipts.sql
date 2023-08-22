select DOCS."Code"                                                                           ID,
       DOCS."Name",
       DOCS."U_Date"                                                                         "Date",
       DOCS."U_empID"                                                                        "EmployeeID",
       COALESCE(T1."firstName", 'NO_NAME') + ' ' + COALESCE(T1."lastName", 'NO_LAST_NAME') "EmployeeName",
       DOCS."U_Status"                                                                       "Status",
       DOCS."U_StatusDate"                                                                   "StatusDate",
       DOCS."U_StatusEmpID"                                                                  "StatusEmployeeID",
       COALESCE(T2."firstName", 'NO_NAME') + ' ' + COALESCE(T2."lastName", 'NO_LAST_NAME') "StatusEmployeeName"
from [@LW_YUVAL08_GRPO] DOCS
         inner join OHEM T1 on T1."empID" = DOCS."U_empID"
         inner join OHEM T2 on T2."empID" = DOCS."U_StatusEmpID"