using System.Collections;

namespace DistributedLedgers.ConsoleHost.PBFT;

sealed class CommitMessageCollection : IEnumerable<CommitMessage>
{
    private readonly List<CommitMessage> _itemList = [];

    public int Count => _itemList.Count;

    public void Add(int v, int s, int ni)
    {
        lock (_itemList)
        {
            _itemList.Add(new(V: v, S: s, Ni: ni));
        }
    }

    public bool CanReply(int v, int s, int ni, int minimum)
    {
        lock (_itemList)
        {
            _itemList.Add(new(V: v, S: s, Ni: ni));
            if (_itemList.Where(Compare).Count() >= minimum)
            {
                _itemList.RemoveAll(Compare);
                return true;
            }
            return false;
        }

        bool Compare(CommitMessage item) => item.V == v && item.S == s;
    }

    #region IEnumerable

    IEnumerator<CommitMessage> IEnumerable<CommitMessage>.GetEnumerator() => _itemList.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _itemList.GetEnumerator();

    #endregion
}