using System.Collections;

namespace DistributedLedgers.ConsoleHost.PBFT;

sealed class CommitMessageCollection : IEnumerable<CommitMessage>
{
    private readonly List<CommitMessage> _itemList = [];

    public int Count => _itemList.Count;

    public void Add(int s)
    {
        _itemList.Add(new(S: s));
    }

    public bool CanReply(int s, int minimum)
    {
        _itemList.Add(new(S: s));
        if (_itemList.Where(Compare).Count() >= minimum)
        {
            // _itemList.RemoveAll(Compare);
            return true;
        }
        return false;

        bool Compare(CommitMessage item) => item.S == s;
    }

    #region IEnumerable

    IEnumerator<CommitMessage> IEnumerable<CommitMessage>.GetEnumerator() => _itemList.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _itemList.GetEnumerator();

    #endregion
}