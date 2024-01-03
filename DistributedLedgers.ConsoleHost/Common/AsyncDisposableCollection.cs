using System.Collections;

namespace DistributedLedgers.ConsoleHost;

sealed class AsyncDisposableCollection<T> : IEnumerable<T>, IAsyncDisposable where T : IAsyncDisposable
{
    private readonly List<T> _itemList;

    public AsyncDisposableCollection()
    {
        _itemList = [];
    }

    public AsyncDisposableCollection(int capacity)
    {
        _itemList = new List<T>(capacity);
    }

    public int Count => _itemList.Count;

    public T this[int index] => _itemList[index];

    public void Add(T item)
    {
        _itemList.Add(item);
    }

    public bool Contains(T item) => _itemList.Contains(item);

    public int IndexOf(T item) => _itemList.IndexOf(item);

    #region IEnumerable

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _itemList.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _itemList.GetEnumerator();

    #endregion

    #region IAsyncDisposable

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        foreach (var item in _itemList)
        {
            await item.DisposeAsync();
        }
    }

    #endregion
}
