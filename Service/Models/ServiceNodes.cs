// using System.Collections.Generic;
// using System.Linq;
// using System.Threading;
// using System.Threading.Tasks;
//
// namespace Service.Models;
//
// public class ServiceNodes : List<ServiceNode> {
//     private readonly CancellationTokenSource token = new();
//
//     private int currentNodeIndex;
//
//     public ServiceNode NextNode {
//         get {
//             var next = this[currentNodeIndex++];
//             if (currentNodeIndex == Count)
//                 currentNodeIndex = 0;
//             return next.NodeStatus == NodeStatus.Running ? next : NextNode;
//         }
//     }
//
//     public void StartRestartTimer() => Task.Delay(Global.RestAPISettings.NodesRestart * 60 * 1000, token.Token).ContinueWith(_ => RestartNodes());
//
//     private void RestartNodes() {
//         if (token.IsCancellationRequested)
//             return;
//         //duplicate current node list
//         var list = new List<ServiceNode>(this);
//         //run till I don't have any node left to restart
//         while (list.Count > 0) {
//             if (token.IsCancellationRequested)
//                 return;
//             //first select a node without any transactions, if non, then select the one with the lowest transactions
//             var node = list.FirstOrDefault(v => v.CurrentTransactions == 0) ?? list.OrderBy(v => v.CurrentTransactions).First();
//             //pause the node so no new transactions will be sent to it, we're going to pause one node at the time
//             //if new transactions come in, they will go to the node that's not paused.
//             node.Pause();
//             while (node.CurrentTransactions > 0) {
//                 //If transactions still running, wait 1 second and check if value is now at zero
//                 Task.Delay(1000).Wait();
//             }
//
//             //no transactions are running anymore, the node Windows Service can be restarted
//             node.Restart();
//
//             //done with this node
//             list.Remove(node);
//         }
//
//         if (token.IsCancellationRequested)
//             return;
//         StartRestartTimer();
//     }
//
//     public void StopRestartTimer() => token.Cancel();
// }