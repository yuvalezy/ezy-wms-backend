using System;
using System.Reflection;

namespace Service.Shared.Data; 

/// <compilewhen>never</compilewhen>
[AttributeUsage(AttributeTargets.Property)]
public abstract class ReaderColumn : Attribute {
    public string ID        { get; internal set; }
    public string AlterType { get; set; }

    /// <summary>
    /// todo implement this so I don't have to manually remove lines
    /// </summary>
    public bool IgnoreIfEmpty { get; set; }

    internal PropertyInfo Property { get; set; }

    protected ReaderColumn(string id) => ID = id;
}

/// <compilewhen>never</compilewhen>
public abstract class BaseReader<T> where T : class {
    protected T GetObject() => Activator.CreateInstance<T>();
}