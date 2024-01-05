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

    public static async Task<AsyncDisposableCollection<T>> CreateAsync(Task<T>[] tasks)
    {
        var collection = new AsyncDisposableCollection<T>(tasks.Length);
        await Task.WhenAll(tasks);
        foreach (var item in tasks)
        {
            collection.Add(item.Result);
        }
        return collection;
    }

    public void Add(T item)
    {
        _itemList.Add(item);
    }

    public bool Contains(T item) => _itemList.Contains(item);

    public int IndexOf(T item) => _itemList.IndexOf(item);

    public async ValueTask DisposeAsync()
    {
        await Parallel.ForEachAsync(_itemList, (item, _) => item.DisposeAsync());
    }

    #region IEnumerable

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _itemList.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _itemList.GetEnumerator();

    #endregion
}
