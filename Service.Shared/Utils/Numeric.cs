using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Service.Shared.Company;

namespace Service.Shared.Utils; 

/// <summary>
/// This utility contains numeric conversions functions and format functions
/// </summary>
/// <remarks>
/// You can use the Numeric directly or use it as an extension class
/// </remarks>
public static class Numeric {
    /// <summary>
    /// Use this functions to convert any double value to a string value using always (.) as a decimal point.<br />
    ///SAP UI API DataSource values only accepts this type of values for numeric string so it doesn't matter what's the decimal configuration of the company details it will only accept this kind of values.
    /// </summary>
    /// <param name="value">Decimal value</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[decimal value = Convert.ToDecimal(199.95);
    /// BE1SConnectionController.Application.Forms.Item("UDOSample").DataSources.DBDataSources.Item("@BE1SSAMPLE1").SetValue("Quantity", 0, Numeric.ToParseValue(value));]]></code>
    /// </example>
    public static string ToParseValue(this decimal value) {
        string parseValue = value.ToString(ConnectionController.ParseFormatInfo);
        if (parseValue.IndexOf(".") == -1)
            parseValue += ".0";
        return parseValue;
    }

    /// <summary>
    /// Use this functions to convert any decimal value to a string value that's converted to SQL Decimal / HANA Numeric type<br />
    /// </summary>
    /// <param name="value">Decimal value</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[decimal value = Convert.ToDecimal(199.95);
    /// string query = $"update \"@TESTTABLE\" set \"U_TestField\" = {Numeric.ToQuery(value)}";]]></code>
    /// </example>
    public static string ToQuery(this decimal value) {
        string parseValue = value.ToString(ConnectionController.ParseFormatInfo);
        if (parseValue.IndexOf(".") == -1)
            parseValue += ".0";
        string convertType = ConnectionController.DatabaseType switch {
            DatabaseType.HANA => "decimal(21, 6)",
            _                 => "numeric(19, 6)",
        };
        return $"Cast({parseValue} as {convertType})";
    }

    /// <summary>
    /// Use this functions to convert any double value to a string value using always
    /// (.) as a decimal point.<br />
    /// SAP UI API DataSource values only accepts this type of values for numeric string
    /// so it doesn't matter what's the decimal configuration of the company
    /// details it will only accept this kind of values.
    /// </summary>
    /// <param name="value"></param>
    /// <example>
    ///   <para></para>
    ///   <code lang="C#"><![CDATA[double value = 199.95;
    /// BE1SConnectionController.Application.Forms.Item("UDOSample").DataSources.DBDataSources.Item("@BE1SSAMPLE1").SetValue("Quantity", 0, Numeric.ToParseValue(value));]]></code>
    /// </example>
    public static string ToParseValue(this double value) {
        string parseValue = value.ToString(ConnectionController.ParseFormatInfo);
        if (parseValue.IndexOf(".") == -1)
            parseValue += ".0";
        return parseValue;
    }

    /// <summary>
    /// Use this functions to convert any double value to a string value that's converted to SQL Decimal / HANA Numeric type<br />
    /// </summary>
    /// <param name="value">Double value</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[double value = 199.95;
    /// string query = $"update \"@TESTTABLE\" set \"U_TestField\" = {Numeric.ToQuery(value)}";]]></code>
    /// </example>
    public static string ToQuery(this double value) {
        string parseValue = value.ToString(ConnectionController.ParseFormatInfo);
        if (parseValue.IndexOf(".") == -1)
            parseValue += ".0";
        string convertType = ConnectionController.DatabaseType switch {
            DatabaseType.HANA => "decimal(21, 6)",
            _                 => "numeric(19, 6)",
        };
        return $"Cast({parseValue} as {convertType})";
    }

    /// <summary>
    /// Convert a string to decimal value.
    /// </summary>
    /// <remarks>
    /// If the string value is not in a valid format, the return value will be 0
    /// </remarks>
    /// <param name="value">The string value to be converted</param>
    /// <example>
    ///   <para>In this example I have a windows forms with a TextBox that uses the SAP Business One quantity value for Germany. The value of the text box is 1.999,95.<br />
    ///This string value will be converted to a decimal variable.</para>
    ///   <code lang="C#"><![CDATA[decimal value = Numeric.FromStringToDecimal(txtDecimalSample.Text);]]></code>
    /// </example>
    public static decimal FromStringToDecimal(string value) {
        if (value == null || value.Trim().Equals(""))
            return 0;
        return decimal.TryParse(value, NumberStyles.Any, ConnectionController.ParseFormatInfo, out decimal decimalValue) ? decimalValue : 0;
    }

    /// <summary>
    /// Convert a string to double value.
    /// </summary>
    /// <remarks>
    /// If the string value is not in a valid format, the return value will be 0
    /// </remarks>
    /// <param name="value">The string value to be converted</param>
    /// <example>
    ///   <para>In this example I have a windows forms with a TextBox that uses the SAP Business One quantity value for Germany. The value of the text box is 1.999,95.<br />
    ///This string value will be converted to a double variable.</para>
    ///   <code lang="C#"><![CDATA[double value = Numeric.FromStringToDouble(txtDoubleSample.Text);]]></code>
    /// </example>
    public static double FromStringToDouble(string value) {
        if (value == null || value.Trim().Equals(""))
            return 0;
        return double.TryParse(value, NumberStyles.Any, ConnectionController.ParseFormatInfo, out double doubleValue) ? doubleValue : 0;
    }

    /// <summary>
    /// Convert a string to integer value.
    /// </summary>
    /// <remarks>
    /// If the string value is not in a valid format, the return value will be 0
    /// </remarks>
    /// <param name="value">The string value to be converted</param>
    /// <example>
    ///   <para>In this example I have a windows forms with a TextBox that uses the SAP Business One quantity value for Germany. The value of the text box is 1999. <br />
    /// This string value will be converted to an integer variable.</para>
    ///   <code lang="C#"><![CDATA[int value = Numeric.FromStringToInt(txtIntegerSample.Text);]]></code>
    /// </example>
    public static int FromStringToInt(string value) {
        if (value == null || value.Trim().Equals(""))
            return 0;
        return int.TryParse(value, out int intValue) ? intValue : 0;
    }

    /// <summary>
    /// Function to check if a string value is numeric by trying to convert it to a decimal value
    /// </summary>
    /// <param name="value"></param>
    /// <example>
    ///   <code lang="C#"><![CDATA[if (Numeric.IsNumeric(txtDoubleSample.Text)) {
    ///    //do something
    ///}]]></code>
    /// </example>
    public static bool IsNumeric(this string value) => decimal.TryParse(value, NumberStyles.Any, ConnectionController.NumberFormatInfo, out _);

    /// <summary>
    /// Converts a decimal value to number format without thousand separator
    /// </summary>
    /// <remarks>
    /// This converter uses the decimals set in the SAP Company General Settings, Display, Decimal Places (Amounts)
    /// </remarks>
    /// <param name="value"></param>
    /// <example>
    ///   <para>In this example I will format the double value 1999.95 in a German database. The result will be a string 1999,95</para>
    ///   <para></para>
    ///   <code lang="C#"><![CDATA[decimal value = Convert.ToDecimal(1999.95);
    ///string formatValue = Numeric.ValueSum(value);]]></code>
    ///   <para></para>
    /// </example>
    public static string ValueSum(this decimal value) => value.ToString("F" + CompanySettings.SumDec, ConnectionController.NumberFormatInfo);

    /// <summary>
    /// Converts a double value to number format without thousand separator
    /// </summary>
    /// <remarks>
    /// This converter uses the decimals set in the SAP Company General Settings, Display, Decimal Places (Amounts)
    /// </remarks>
    /// <param name="value"></param>
    /// <example>
    ///   <para>In this example I will format the double value 1999.95 in a German database. The result will be a string 1999,95</para>
    ///   <code lang="C#"><![CDATA[double value = 1999.95;
    ///string formatValue = Numeric.ValueSum(value);]]></code>
    ///   <para></para>
    /// </example>
    public static string ValueSum(this double value) => value.ToString("F" + CompanySettings.SumDec, ConnectionController.NumberFormatInfo);

    /// <summary>
    /// Converts a decimal value to number format without thousand separator
    /// </summary>
    /// <remarks>
    /// This converter uses the decimals set in the SAP Company General Settings, Display, Decimal Places (Price)
    /// </remarks>
    /// <param name="value"></param>
    /// <example>
    ///   <para>In this example I will format the double value 1999.95 in a German database. The result will be a string 1999,95</para>
    ///   <para></para>
    ///   <code lang="C#"><![CDATA[decimal value = Convert.ToDecimal(1999.95);
    ///string formatValue = Numeric.ValuePrice(value);]]></code>
    ///   <para></para>
    /// </example>
    public static string ValuePrice(this decimal value) => value.ToString("F" + CompanySettings.PriceDec, ConnectionController.NumberFormatInfo);

    /// <summary>
    /// Converts a double value to number format without thousand separator
    /// </summary>
    /// <remarks>
    /// This converter uses the decimals set in the SAP Company General Settings, Display, Decimal Places (Price)
    /// </remarks>
    /// <param name="value"></param>
    /// <example>
    ///   <para>In this example I will format the double value 1999.95 in a German database. The result will be a string 1999,95</para>
    ///   <para></para>
    ///   <code lang="C#"><![CDATA[double value = 1999.95;
    ///string formatValue = Numeric.ValuePrice(value);]]></code>
    ///   <para></para>
    /// </example>
    public static string ValuePrice(this double value) => value.ToString("F" + CompanySettings.PriceDec, ConnectionController.NumberFormatInfo);

    /// <summary>
    /// Converts a decimal value to number format without thousand separator
    /// </summary>
    /// <remarks>
    /// This converter uses the decimals set in the SAP Company General Settings, Display, Decimal Places (Rate)
    /// </remarks>
    /// <param name="value"></param>
    /// <example>
    ///   <para>In this example I will format the double value 1999.95 in a German database. The result will be a string 1999,950</para>
    ///   <para></para>
    ///   <code lang="C#"><![CDATA[decimal value = Convert.ToDecimal(1999.95);
    ///string formatValue = Numeric.ValueRate(value);]]></code>
    ///   <para></para>
    /// </example>
    public static string ValueRate(this decimal value) => value.ToString("F" + CompanySettings.RateDec, ConnectionController.NumberFormatInfo);

    /// <summary>
    /// Converts a double value to number format without thousand separator
    /// </summary>
    /// <remarks>
    /// This converter uses the decimals set in the SAP Company General Settings, Display, Decimal Places (Rate)
    /// </remarks>
    /// <param name="value"></param>
    /// <example>
    ///   <para>In this example I will format the double value 1999.95 in a German database. The result will be a string 1999,950</para>
    ///   <code lang="C#"><![CDATA[double value = 1999.95;
    ///string formatValue = Numeric.ValueRate(value);]]></code>
    /// </example>
    public static string ValueRate(this double value) => value.ToString("F" + CompanySettings.RateDec, ConnectionController.NumberFormatInfo);

    /// <summary>
    /// Converts a decimal value to number format without thousand separator
    /// </summary>
    /// <remarks>
    /// This converter uses the decimals set in the SAP Company General Settings, Display, Decimal Places (Quantity)
    /// </remarks>
    /// <param name="value"></param>
    /// <example>
    ///   <para>In this example I will format the double value 1999.95 in a German database. The result will be a string 1999,950</para>
    ///   <code lang="C#"><![CDATA[decimal value = Convert.ToDecimal(1999.95);
    ///string formatValue = Numeric.ValueQuantity(value);]]></code>
    ///   <para></para>
    /// </example>
    public static string ValueQuantity(this decimal value) => value.ToString("F" + CompanySettings.QtyDec, ConnectionController.NumberFormatInfo);

    /// <summary>
    /// Converts a double value to number format without thousand separator
    /// </summary>
    /// <remarks>
    /// This converter uses the decimals set in the SAP Company General Settings, Display, Decimal Places (Quantity)
    /// </remarks>
    /// <param name="value"></param>
    /// <example>
    ///   <para>In this example I will format the double value 1999.95 in a German database. The result will be a string 1999,95</para>
    ///   <code lang="C#"><![CDATA[double value = 1999.95;
    ///string formatValue = ValueQuantity(value);]]></code>
    /// </example>
    public static string ValueQuantity(this double value) => value.ToString("F" + CompanySettings.QtyDec, ConnectionController.NumberFormatInfo);

    /// <summary>
    /// Converts a decimal value to number format without thousand separator
    /// </summary>
    /// <remarks>
    /// This converter uses the decimals set in the SAP Company General Settings, Display, Decimal Places (Measure)
    /// </remarks>
    /// <param name="value"></param>
    /// <example>
    ///   <para>In this example I will format the double value 1999.95 in a German database. The result will be a string 1999,950</para>
    ///   <para></para>
    ///   <code lang="C#"><![CDATA[decimal value = Convert.ToDecimal(1999.95);
    ///string formatValue = Numeric.ValueMeasure(value);]]></code>
    ///   <para></para>
    /// </example>
    public static string ValueMeasure(this decimal value) => value.ToString("F" + CompanySettings.MeasureDec, ConnectionController.NumberFormatInfo);

    /// <summary>
    /// Converts a double value to number format without thousand separator
    /// </summary>
    /// <remarks>
    /// This converter uses the decimals set in the SAP Company General Settings, Display, Decimal Places (Measure)
    /// </remarks>
    /// <param name="value"></param>
    /// <example>
    ///   <para>In this example I will format the double value 1999.95 in a German database. The result will be a string 1999,95</para>
    ///   <code lang="C#"><![CDATA[double value = 1999.95;
    ///string formatValue = Numeric.ValueMeasure(value);]]></code>
    /// </example>
    public static string ValueMeasure(this double value) => value.ToString("F" + CompanySettings.MeasureDec, ConnectionController.NumberFormatInfo);

    /// <summary>
    /// Converts a decimal value to number format without thousand separator
    /// </summary>
    /// <remarks>
    /// This converter uses the decimals set in the SAP Company General Settings, Display, Decimal Places (Percent)
    /// </remarks>
    /// <param name="value"></param>
    /// <example>
    ///   <para>In this example I will format the double value 1999.95 in a German database. The result will be a string 1999,950</para>
    ///   <code lang="C#"><![CDATA[decimal value = Convert.ToDecimal(1999.95);
    ///string formatValue = Numeric.ValuePercent(value);]]></code>
    ///   <para></para>
    /// </example>
    public static string ValuePercent(this decimal value) => value.ToString("F" + CompanySettings.PercentDec, ConnectionController.NumberFormatInfo);

    /// <summary>
    /// Converts a double value to number format without thousand separator
    /// </summary>
    /// <remarks>
    /// This converter uses the decimals set in the SAP Company General Settings, Display, Decimal Places (Percent)
    /// </remarks>
    /// <param name="value"></param>
    /// <example>
    ///   <para>In this example I will format the double value 1999.95 in a German database. The result will be a string 1999,95</para>
    ///   <code lang="C#"><![CDATA[double value = 1999.95;
    ///string formatValue = Numeric.ValuePercent(value);]]></code>
    /// </example>
    public static string ValuePercent(this double value) => value.ToString("F" + CompanySettings.PercentDec, ConnectionController.NumberFormatInfo);

    /// <summary>
    /// Converts an integer value to a string
    /// </summary>
    /// <remarks>
    /// If integer value is less then 0 the return value will by an empty string
    /// </remarks>
    /// <param name="value"></param>
    /// <example>
    ///   <code lang="C#"><![CDATA[int value = 1999;
    ///string stringInteger = Numeric.ValueInteger(value);]]></code>
    /// </example>
    public static string ValueInteger(this int value) => value >= 0 ? value.ToString() : "";

    /// <summary>
    /// Function to get the decimal number by format type
    /// </summary>
    /// <param name="format">Format type <see cref="NumberFormatType" /> for the
    /// decimal configuration</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[int quantityDecimals = Numeric.DecimalByFormat(Numeric.enNumberFormatType.Quantity);]]></code>
    /// </example>
    public static short DecimalByFormat(NumberFormatType format) =>
        format switch {
            NumberFormatType.Sum      => CompanySettings.SumDec,
            NumberFormatType.Price    => CompanySettings.PriceDec,
            NumberFormatType.Rate     => CompanySettings.RateDec,
            NumberFormatType.Quantity => CompanySettings.QtyDec,
            NumberFormatType.Measure  => CompanySettings.MeasureDec,
            NumberFormatType.Percent  => CompanySettings.PercentDec,
            _                         => 0
        };

    /// <summary>
    /// Truncate decimal trailing to format number string
    /// </summary>
    /// <param name="value">The double variable to be converted</param>
    /// <param name="format">Format type <see cref="NumberFormatType" /> for the
    /// decimal configuration</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[double value = Numeric.TruncateNumber(4.123456, Numeric.enNumberFormatType.Sum);]]></code>
    /// </example>
    public static double TruncateNumber(double value, NumberFormatType format) {
        short decimals = DecimalByFormat(format);
        short calc     = 1;
        for (int i = 1; i <= decimals; i++)
            calc *= 10;
        return Math.Floor(value * calc) / calc;
    }

    /// <summary>
    /// Truncate decimal trailing to format number string
    /// </summary>
    /// <param name="value">The decimal variable to be converted</param>
    /// <param name="format">Format type <see cref="NumberFormatType" /> for the
    /// decimal configuration</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[double value = Numeric.TruncateNumber(4.123456, Numeric.enNumberFormatType.Sum);]]></code>
    /// </example>
    public static decimal TruncateNumber(decimal value, NumberFormatType format) {
        short decimals = DecimalByFormat(format);
        short calc     = 1;
        for (int i = 1; i <= decimals; i++)
            calc *= 10;
        return Math.Floor(value * calc) / calc;
    }

    /// <summary>
    /// Format number of any numeric type (double, decimal, float, etc) to a formatted string
    /// </summary>
    /// <remarks>
    /// This format uses the decimal and thousand separator using the NumberFormatInfo CultureInfo
    /// </remarks>
    /// <param name="value">The double, decimal, float, etc. variable to be converted</param>
    /// <param name="format">Format type <see cref="NumberFormatType" /> for the decimal configuration</param>
    /// <param name="groupSeparator">Group separator for thousands values.</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[string formatString = Numeric.FormatNumber(2999.95, Numeric.enNumberFormatType.Sum, true);]]></code>
    /// </example>
    public static string FormatNumber(object value, NumberFormatType format, bool groupSeparator = true) {
        //Load decimal points for format
        short  d = DecimalByFormat(format);
        string retVal;

        //Convert value to formatted string
        try {
            retVal = Convert.ToDouble(value).ToString((groupSeparator ? "N" : "F") + d, ConnectionController.NumberFormatInfo);
        }
        catch {
            retVal = 0.0.ToString((groupSeparator ? "N" : "F") + d, ConnectionController.NumberFormatInfo);
        }

        //If format type is quantity remove the last 0 values at the end of the decimal points.
        //For example I have a value of 26.50 the end result will be 26.5
        if (format != NumberFormatType.Quantity) return retVal;
        while (retVal.EndsWith("0"))
            retVal = retVal.Substring(0, retVal.LastIndexOf("0"));
        if (retVal.EndsWith(CompanySettings.DecSep))
            retVal = retVal.Substring(0, retVal.LastIndexOf(CompanySettings.DecSep));

        return retVal;
    }

    /// <summary>
    /// Format number of any numeric type (double, decimal, float, etc) to a formatted string
    /// </summary>
    /// <param name="value">The double, decimal, float, etc. variable to be converted</param>
    /// <param name="decimals">Number of decimals required in the formatted
    /// string</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[string formatString = Numeric.FormatNumber(2999.95, 2);]]></code>
    /// </example>
    public static string FormatNumber(object value, int decimals) {
        string retVal;
        //Convert value to formatted string
        try {
            retVal = Convert.ToDouble(value).ToString("N" + decimals, ConnectionController.NumberFormatInfo);
        }
        catch {
            retVal = 0.0.ToString("N" + decimals, ConnectionController.NumberFormatInfo);
        }

        return retVal;
    }

    /// <summary>
    /// Convert a formatted string back to decimal
    /// </summary>
    /// <remarks>
    /// If the decimal separator is (.) and the thousand separator is (,) then the value must be 1,234.45
    /// </remarks>
    /// <param name="value">The value string must be in the right format</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[decimal value = Numeric.FromFormat("2999,95");]]></code>
    /// </example>
    public static decimal FromFormat(object value) {
        try {
            if (value is string stringValue && string.IsNullOrWhiteSpace(stringValue))
                return 0;
            return decimal.Parse(value.ToString(), ConnectionController.NumberFormatInfo);
        }
        catch {
            return 0;
        }
    }

    /// <summary>
    /// Convert a formatted string back to double
    /// </summary>
    /// <remarks>
    /// If the double separator is (.) and the thousand separator is (,) then the value must be 1,234.45
    /// </remarks>
    /// <param name="value">The value string must be in the right format</param>return readValue != null ? (T) Convert.ChangeType(readValue, typeof(T)) : default; 
    /// <example>
    ///   <code lang="C#"><![CDATA[double value = Numeric.FromFormat("2999,95");]]></code>
    /// </example>
    public static double FromFormatDouble(object value) {
        try {
            if (value is string stringValue && string.IsNullOrWhiteSpace(stringValue))
                return 0;
            return double.Parse(value.ToString(), ConnectionController.NumberFormatInfo);
        }
        catch {
            return 0;
        }
    }

    private static List<int> NumericCharacters {
        get {
            if (numericCharacters != null)
                return numericCharacters;
            numericCharacters = Enumerable.Range(48, 10).ToList();
            numericCharacters.AddRange(new[] { 8, 9, 13, 27, 37, 38, 39, 40 });
            return numericCharacters;
        }
    }

    private static List<int> numericCharacters;

    /// <summary>
    /// This method is used to validated if the character pressed in the keyboard is a numeric value or a decimal / thousand separator
    /// </summary>
    /// <param name="charPressed">The integer number of the pressed character</param>
    /// <param name="decimals">Allow decimal values</param>
    /// <param name="thousands">Allow thousands separator</param>
    /// <returns></returns>
    public static bool IsNumericChar(int charPressed, bool decimals = false, bool thousands = false) {
        bool isNumeric = NumericCharacters.Contains(charPressed);
        if (!isNumeric && decimals)
            isNumeric = Convert.ToChar(CompanySettings.DecSep) == charPressed;
        if (!isNumeric && thousands)
            isNumeric = Convert.ToChar(CompanySettings.ThousSep) == charPressed;
        return isNumeric;
    }


    /// <summary>
    /// Validated if a string value is a valid numeric value and checks the decimal precision length
    /// </summary>
    /// <param name="valueStr">String value to check</param>
    /// <param name="precision">Length of permitted decimal length</param>
    public static ValidateNumericReturnValue ValidateNumericValueFromString(string valueStr, short precision) {
        if (string.IsNullOrWhiteSpace(valueStr))
            return ValidateNumericReturnValue.Blank;
        decimal value = valueStr.ToDecimal();
        if (value == 0 && !valueStr.Equals("0") && !valueStr.Equals(FormatNumber(0, precision)))
            return ValidateNumericReturnValue.Invalid;

        if (precision == -1)
            return ValidateNumericReturnValue.Valid;

        char[] decChar = CompanySettings.DecSep.ToCharArray();
        if (!valueStr.Contains(decChar[0]))
            return ValidateNumericReturnValue.Valid;

        int decLength = valueStr.Split(decChar)[1].Length;
        return decLength > precision ? ValidateNumericReturnValue.Precision : ValidateNumericReturnValue.Valid;
    }

    /// <summary>
    /// Function to extract numbers from a currency string.
    /// Example 70,91 EUR
    /// </summary>
    /// <param name="value">The formatted value where the numbers will be extracted from</param>
    /// <returns>string without the currency 70,91</returns>
    public static string ExtractNumbersFromCurrency(string value) {
        value = Regex.Replace(value, $"\\{CompanySettings.ThousSep}", "");
        value = Regex.Match(value, $@"[-+]?[0-9]*\{CompanySettings.DecSep}?[0-9]+(?:[eE][-+]?[0-9]+)?").Value;
        return value;
    }

    /// <summary>
    /// Function to extract numbers from a currency string and converts it to decimal type
    /// Example 70,91 EUR
    /// </summary>
    /// <param name="value">The formatted value where the numbers will be extracted from</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[decimal value = Numeric.ExtractDecimalFromCurrency("70,91 EUR");]]></code>
    /// </example>
    /// <returns>decimal value of 70.91</returns>
    public static decimal ExtractDecimalFromCurrency(string value) => ExtractNumbersFromCurrency(value).ToDecimal();

    /// <summary>
    /// Function to extract numbers from a currency string and converts it to Double type
    /// Example 70,91 EUR
    /// </summary>
    /// <param name="value">The formatted value where the numbers will be extracted from</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[double value = Numeric.ExtractDoubleFromCurrency("70,91 EUR");]]></code>
    /// </example>
    /// <returns>double value of 70.91</returns>
    public static double ExtractDoubleFromCurrency(string value) => ExtractNumbersFromCurrency(value).ToDouble();

    /// <summary>
    /// Get minimum decimal value
    /// </summary>
    /// <remarks>
    /// For example if decimal size is 3 decimals, return value will be 0.001
    /// </remarks>
    /// <param name="type">Number format type</param>
    public static decimal MinimumDecimalValue(NumberFormatType type) {
        int decimals = MinimumDecimals(type);
        return decimals switch {
            0 => 0,
            1 => (decimal)0.1,
            2 => (decimal)0.01,
            3 => (decimal)0.001,
            4 => (decimal)0.0001,
            5 => (decimal)0.00001,
            6 => (decimal)0.000001,
            _ => 0
        };
    }

    /// <summary>
    /// Get minimum double value
    /// </summary>
    /// <remarks>
    /// For example if double size is 3 doubles, return value will be 0.001
    /// </remarks>
    /// <param name="type">Number format type</param>
    public static double MinimumDoubleValue(NumberFormatType type) {
        int decimals = MinimumDecimals(type);
        return decimals switch {
            0 => 0,
            1 => 0.1,
            2 => 0.01,
            3 => 0.001,
            4 => 0.0001,
            5 => 0.00001,
            6 => 0.000001,
            _ => 0
        };
    }

    private static int MinimumDecimals(NumberFormatType type) {
        int decimals = type switch {
            NumberFormatType.Sum      => CompanySettings.SumDec,
            NumberFormatType.Price    => CompanySettings.PriceDec,
            NumberFormatType.Rate     => CompanySettings.RateDec,
            NumberFormatType.Quantity => CompanySettings.QtyDec,
            NumberFormatType.Measure  => CompanySettings.MeasureDec,
            NumberFormatType.Percent  => CompanySettings.PercentDec,
            _                         => 0,
        };
        return decimals;
    }
}