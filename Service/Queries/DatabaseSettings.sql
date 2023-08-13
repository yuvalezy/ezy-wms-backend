select "U_Version"                                                              "Version",
       "U_User"                                                                 "User",
       "U_Password"                                                             "Password",
       'Y'                                                                      "TestHelloWorld",
       U_DEBUG                                                                  DEBUG,
       "U_PrintThread"                                                          "PrintThread",
       Case When (select top 1 "Version" from CINF) < 1000180 Then 1 Else 0 End "CrystalLegacy"
from "@LW_YUVAL08_COMMON" T0
