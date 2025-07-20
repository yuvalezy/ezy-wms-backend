namespace Core.Models.Settings;

public class BackgroundServicesSettings {
    public BackgroundPickListSyncOptions PickListSync { get; set; } = new();
    public CloudSyncBackgroundOptions    CloudSync    { get; set; } = new();
}

public class BackgroundPickListSyncOptions {
    public int  IntervalSeconds { get; set; } = 60;
    public bool Enabled         { get; set; } = true;
    
    /// <summary>
    /// Whether to check for closed pick lists during sync
    /// </summary>
    public bool CheckClosedPickLists { get; set; } = true;
    
    /// <summary>
    /// Whether to process package movements when pick lists are closed with follow-up documents
    /// </summary>
    public bool ProcessPackageMovements { get; set; } = true;
}

public class CloudSyncBackgroundOptions {
    public int  SyncIntervalMinutes     { get; set; } = 10;
    public int  ValidationIntervalHours { get; set; } = 24;
    public bool Enabled                 { get; set; } = true;
}