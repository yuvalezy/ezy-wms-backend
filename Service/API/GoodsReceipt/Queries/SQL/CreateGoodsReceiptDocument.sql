declare @LineID int = IsNull((select Max("U_LineID") + 1
                              from "@LW_YUVAL08_GRPO3" where "U_ID" = @ID), 0);
insert into "@LW_YUVAL08_GRPO3"(U_ID, "U_LineID", "U_ObjType", "U_DocEntry")
values(@ID, @LineID, @ObjType, @DocEntry)
