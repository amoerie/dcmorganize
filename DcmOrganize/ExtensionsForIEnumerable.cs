using System.Collections.Generic;
using System.Threading.Tasks;

namespace DcmOrganize;

internal static class ExtensionsForIEnumerable
{
    public static async IAsyncEnumerable<T> AsAsyncEnumerable<T>(this IEnumerable<T> enumerable)
    {
        foreach (var item in enumerable)
        {
            await Task.Yield();
                
            yield return item;
        }
    }
}