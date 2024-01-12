using System.Collections;

namespace DistributedLedgers.ConsoleHost.PBFT;

sealed class PrepareMessageCollection : IEnumerable<PrepareMessage>
{
    private readonly List<PrepareMessage> _itemList = [];

    public int Count => _itemList.Count;

    public void Add(int v, int s, int r)
    {
        _itemList.Add(new(V: v, S: s, R: r));
    }

    public bool CanCommit(int v, int s, int r, int minimum)
    {
        _itemList.Add(new(V: v, S: s, R: r));
        if (_itemList.Where(Compare).Count() >= minimum)
        {
            // _itemList.RemoveAll(Compare);
            return true;
        }
        return false;

        bool Compare(PrepareMessage item) => item.V == v && item.S == s && item.R == r;
    }

    public (int r, int s)[] Collect()
    {
        return [.. _itemList.Select(item => (item.R, item.S))];
    }

    public void AddRange(int v, (int r, int s)[] Pb)
    {
        for (var i = 0; i < Pb.Length; i++)
        {
            var (r, s) = Pb[i];
            _itemList.Add(new(V: v, S: s, R: r));
        }
    }

    #region IEnumerable

    IEnumerator<PrepareMessage> IEnumerable<PrepareMessage>.GetEnumerator() => _itemList.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _itemList.GetEnumerator();

    #endregion
}