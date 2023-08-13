using System;

namespace Service.Shared.PrintLayout; 

public class SelectionDoneEventArgs : EventArgs {
    public OperationType Type       { get; }
    public LayoutData    LayoutData { get; }

    public SelectionDoneEventArgs(OperationType type, LayoutData layoutData) {
        Type       = type;
        LayoutData = layoutData;
    }
}