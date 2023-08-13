using System;
using System.Linq;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

// ReSharper disable once InvalidXmlDocComment
/// <summary>
/// This namespace contains a collection of utilities for data conversions, data transformations and much more.
/// </summary>
namespace Service.Shared.Utils; 

/// <summary>
/// This utility contains string conversions functions and string generation
/// functions
/// </summary>
/// <remarks>
/// You can use the StringUtils directly or use it as an extension class
/// </remarks>
public static class StringUtils {
    /// <summary>
    /// This method is used to extract the left part of a string value
    /// </summary>
    /// <remarks>
    /// If the total length of the original value is less than the length parameter, the returned value will be equal to the original value
    /// </remarks>
    /// <param name="param">Original value from where the return value will be extracted</param>
    /// <param name="length">Total characters to be extracted from original value</param>
    /// <example>
    ///   <para></para>
    ///   <code lang="C#"><![CDATA[ string originalValue = "This is a test text";
    ///string value = StringUtils.Left(originalValue, 10);]]></code>
    /// </example>
    /// <includesource>yes</includesource>
    public static string Left(this string param, int length) {
        string result = param;
        if (result.Length > length)
            result = result.Substring(0, length);
        return result;
    }

    /// <summary>
    /// This method is used to extract the right part of a string value
    /// </summary>
    /// <remarks>
    /// If the total length of the original value is less than the length parameter, the returned value will be equal to the original value
    /// </remarks>
    /// <param name="param">Original value from where the return value will be extracted</param>
    /// <param name="length">Total characters to be extracted from original value</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[string originalValue = "This is a test text";
    ///string value = StringUtils.Right(originalValue, 10);]]></code>
    /// </example>
    /// <includesource>yes</includesource>
    public static string Right(this string param, int length) {
        string result = param;
        if (result.Length > length)
            result = result.Substring(param.Length - length, length);
        return result;
    }

    /// <summary>
    /// Converts a hex string to byte array
    /// </summary>
    /// <param name="hexString">Hex string value to convert to a byte array</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[byte[] byteValue = StringUtils.ToByteArray("5468697320697320612074657374");]]></code>
    ///   <para>The result of this conversion will be a byte array containing the string "This is a test"</para>
    /// </example>
    public static byte[] ToByteArray(this string hexString) {
        byte[] retVal = new byte[hexString.Length / 2];
        for (int i = 0; i < hexString.Length; i += 2)
            retVal[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
        return retVal;
    }

    /// <summary>
    /// Converts a string to byte array
    /// </summary>
    /// <param name="str">String value to be converted to a byte array</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[byte[] byteValue = StringUtils.StringToByteArray("This is a test");]]></code>
    /// </example>
    public static byte[] StringToByteArray(this string str) {
        byte[] bytes = new byte[str.Length * sizeof(char)];
        Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
        return bytes;
    }

    /// <summary>
    /// Converts a byte array to string
    /// </summary>
    /// <param name="ba">Byte array to be converted to a string</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[ var sample = new byte[] { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };
    /// string value = StringUtils.ByteArrayToString(sample);]]></code>
    /// </example>
    public static string ByteArrayToString(byte[] ba) {
        var hex = new StringBuilder(ba.Length * 2);
        foreach (byte b in ba)
            hex.AppendFormat("{0:X2}", b);
        return hex.ToString();
    }

    /// <summary>
    /// Convert string and a hex string value
    /// </summary>
    /// <param name="input">String value to be converted to hex string value</param>
    /// <param name="encoding">Encoding you want to use for the conversion</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[string hexString = StringUtils.ConvertStringToHex("This is a test", Encoding.UTF8);]]></code>
    /// </example>
    public static string ConvertStringToHex(this string input, Encoding encoding) {
        byte[] stringBytes = encoding.GetBytes(input);
        var    sbBytes     = new StringBuilder(stringBytes.Length * 2);
        foreach (byte b in stringBytes)
            sbBytes.AppendFormat("{0:X2}", b);
        return sbBytes.ToString();
    }

    /// <summary>
    /// Convert hex string to string value
    /// </summary>
    /// <param name="hexInput">Hex value to be converted to a string</param>
    /// <param name="encoding">Encoding of the original string</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[string value = StringUtils.ConvertHexToString("5468697320697320612074657374");]]></code>
    /// </example>
    public static string ConvertHexToString(this string hexInput, Encoding encoding) {
        int    numberChars = hexInput.Length;
        byte[] bytes       = new byte[numberChars / 2];
        for (int i = 0; i < numberChars; i += 2)
            bytes[i / 2] = Convert.ToByte(hexInput.Substring(i, 2), 16);
        return encoding.GetString(bytes);
    }

    /// <summary>
    /// This methods convert a string to an encrypted string
    /// </summary>
    /// <param name="value">String value to encrypt</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[string encryptedValue = StringUtils.EncryptData("This is a test");]]></code>
    /// </example>
    public static string EncryptData(this string value) {
        byte[] results      = null;
        var    utf8         = new UTF8Encoding();
        var    hashProvider = new MD5CryptoServiceProvider();
        byte[] key          = hashProvider.ComputeHash(utf8.GetBytes("kkklll"));
        var algorithm = new TripleDESCryptoServiceProvider {
            Key     = key,
            Mode    = CipherMode.ECB,
            Padding = PaddingMode.PKCS7
        };
        byte[] dataToEncrypt = utf8.GetBytes(value);
        try {
            results = algorithm.CreateEncryptor().TransformFinalBlock(dataToEncrypt, 0, dataToEncrypt.Length);
        }
        catch (Exception) {
            // ignored
        }
        finally {
            algorithm.Clear();
            hashProvider.Clear();
        }
        return Convert.ToBase64String(results);
    }

    /// <summary>
    /// This methods takes any encrypted string and decrypts it
    /// </summary>
    /// <param name="value">Pass the string you want to decrypt</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[string value = StringUtils.DecryptString("5LGS4FM7q/xqVrxgSfEAeg==");]]></code>
    /// </example>
    public static string DecryptString(this string value) {
        if (string.IsNullOrWhiteSpace(value))
            return "";
        byte[] results      = null;
        var    utf8         = new UTF8Encoding();
        var    hashProvider = new MD5CryptoServiceProvider();
        byte[] key          = hashProvider.ComputeHash(utf8.GetBytes("kkklll"));
        var algorithm = new TripleDESCryptoServiceProvider {
            Key     = key,
            Mode    = CipherMode.ECB,
            Padding = PaddingMode.PKCS7
        };
        byte[] dataToDecrypt = Convert.FromBase64String(value);

        try {
            results = algorithm.CreateDecryptor().TransformFinalBlock(dataToDecrypt, 0, dataToDecrypt.Length);
        }
        catch (Exception) {
            // ignored
        }
        finally {
            algorithm.Clear();
            hashProvider.Clear();
        }
        return utf8.GetString(results);
    }

    /// <summary>
    /// Create an MD5 computed hash out of a string
    /// </summary>
    /// <param name="input">String or content of file you want to create the MD5 hash</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[string md5Value = StringUtils.CreateMD5("This is a test");]]></code>
    /// </example>
    public static string CreateMD5(this string input) {
        // Use input string to calculate MD5 hash
        byte[] inputBytes = Encoding.ASCII.GetBytes(input);
        return CreateMD5(inputBytes);
    }

    /// <summary>
    /// Create an MD5 computed hash out of a byte array
    /// </summary>
    /// <param name="input">Byte array you want to create the MD5 hash</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[ var sample = new byte[] { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };
    /// string value = StringUtils.CreateMD5(sample);]]></code>
    /// </example>
    public static string CreateMD5(byte[] input) {
        using var md5       = MD5.Create();
        byte[]    hashBytes = md5.ComputeHash(input);

        // Convert the byte array to hexadecimal string
        var sb = new StringBuilder();
        foreach (byte b in hashBytes)
            sb.Append(b.ToString("X2"));
        return sb.ToString();
    }

    /// <summary>
    /// Generates a random string value of any given length
    /// </summary>
    /// <param name="length">The length of the random string</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[string randomString = StringUtils.RandomValue(20);]]></code>
    /// </example>
    public static string RandomValue(int length) {
        var random = new Random();
        return new string(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", length).Select(s => s[random.Next(s.Length)]).ToArray());
    }

    /// <summary>
    /// Extract value inside the parenthesis in a string
    /// </summary>
    /// <remarks>
    /// If string does not contain a value inside parenthesis a string.Empty value will be returned
    /// </remarks>
    /// <param name="value">Original string value from where the value will be extracted</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[string value = StringUtils.FromParenthesis("The value is (ABC)");]]></code>
    ///   <para>The result value our of this sample will be ABC</para>
    /// </example>
    public static string FromParenthesis(this string value) {
        try {
            return Regex.Match(value, "(\\()(.*?)(\\))").Groups[2].Value;
        }
        catch {
            return string.Empty;
        }
    }

    /// <summary>
    /// Check if value starts with a pattern value
    /// </summary>
    /// <param name="value">String value from where the comparison will be done</param>
    /// <param name="pattern">The pattern to compare with</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[string checkValue = "This is a test";
    ///if (StringUtils.Like(checkValue, "This")) {
    ///    //do...
    ///}]]></code>
    /// </example>
    public static bool Like(this string value, string pattern) {
        value   = value.ToLower();
        pattern = pattern.ToLower();
        if (pattern.IndexOf("*") == -1)
            return value.Contains(pattern);
        pattern = WildcardToRegex(pattern);
        return Regex.IsMatch(value, pattern);
    }

    /// <summary>
    /// Convert a string to regex compatible search pattern
    /// </summary>
    /// <param name="pattern">Original pattern to be converted</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[string wildcard = StringUtils.WildcardToRegex("This");]]></code>
    /// </example>
    public static string WildcardToRegex(this string pattern) => $"^{Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".")}$";

    /// <summary>
    /// Check if string address is a valid e-mail address
    /// </summary>
    /// <param name="address">Original e-mail address value</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[string checkValue = "test@test.net";
    ///if (StringUtils.IsEmailValid(checkValue)) {
    ///    //do...
    ///}]]></code>
    /// </example>
    public static bool IsEmailValid(string address) {
        try {
            var m = new MailAddress(address);
            return true;
        } catch {
            return false;
        }
    }

    /// <summary>
    /// Clear HTML Tags from a string variable
    /// </summary>
    /// <param name="value">Original string to clear</param>
    /// <example>
    ///   <para></para>
    ///   <code lang="C#"><![CDATA[string clearValue = StringUtils.ClearHTMLTags("<span color=red>Color is red</span>");]]></code>
    ///   <para>The result value our of this sample will be Color is red</para>
    /// </example>
    public static string ClearHTMLTags(string value) {
        value = Regex.Replace(value, @"<br[^>]*>", Environment.NewLine);
        value = Regex.Replace(value, @"<br>", Environment.NewLine);
        value = Regex.Replace(value, @"<[^>]*>", string.Empty);
        return value;
    }

    /// <summary>
    /// Use this if you are concatenating values and require a comma when it's not null
    /// </summary>
    /// <param name="value">Check if this value is not empty, return comma</param>
    public static string AggregateQuery(this string value) => ProcessQuery(value, ",");

    /// <summary>
    /// Use this if you are concatenating values and require a comma when it's not null
    /// </summary>
    /// <param name="value">Check if this value is not empty, return comma</param>
    public static string UnionQuery(this string value) => ProcessQuery(value, "union");

    /// <summary>
    /// Use this if you are concatenating values and require a comma when it's not null
    /// </summary>
    /// <param name="value">Check if this value is not empty, return and</param>
    public static string AndQuery(this string value) => ProcessQuery(value, "and");

    /// <summary>
    /// Use this if you are concatenating values and require a comma when it's not null
    /// </summary>
    /// <param name="value">Check if this value is not empty, return and</param>
    public static string OrQuery(this string value) => ProcessQuery(value, "or");
        
    private static string ProcessQuery(this string value, string replaceString) => !string.IsNullOrWhiteSpace(value) ? $" {replaceString} " : "";
}