using System.Collections;

namespace DistributedLedgers.ConsoleHost.PBFT;

sealed class PrePrepareMessageCollection : IEnumerable<PrePrepareMessage>
{
    private readonly List<PrePrepareMessage> _itemList = [];

    public int Count => _itemList.Count;

    public bool Add(int v, int s, int r, int p)
    {
        if (_itemList.Any(Compare) == false)
        {
            _itemList.Add(new(V: v, S: s, R: r, P: p));
            return true;
        }
        return false;

        bool Compare(PrePrepareMessage item)
        {
            return item.V == v && item.S == s;
        }
    }

    // public PrePrepareMessage[] Prepare(int s)
    // {
    //     // lock (_itemList)
    //     {
    //         var itemList = new List<PrePrepareMessage>(_itemList.Count);
    //         for (var i = _itemList.Count - 1; i >= 0; i--)
    //         {
    //             var item = _itemList[i];
    //             if (item.S <= s)
    //             {
    //                 _itemList.RemoveAt(i);
    //                 itemList.Add(item);
    //             }
    //         }
    //         return [.. itemList.OrderBy(item => item.S)];
    //     }
    // }

    #region IEnumerable

    IEnumerator<PrePrepareMessage> IEnumerable<PrePrepareMessage>.GetEnumerator() => _itemList.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _itemList.GetEnumerator();

    #endregion
}