update [@LW_YUVAL08_GRPO] set "U_Status" = @Status, "U_StatusDate" = getdate(), "U_StatusEmpID" = @empID where "Code" = @ID;
If @Status = 'C' Begin
    update [@LW_YUVAL08_GRPO1] set U_TargetStatus = 'C' where U_ID = @ID
end