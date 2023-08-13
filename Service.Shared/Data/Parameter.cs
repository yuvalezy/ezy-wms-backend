using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Service.Shared.Data; 

/// <summary>
/// This is a parameters list object used in <see cref="Procedure"/>.
/// </summary>
/// <remarks></remarks>
public class Parameters : List<Parameter> {
    /// <summary>
    /// Default Parameters Constructor
    /// </summary>
    public Parameters() {
    }

    /// <summary>
    /// Parameters Constructor with values
    /// </summary>
    /// <param name="parameters"></param>
    public Parameters(params Parameter[] parameters) => AddRange(parameters);

    /// <summary>
    /// Helps to easily find a parameter in the parameters collection
    /// </summary>
    /// <param name="name">Parameter name</param>
    /// <example>
    ///   <para>In this example I have a parameters collection called myParams and I want to
    /// change the ItemCode variable value to "ABC"</para>
    ///   <para></para>
    ///   <code lang="C#"><![CDATA[myParams.Parameters["ItemCode"].Value = "ABC";]]></code>
    ///   <para></para>
    /// </example>
    public Parameter this[string name] => this.FirstOrDefault(p => p.Name == name);

    /// <summary>
    /// Initializes a new instance of the <see cref="Parameter" /> class. 
    /// </summary>
    /// <param name="name">Parameter name</param>
    /// <remarks></remarks>
    public Parameter Add(string name) {
        var parameter = new Parameter(name);
        Add(parameter);
        return parameter;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Parameter" /> class. 
    /// </summary>
    /// <param name="name">Parameter name</param>
    /// <param name="value">Parameter value</param>
    /// <remarks></remarks>
    public Parameter Add(string name, object value) {
        var parameter = new Parameter(name, value);
        Add(parameter);
        return parameter;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Parameter" /> class. 
    /// </summary>
    /// <param name="name">Parameter name</param>
    /// <param name="type">Parameter type</param>
    /// <remarks></remarks>
    public Parameter Add(string name, SqlDbType type) {
        var parameter = new Parameter(name, type);
        Add(parameter);
        return parameter;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Parameter" /> class. 
    /// </summary>
    /// <param name="name">Parameter name</param>
    /// <param name="type">Parameter type</param>
    /// <param name="value">Parameter value</param>
    /// <remarks></remarks>
    public Parameter Add(string name, SqlDbType type, object value) {
        var parameter = new Parameter(name, type, value);
        Add(parameter);
        return parameter;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Parameter" /> class. 
    /// </summary>
    /// <param name="name">Parameter name</param>
    /// <param name="type">Parameter type</param>
    /// <param name="size">Parameter string length</param>
    /// <param name="value">Parameter value</param>
    /// <remarks></remarks>
    public Parameter Add(string name, SqlDbType type, int size, object value) {
        var parameter = new Parameter(name, type, size, value);
        Add(parameter);
        return parameter;
    }
}

/// <summary>
/// This is used to contain the parameter properties used in <see cref="Procedure"/>.
/// </summary>
/// <remarks></remarks>
public class Parameter {
    /// <summary>
    /// Gets or sets the parameter name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the parameter type. It used SqlDbType enumeration for both SQL and HANA
    /// </summary>
    public SqlDbType Type { get; set; }

    /// <summary>
    /// Gets or sets size. This is used for string parameters
    /// </summary>
    public int Size { get; set; }

    /// <summary>
    /// Gets or sets the parameter value.
    /// </summary>
    /// <value></value>
    /// <remarks></remarks>
    public object Value { get; set; }

    /// <summary>
    /// Gets or sets the parameter direction value.
    /// </summary>
    /// <value></value>
    /// <remarks></remarks>
    public ParameterDirection Direction { get; set; } = ParameterDirection.Input;

    /// <summary>
    /// Initializes a new instance of the <see cref="Parameter" /> class. 
    /// </summary>
    /// <remarks></remarks>
    public Parameter() {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Parameter" /> class. 
    /// </summary>
    /// <param name="name">Parameter name</param>
    /// <remarks></remarks>
    public Parameter(string name) {
        Name = name;
        Type = SqlDbType.NVarChar;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Parameter" /> class. 
    /// </summary>
    /// <param name="name">Parameter name</param>
    /// <param name="value">Parameter value</param>
    /// <remarks></remarks>
    public Parameter(string name, object value) {
        Name  = name;
        Value = value;
        Type  = SqlDbType.NVarChar;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Parameter" /> class. 
    /// </summary>
    /// <param name="name">Parameter name</param>
    /// <param name="type">Parameter type</param>
    /// <remarks></remarks>
    public Parameter(string name, SqlDbType type) {
        Name = name;
        Type = type;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Parameter" /> class. 
    /// </summary>
    /// <param name="name">Parameter name</param>
    /// <param name="type">Parameter type</param>
    /// <param name="value">Parameter value</param>
    /// <remarks></remarks>
    public Parameter(string name, SqlDbType type, object value) {
        Name  = name;
        Type  = type;
        Value = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Parameter" /> class. 
    /// </summary>
    /// <param name="name">Parameter name</param>
    /// <param name="type">Parameter type</param>
    /// <param name="size">Parameter string length</param>
    /// <param name="value">Parameter value</param>
    /// <remarks></remarks>
    public Parameter(string name, SqlDbType type, int size, object value) {
        Name  = name;
        Type  = type;
        Size  = size;
        Value = value;
    }
}