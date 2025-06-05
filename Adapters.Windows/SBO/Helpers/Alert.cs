// using System;
// using System.Collections.Generic;
// using System.Runtime.InteropServices;
// using SAPbobsCOM;
// using Service.API.Models;
// using Service.Shared.Company;
//
// namespace Service.API.General;
//
// public class Alert : IDisposable {
//     private readonly CompanyService    service;
//     private readonly MessagesService   messageService;
//     private readonly Message           message;
//     public           string            Subject { get; set; }
//     public           List<AlertColumn> Columns { get; } = new();
//
//     public Alert() {
//         service        = ConnectionController.Company.GetCompanyService();
//         messageService = (MessagesService)service.GetBusinessService(ServiceTypes.MessagesService);
//         message        = (Message)messageService.GetDataInterface(MessagesServiceDataInterfaces.msdiMessage);
//     }
//
//     public void Send(List<string> sendTo) {
//         if (sendTo.Count == 0)
//             return;
//         message.Subject = Subject;
//
//         if (Columns is { Count: > 0 }) {
//             var messageColumns = message.MessageDataColumns;
//             foreach (var column in Columns) {
//                 var messageColumn = messageColumns.Add();
//                 messageColumn.ColumnName = column.Name;
//                 messageColumn.Link       = column.Link ? BoYesNoEnum.tYES : BoYesNoEnum.tNO;
//
//                 var dataLines = messageColumn.MessageDataLines;
//                 column.Values.ForEach(value => {
//                     var dataLine = dataLines.Add();
//                     dataLine.Value = value.Value;
//                     if (!column.Link)
//                         return;
//                     dataLine.Object    = value.Object;
//                     dataLine.ObjectKey = value.ObjectKey;
//                 });
//             }
//         }
//
//
//         var recipientCollection = message.RecipientCollection;
//         sendTo.ForEach(user => {
//             recipientCollection.Add();
//             recipientCollection.Item(0).SendInternal = BoYesNoEnum.tYES;
//             recipientCollection.Item(0).UserCode     = user;
//         });
//
//         messageService.SendMessage(message);
//     }
//
//     public void Dispose() {
//         Marshal.ReleaseComObject(message);
//         Marshal.ReleaseComObject(messageService);
//         Marshal.ReleaseComObject(service);
//         GC.Collect();
//     }
// }