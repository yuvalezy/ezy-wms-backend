select "U_ServiceVersion"                                                                      "Version",
       "U_ServiceUser"                                                                         "User",
       "U_ServicePassword"                                                                     "Password",
       'Y' "HelloWorld",
       U_DEBUG                                                                                 DEBUG,
       "U_PrintThread"                                                                         "PrintThread",
       Case When (select top 1 "Version" from CINF) < 1000180 Then 1 Else 0 End                "CrystalLegacy"
from "@LWCOMMON" T0
