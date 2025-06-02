-- declare @ID int = 1074;
-- declare @BarCode nvarchar(255) = 'BOX';
-- declare @ItemCode nvarchar(50) = 'BOX';
-- declare @empID int = 2;
declare @WhsCode nvarchar(8) = (select U_LW_Branch
                                from OHEM
                                where empID = @empID);
select Case
           When T1.ItemCode is null Then -1
           When T0.BarCode <> T1.CodeBars and T3.BcdCode is null Then -2
           When T2.Code is null Then -3
           When T2.U_Status not in ('O', 'I') Then -4
           When T1.PrchseItem = 'N' Then -5
           When T2."U_Type" in ('S', 'R') and not exists(select 1
                                                         from "@LW_YUVAL08_GRPO3" X0
                                                                  left outer join POR1 X1 on X1."DocEntry" = X0."U_DocEntry" and X1."ObjType" = X0."U_ObjType" and X1."ItemCode" = @ItemCode and
                                                                                             X1."WhsCode" = @WhsCode
                                                                  left outer join PCH1 X2 on X2."DocEntry" = X0."U_DocEntry" and X2."ObjType" = X0."U_ObjType" and X2."ItemCode" = @ItemCode and
                                                                                             X2."WhsCode" = @WhsCode
                                                                  left outer join PDN1 X3 on X3."DocEntry" = X0."U_DocEntry" and X3."ObjType" = X0."U_ObjType" and X3."ItemCode" = @ItemCode and
                                                                                             X3."WhsCode" = @WhsCode
                                                         where X0.U_ID = @ID
                                                           and (X1."LineNum" is not null or X2."LineNum" is not null or X3."LineNum" is not null)) Then -6
           Else 0 End ValidationMessage
from (select @ID ID, @BarCode BarCode, @ItemCode ItemCode) T0
         left outer join OITM T1 on T1.ItemCode = T0.ItemCode
         left outer join "@LW_YUVAL08_GRPO" T2 on T2.Code = T0.ID
         left outer join OBCD T3 on T3.ItemCode = T0.ItemCode and T3.BcdCode = @BarCode

