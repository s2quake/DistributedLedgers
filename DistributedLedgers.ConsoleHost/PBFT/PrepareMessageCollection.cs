using System.Collections;

namespace DistributedLedgers.ConsoleHost.PBFT;

sealed class PrepareMessageCollection : IEnumerable<PrepareMessage>
{
    private readonly List<PrepareMessage> _itemList = [];
    private int _s;

    public int Count => _itemList.Count;

    public void Add(int s, int r)
    {
        _itemList.Add(new(S: s, R: r));
    }

    public bool Contains(int s, int r)
    {
        return _itemList.Any(Compare) == true;

        bool Compare(PrepareMessage item) => item.S == s && item.R == r;
    }

    public bool CanCommit(int s, int r, int minimum)
    {
        if (_itemList.Where(Compare).Count() >= minimum)
        {
            return true;
        }
        return false;

        bool Compare(PrepareMessage item) => item.S == s && item.R == r;
    }

    public (int s, int r)[] Collect()
    {
        return [.. _itemList.Select(item => (item.S, item.R))];
    }

    public void AddRange((int s, int r)[] Pb)
    {
        for (var i = 0; i < Pb.Length; i++)
        {
            var (s, r) = Pb[i];
            _itemList.Add(new(S: s, R: r));
        }
    }

    public PrepareMessage[] Collect(int s)
    {
        if (s > _s)
        {
            var itemList = new List<PrepareMessage>(_itemList.Count);
            for (var i = _itemList.Count - 1; i >= 0; i--)
            {
                var item = _itemList[i];
                if (item.S > _s && item.S <= s && item.R != int.MinValue)
                {
                    itemList.Add(item);
                }
            }

            _s = s;
            return [.. itemList.Distinct().OrderBy(item => item.S)];
        }
        return [];
    }

    public void SetS(int s)
    {
        _s = s;
    }

    public int GetS() => _s;

    #region IEnumerable

    IEnumerator<PrepareMessage> IEnumerable<PrepareMessage>.GetEnumerator() => _itemList.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _itemList.GetEnumerator();

    #endregion
}