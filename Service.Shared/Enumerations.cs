namespace Service.Shared;

/// <summary>
/// Number Format Type
/// </summary>
public enum NumberFormatType {
    /// <summary>
    /// Amounts decimal configuration
    /// </summary>
    Sum,

    /// <summary>
    /// Price decimal configuration
    /// </summary>
    Price,

    /// <summary>
    /// Rate decimal configuration
    /// </summary>
    Rate,

    /// <summary>
    /// Quantity decimal configuration
    /// </summary>
    Quantity,

    /// <summary>
    /// Measure decimal configuration
    /// </summary>
    Measure,

    /// <summary>
    /// Amounts decimal configuration
    /// </summary>
    Percent,

    /// <summary>
    /// Percent decimal configuration
    /// </summary>
    None = -1
}

public enum ValidateNumericReturnValue {
    /// <summary>
    /// Blank Return Value
    /// </summary>
    Blank = -1,

    /// <summary>
    /// Valid Return Value
    /// </summary>
    Valid = 0,

    /// <summary>
    /// Invalid Return Value
    /// </summary>
    Invalid = 1,

    /// <summary>
    /// Precision Return Value
    /// </summary>
    Precision = 2
}

public enum DateNumber {
    /// <summary>
    /// First week of the month
    /// </summary>
    First = 0,

    /// <summary>
    /// Second week of the month
    /// </summary>
    Second = 1,

    /// <summary>
    /// Thirds week of the month
    /// </summary>
    Third = 2,

    /// <summary>
    /// Fourth week of the month
    /// </summary>
    Fourth = 3,

    /// <summary>
    /// Last week of the month
    /// </summary>
    Last = 4
}

public enum WeekDay {
    Day        = 8,
    Weekday    = 9,
    WeekendDay = 0,
    Sunday     = 1,
    Monday     = 2,
    Tuesday    = 3,
    Wednesday  = 4,
    Thursday   = 5,
    Friday     = 6,
    Saturday   = 7
}

public enum CheckFolderErrorType {
    /// <summary>
    /// Folder Access Valid
    /// </summary>
    OK,

    /// <summary>
    /// Folder does not exists
    /// </summary>
    FolderNotExists,

    /// <summary>
    /// Write Access Error
    /// </summary>
    WriteAccessError,

    /// <summary>
    /// Unknown
    /// </summary>
    Unknown
}

public enum InstanceProcesorType {
    /// <summary>
    /// Used when process is running as a 32 bit process
    /// </summary>
    x86 = 32,

    /// <summary>
    /// Used when process is running as a 64 bit process
    /// </summary>
    x64 = 64
}

public enum LogType {
    /// <summary>
    /// Used for an activation process
    /// </summary>
    Activation = 0,

    /// <summary>
    /// Used when executing an update process
    /// </summary>
    Update = 1,

    /// <summary>
    /// Used when executed a restore process
    /// </summary>
    Restore = 2,

    /// <summary>
    /// Used when executing an uninstall process
    /// </summary>
    Uninstall = 3,

    /// <summary>
    /// Used for generic log types (default)
    /// </summary>
    Regular = 4
}

public enum LogStatus {
    /// <summary>
    /// Used for processing log entry
    /// </summary>
    Processing = 0,

    /// <summary>
    /// Used for successful log entries
    /// </summary>
    Success = 1,

    /// <summary>
    /// Used for error log entries
    /// </summary>
    Error = 2
}

/// <summary>
/// Enumeration of the different office components you can check
/// </summary>
/// <remarks></remarks>
public enum OfficeComponent {
    Word,
    Excel,
    PowerPoint,
    Outlook
}

/// <summary>
/// Determine Type of time zone managed
/// Local: Server time zone
/// External: Time zone different from the server time zone
/// </summary>
public enum TimeZoneType {
    Local,
    External
}

/// <summary>
/// Version check enumeration value
/// </summary>
/// <remarks></remarks>
public enum VersionCheck {
    Current = 0,
    Newer   = 1,
    Older   = 2
}

/// <summary>
/// Define the type of debug output
/// </summary>
public enum DebugOutputType {
    Debug,
    Console,
    Trace
}

/// <summary>
/// SBO Version enumeration
/// </summary>
public enum Versions {
    Unknown = -1,
    SBO92   = 0,
    SBO93   = 1,
    SBO100  = 2
}

public enum DatabaseType {
    /// <summary>
    /// Microsoft SQL Server
    /// </summary>
    SQL = 0,

    /// <summary>
    /// SAP HANA Server
    /// </summary>
    HANA = 1
}

public enum ApplicationType {
    /// <summary>
    /// SAP Business One Addon
    /// </summary>
    SBO = 0,

    /// <summary>
    /// Windows Forms Application
    /// </summary>
    Windows = 1,

    /// <summary>
    /// Shop Floor Application
    /// </summary>
    ShopFloor = 2,

    /// <summary>
    /// Windows Service Application
    /// </summary>
    Service = 3
}

/// <summary>
/// Custom Object Types Enumeration
/// </summary>
public enum ObjectTypes {
    oChartOfAccounts                   = 1,
    oBusinessPartners                  = 2,
    oBanks                             = 3,
    oItems                             = 4,
    oBatchNumber                       = 10000044,
    oSerialNumber                      = 10000045,
    oVatGroups                         = 5,
    oPriceLists                        = 6,
    oSpecialPrices                     = 7,
    oItemProperties                    = 8,
    oBusinessPartnerGroups             = 10,
    oUsers                             = 12,
    oInvoices                          = 13,
    oCreditNotes                       = 14,
    oDeliveryNotes                     = 15,
    oReturns                           = 16,
    oOrders                            = 17,
    oPurchaseInvoices                  = 18,
    oPurchaseCreditNotes               = 19,
    oPurchaseDeliveryNotes             = 20,
    oPurchaseReturns                   = 21,
    oPurchaseOrders                    = 22,
    oQuotations                        = 23,
    oIncomingPayments                  = 24,
    oJournalVouchers                   = 28,
    oJournalEntries                    = 30,
    oStockTakings                      = 31,
    oContacts                          = 33,
    oCreditCards                       = 36,
    oCurrencyCodes                     = 37,
    oPaymentTermsTypes                 = 40,
    oBankPages                         = 42,
    oManufacturers                     = 43,
    oVendorPayments                    = 46,
    oLandedCostsCodes                  = 48,
    oShippingTypes                     = 49,
    oLengthMeasures                    = 50,
    oWeightMeasures                    = 51,
    oItemGroups                        = 52,
    oSalesPersons                      = 53,
    oCustomsGroups                     = 56,
    oChecksforPayment                  = 57,
    oInventoryGenEntry                 = 59,
    oInventoryGenExit                  = 60,
    oWarehouses                        = 64,
    oCommissionGroups                  = 65,
    oProductTrees                      = 66,
    oStockTransfer                     = 67,
    oWorkOrders                        = 68,
    oLandedCosts                       = 69,
    oCreditPaymentMethods              = 70,
    oCreditCardPayments                = 71,
    oAlternateCatNum                   = 73,
    oBudget                            = 77,
    oBudgetDistribution                = 78,
    oMessages                          = 81,
    oBudgetScenarios                   = 91,
    oUserDefaultGroups                 = 93,
    oOpportunities                     = 97,
    oSalesStages                       = 101,
    oActivityTypes                     = 103,
    oActivityLocations                 = 104,
    oDrafts                            = 112,
    oDeductionTaxHierarchies           = 116,
    oDeductionTaxGroups                = 117,
    oAdditionalExpenses                = 125,
    oSalesTaxAuthorities               = 126,
    oSalesTaxAuthoritiesTypes          = 127,
    oSalesTaxCodes                     = 128,
    oQueryCategories                   = 134,
    oFactoringIndicators               = 138,
    oPaymentsDrafts                    = 140,
    oAccountSegmentations              = 142,
    oAccountSegmentationCategories     = 143,
    oWarehouseLocations                = 144,
    oForms1099                         = 145,
    oInventoryCycles                   = 146,
    oWizardPaymentMethods              = 147,
    oBPPriorities                      = 150,
    oDunningLetters                    = 151,
    oUserFields                        = 152,
    oUserTables                        = 153,
    oPickLists                         = 156,
    oPaymentRunExport                  = 158,
    oUserQueries                       = 160,
    oInventoryRevaluation              = 162,
    oCorrectionPurchaseInvoice         = 163,
    oCorrectionPurchaseInvoiceReversal = 164,
    oCorrectionInvoice                 = 165,
    oCorrectionInvoiceReversal         = 166,
    oContractTemplates                 = 170,
    oEmployeesInfo                     = 171,
    oCustomerEquipmentCards            = 176,
    oWithholdingTaxCodes               = 178,
    oBillOfExchangeTransactions        = 182,
    oKnowledgeBaseSolutions            = 189,
    oServiceContracts                  = 190,
    oServiceCalls                      = 191,
    oUserKeys                          = 193,
    oQueue                             = 194,
    oDunningWizard                     = 197,
    oSalesForecast                     = 198,
    oTerritories                       = 200,
    oIndustries                        = 201,
    oProductionOrders                  = 202,
    oDownPayments                      = 203,
    oPurchaseDownPayments              = 204,
    oPackagesTypes                     = 205,
    oUserObjectsMD                     = 206,
    oTeams                             = 211,
    oRelationships                     = 212,
    oUserPermissionTree                = 214,
    oActivityStatus                    = 217,
    oChooseFromList                    = 218,
    oFormattedSearches                 = 219,
    oAttachments2                      = 221,
    oUserLanguages                     = 223,
    oMultiLanguageTranslations         = 224,
    oDynamicSystemStrings              = 229,
    oHouseBankAccounts                 = 231,
    oBusinessPlaces                    = 247,
    oLocalEra                          = 250,
    oNotaFiscalCFOP                    = 258,
    oNotaFiscalCST                     = 259,
    oNotaFiscalUsage                   = 260,
    oClosingDateProcedure              = 261,
    oBPFiscalRegistryID                = 278,
    oSalesTaxInvoice                   = 280,
    oPurchaseTaxInvoice                = 281,
    oResources                         = 290,
    oStockTransferDraft                = 1179,
    oBinLocation                       = 10000206,
    oBlanketAgreement                  = 1250000025,
    oReturnRequest                     = 234000031,
    oGoodsReturnRequest                = 234000032,
    oPurchaseQuotations                = 540000006,
    oInventoryTransferRequest          = 1250000001,
    oPurchaseRequest                   = 1470000113,
    oInventoryCounting                 = 1470000065,
    oInventoryPostings                 = 10000071,
    oInventoryOpeningBalance           = 310000001,
    Unknown                            = -1
}

public enum Authorization {
    GoodsReceipt           = 1,
    GoodsReceiptSupervisor = 2,
    Picking                = 3,
    PickingSupervisor      = 4,
    Counting               = 5,
    CountingSupervisor     = 6,
    Transfer               = 7,
    TransferSupervisor     = 8,
}