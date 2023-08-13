using SAPbobsCOM;
using Service.Shared.Utils;

namespace Service.Shared.Company; 

/// <summary>
/// This class contains the company settings that are loaded from the SAP Business
/// One database
/// </summary>
public static class CompanySettings {
    /// <summary>
    /// Gets or sets a value indicating whether the connected company has Multi Language support enabled
    /// This settings is from SBO, Administration, Company Details, Multi-Language Support
    /// </summary>
    /// <value>
    ///   true if ; otherwise, <see langword="false" />.</value>
    /// <remarks></remarks>
    public static bool MultiLanguage { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the connected company item cost is managed at warehouse level
    /// This settings is from SBO, Administration, Company Details, Basic Initialization, Manage Item Cost per Warehouse
    /// </summary>
    /// <value>
    ///   true if ; otherwise, <see langword="false" />.</value>
    /// <remarks></remarks>
    public static bool ItemCostPerWarehouse { get; set; }

    /// <summary>
    /// Gets or sets a value with the decimals to be displayed in amount numeric values.
    /// This settings is from SBO, Administration, General Settings, Display, Decimal Places, Amounts
    /// </summary>
    public static short SumDec { get; set; }

    /// <summary>
    /// Gets or sets a value with the decimals to be displayed in price numeric values.
    /// This settings is from SBO, Administration, General Settings, Display, Decimal Places, Prices
    /// </summary>
    public static short PriceDec { get; set; }

    /// <summary>
    /// Gets or sets a value with the decimals to be displayed in rate numeric values.
    /// This settings is from SBO, Administration, General Settings, Display, Decimal Places, Rates
    /// </summary>
    public static short RateDec { get; set; }

    /// <summary>
    /// Gets or sets a value with the decimals to be displayed in quantity numeric values.
    /// This settings is from SBO, Administration, General Settings, Display, Decimal Places, Quantity
    /// </summary>
    public static short QtyDec { get; set; }

    /// <summary>
    /// Gets or sets a value with the decimals to be displayed in amount numeric values.
    /// This settings is from SBO, Administration, General Settings, Display, Decimal Places, Amounts
    /// </summary>
    public static short PercentDec { get; set; }

    /// <summary>
    /// Gets or sets a value with the decimals to be displayed in measure numeric values.
    /// This settings is from SBO, Administration, General Settings, Display, Decimal Places, Measures
    /// </summary>
    public static short MeasureDec { get; set; }

    /// <summary>
    /// Gets or sets a value with the thousands separator character to be displays in numeric values.
    /// This settings is from SBO, Administration, General Settings, Display, Decimal Places, Thousands Separator
    /// </summary>
    public static string ThousSep { get; set; }

    /// <summary>
    /// Gets or sets a value with the decimal separator character to be displays in numeric values.
    /// This settings is from SBO, Administration, General Settings, Display, Decimal Places, Decimal Separator
    /// </summary>
    public static string DecSep { get; set; }

    /// <summary>
    /// Gets or sets a value of the local currency of the SBO Company Database
    /// This settings is from SBO, Administration, Company Details, Local Currency
    /// </summary>
    public static string MainCurr { get; set; }

    /// <summary>
    /// Gets or sets a value of the system currency of the SBO Company Database
    /// This settings is from SBO, Administration, Company Details, System Currency
    /// </summary>
    public static string SysCurrncy { get; set; }

    /// <summary>
    /// Gets or sets a value with the time format.
    /// This settings is from SBO, Administration, General Settings, Display, Time Format
    /// </summary>
    public static BoTimeTemplate TimeFormat { get; set; }

    /// <summary>
    /// Gets or sets a value with the date format.
    /// This settings is from SBO, Administration, General Settings, Display, Date Format
    /// </summary>
    public static BoDateTemplate DateFormat { get; set; }

    /// <summary>
    /// Gets or sets a value with the Date separator character to be displays in date values.
    /// This settings is from SBO, Administration, General Settings, Display, Decimal Places, Date Separator
    /// </summary>
    public static string DateSep { get; set; }

    /// <summary>
    /// Gets or sets a value the current SAP Business One Database Version
    /// </summary>
    public static Versions SboVersion;

    /// <summary>
    /// Gets or sets a value the current SAP Business One Crystal Reports Legacy
    /// </summary>
    public static bool CrystalLegacy { get; set; }

    /// <summary>
    /// This methods loads the settings from SBO table OADM into the Company Settings
    /// Class
    /// </summary>
    /// <remarks>
    /// This method should only be called after the ConnectionController Company Object
    /// has connected to a SAP Business one database using the
    /// ConnectionController.Connect Method
    /// </remarks>
    /// <example>
    /// <para></para>
    /// <code lang="C#"><![CDATA[ConnectionController.ConnectCompany("LAPTOP-K2UQK03D", BoDataServerTypes.dst_MSSQL2016, "sa", "be1s", "SBODemo_DE", "manager", "be1s", "LAPTOP-K2UQK03D");
    /// CompanySettings.Load();]]></code>
    /// <para></para>
    /// </example>
    public static void Load() {
        var rs = Utils.Shared.GetRecordset();
        rs.DoQuery(Queries.LoadCompanyDetails);
        DecSep   = rs.GetString("DecSep");
        ThousSep = rs.GetString("ThousSep");
        if (rs.GetString("SumDec") != "")
            SumDec = rs.GetInt16("SumDec");
        if (rs.GetString("PriceDec") != "")
            PriceDec = rs.GetInt16("PriceDec");
        if (rs.GetString("RateDec") != "")
            RateDec = rs.GetInt16("RateDec");
        if (rs.GetString("QtyDec") != "")
            QtyDec = rs.GetInt16("QtyDec");
        if (rs.GetString("MeasureDec") != "")
            MeasureDec = rs.GetInt16("MeasureDec");
        if (rs.GetString("PercentDec") != "")
            PercentDec = rs.GetInt16("PercentDec");
        if (rs.GetString("MainCurncy") != "")
            MainCurr = ((string)rs.Fields.Item("MainCurncy").Value).TrimEnd();
        if (rs.GetString("SysCurrncy") != "")
            SysCurrncy = ((string)rs.Fields.Item("SysCurrncy").Value).TrimEnd();

        DateFormat           = (BoDateTemplate)rs.GetInt16("DateFormat");
        DateSep              = rs.GetString("DateSep");
        TimeFormat           = (BoTimeTemplate)rs.GetInt16("TimeFormat");
        MultiLanguage        = rs.GetString("MultiLang") == "Y";
        ItemCostPerWarehouse = rs.GetString("PriceSys") == "Y";
        SboVersion           = (Versions)rs.GetInt16("SBOVersion");
        CrystalLegacy        = rs.GetBool("CrystalLegacy");

        ConnectionController.NumberFormatInfo.NumberFormat.NumberDecimalSeparator = DecSep;
        ConnectionController.NumberFormatInfo.NumberFormat.NumberGroupSeparator   = ThousSep;
    }
}