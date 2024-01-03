// using DistributedLedgers.ConsoleHost.Common;
// using JSSoft.Communication;

// namespace DistributedLedgers.ConsoleHost.Commands.Chapter1;

// partial class Algorithm_2_9Command
// {
//     sealed class Client : IAsyncDisposable
//     {
//         private SimpleClient[] _peers = [];

//         private Client()
//         {
//         }

//         public static async Task<Client> CreateAsync(int[] ports)
//         {
//             var peers = new SimpleClient[ports.Length];
//             for (var i = 0; i < ports.Length; i++)
//             {
//                 peers[i] = await SimpleClient.CreateAsync(ports[i], new ClientDataService());
//             }
//             return new Client
//             {
//                 _peers = peers
//             };
//         }

//         public async ValueTask DisposeAsync()
//         {
//             foreach (var item in _peers)
//             {
//                 await item.DisposeAsync();
//             }
//         }
//     }
// }
