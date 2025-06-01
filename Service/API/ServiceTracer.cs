// using System;
// using Service.Shared.Utils;
//
// namespace Service.API; 
//
// public class ServiceTracer : IDisposable {
//     private readonly Tracer tracer;
//     public ServiceTracer(MethodType method, string id) => tracer = Global.GetTracer($"http_{method.ToString().ToLower()}_{id}");
//     public bool Enabled => tracer != null;
//
//     public void Write(string message) => tracer?.Write(message);
//
//     public void Dispose() => tracer?.Close();
//
//     public TraceObject CreateObject(string id) => tracer?.CreateObject(id);
// }
//
// public enum MethodType {
//     Get, Post
// }