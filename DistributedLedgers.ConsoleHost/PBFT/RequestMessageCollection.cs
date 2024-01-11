using System.Collections;

namespace DistributedLedgers.ConsoleHost.PBFT;

sealed class RequestMessageCollection : IEnumerable<RequestMessage>
{
    private readonly List<RequestMessage> _itemList = [];

    public int Count => _itemList.Count;

    public void Add(int r, int c, int s)
    {
        lock (_itemList)
        {
            _itemList.Add(new(R: r, C: c, S: s));
        }
    }

    public RequestMessage[] Remove(int s)
    {
        lock (_itemList)
        {
            var itemList = new List<RequestMessage>(_itemList.Count);
            for (var i = _itemList.Count - 1; i >= 0; i--)
            {
                var item = _itemList[i];
                if (item.S <= s)
                {
                    _itemList.RemoveAt(i);
                    itemList.Add(item);
                }
            }
            return [.. itemList.OrderBy(item => item.S)];
        }
    }

    #region IEnumerable

    IEnumerator<RequestMessage> IEnumerable<RequestMessage>.GetEnumerator() => _itemList.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _itemList.GetEnumerator();

    #endregion
}