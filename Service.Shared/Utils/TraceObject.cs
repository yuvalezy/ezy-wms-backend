using System;
using System.Diagnostics;

namespace Service.Shared.Utils;

public class TraceObject : IDisposable {
    private readonly Tracer tracer;
    private readonly string            id;

    internal TraceObject(Tracer tracer, string id) {
        this.tracer = tracer;
        this.id     = id;
    }

    /// <summary>
    /// Write a message into the trace object
    /// </summary>
    /// <param name="message">Message to be written</param>
    /// <param name="level">Level of the message</param>
    public void Write(string message, int level = 0) {
        if (level <= tracer.Level)
            Trace.WriteLine($"{DateTime.Now:dd-MM-yyyy HH:mm:ss.ffffff} - {id} - {message}");
    }

    /// <summary>
    /// Dispose the trace object
    /// </summary>
    public void Dispose() => GC.SuppressFinalize(this);
}