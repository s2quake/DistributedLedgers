// using System.Collections;

// namespace DistributedLedgers.ConsoleHost;

// abstract class NodeCollection<T> : IEnumerable<T>, IBroadcastService where T : NodeBase
// {
//     private readonly List<T> _nodeList;

//     public NodeCollection(int length)
//     {
//         _nodeList = new List<T>(length);
//         for (var i = 0; i < length; i++)
//         {
//             _nodeList.Add(CreateInstanceInternal());
//         }
//     }

//     public int Count => _nodeList.Count;

//     public T this[int index] => _nodeList[index];

//     public bool Contains(T item) => _nodeList.Contains(item);

//     public int IndexOf(T item) => _nodeList.IndexOf(item);

//     protected abstract T CreateInstance();

//     private T CreateInstanceInternal()
//     {
//         var node = CreateInstance();
//         node.BroadcastService = this;
//         return node;
//     }

//     #region IEnumerable

//     IEnumerator<T> IEnumerable<T>.GetEnumerator() => _nodeList.GetEnumerator();

//     IEnumerator IEnumerable.GetEnumerator() => _nodeList.GetEnumerator();

//     #endregion

//     #region IBroadcastService

//     async void IBroadcastService.Broadcast(object sender, int type, params object[] args)
//     {
//         var query = from node in _nodeList
//                     where Equals(node, sender) != true
//                     select node;
//         var nodes = query.ToArray();
//         await Task.WhenAll(nodes.Select(item => ReceiveMessage(item, type, args)));

//         async Task ReceiveMessage(T node, int type, object[] args)
//         {
//             try
//             {
//                 await Task.Run(() => node.ReceiveMessage(type, args));
//             }
//             catch
//             {
//             }
//         }
//     }

//     #endregion
// }
