using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DcmOrganize;

public static class ExtensionsForIAsyncEnumerable
{
    public static async ValueTask<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> asyncEnumerable, CancellationToken cancellationToken = default)
    {
        if (asyncEnumerable == null)
            throw new ArgumentNullException(nameof(asyncEnumerable));

        var list = new List<T>();

        await foreach (var item in asyncEnumerable.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            list.Add(item);
        }

        return list;
    }
}