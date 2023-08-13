using System;
using System.Diagnostics;
using System.IO;

namespace Service.Shared.Utils; 

/// <summary>
/// Utility to create trace files for your application in a temporary folder
/// </summary>
public class Tracer {
    /// <summary>
    /// Get the tracer file file name
    /// </summary>
    public string FileName { get; set; }
        
    /// <summary>
    /// Gets or sets the level of tracing
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Initializes the tracer object
    /// </summary>
    /// <remarks>
    /// File name to be generated in the temp folder: "yuval08_{id}_trace_{DateTime.Now:ddMMyyyyHHmmsss}.txt" 
    /// </remarks>
    /// <param name="id">ID of the tracer, will be used in the file name</param>
    /// <param name="level">Level of the tracer</param>
    /// <param name="useWindowsTempPath">By default the user temporary folder will be used, if true it will use the windows temporary folder</param>
    /// <example>
    ///   <para>In this example the dropping table comment will not be shown in the trace because the trace was initialized at level 1</para>
    ///   <code lang="C#"><![CDATA[var tracer = new Tracer(1);
    ///tracer.Write("Starting Installation Log");
    ///var tracerObject = tracer.CreateObject("InstallationTrace");
    ///tracerObject.Write("Connecting to SQL Server", 0);
    ///tracerObject.Write("Creating Database", 1);
    ///tracerObject.Write("Dropping Table [TEST_TABLE]", 2);
    ///]]></code>
    /// </example>
    public Tracer(string id, int level = 0, bool useWindowsTempPath = false) {
        Level = level;
        string tempPath = !useWindowsTempPath ? Path.GetTempPath() : Environment.ExpandEnvironmentVariables("%windir%\\temp");
        FileName = Path.Combine(tempPath, $"yuval08_{id}_trace_{DateTime.Now:ddMMyyyyHHmmsss}.txt");
        var stream   = new FileStream(FileName, FileMode.Create);
        var listener = new TextWriterTraceListener(stream);
        Trace.Listeners.Add(listener);
        Trace.AutoFlush = true;
        Trace.WriteLine("Trace Initialized...");
    }

    /// <summary>
    /// Close the tracer file
    /// </summary>
    public void Close() => Trace.Close();

    /// <summary>
    /// Write a message into the main trace
    /// </summary>
    /// <param name="message"></param>
    public void Write(string message) => Trace.WriteLine($"{DateTime.Now:dd-MM-yyyy-HH-mm-sss} - {message}");

    /// <summary>
    /// Create a new trace object
    /// </summary>
    /// <param name="id">ID of the trace object</param>
    public TraceObject CreateObject(string id) => new(this, id);
}