// using System.Collections;

// namespace DistributedLedgers.ConsoleHost.PBFT;

// sealed class PrePrepareMessageCollection : IEnumerable<PrePrepareMessage>
// {
//     private readonly List<PrePrepareMessage> _itemList = [];

//     public int Count => _itemList.Count;

//     public bool Add(int s, int r)
//     {
//         if (_itemList.Any(Compare) == false)
//         {
//             _itemList.Add(new(S: s, R: r));
//             return true;
//         }
//         return false;

//         bool Compare(PrePrepareMessage item)
//         {
//             return item.S == s;
//         }
//     }

//     public (int s, int r)[] Collect()
//     {
//         return [.. _itemList.Select(item => (item.S, item.R))];
//     }

//     public void AddRange((int r, int s)[] Pb)
//     {
//         for (var i = 0; i < Pb.Length; i++)
//         {
//             var (r, s) = Pb[i];
//             _itemList.Add(new(S: s, R: r));
//         }
//     }

//     public int GetMaximumS()
//     {
//         return _itemList.Max(item => item.S);
//     }

//     // public PrePrepareMessage[] Prepare(int s)
//     // {
//     //     // lock (_itemList)
//     //     {
//     //         var itemList = new List<PrePrepareMessage>(_itemList.Count);
//     //         for (var i = _itemList.Count - 1; i >= 0; i--)
//     //         {
//     //             var item = _itemList[i];
//     //             if (item.S <= s)
//     //             {
//     //                 _itemList.RemoveAt(i);
//     //                 itemList.Add(item);
//     //             }
//     //         }
//     //         return [.. itemList.OrderBy(item => item.S)];
//     //     }
//     // }

//     #region IEnumerable

//     IEnumerator<PrePrepareMessage> IEnumerable<PrePrepareMessage>.GetEnumerator() => _itemList.GetEnumerator();

//     IEnumerator IEnumerable.GetEnumerator() => _itemList.GetEnumerator();

//     #endregion
// }