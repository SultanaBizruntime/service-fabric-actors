using System.Collections.Generic;
using System.Linq;

namespace WebService
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<IEnumerable<TItem>> Chunk<TItem>(this IEnumerable<TItem> enumerableToChunk, int countOfCollectionsToReturn)
        {
            long chunkSize = enumerableToChunk.LongCount() / countOfCollectionsToReturn;
            using (var enumerator = enumerableToChunk.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    yield return GetChunk(enumerator, chunkSize);
                }
            }
        }

        private static IEnumerable<TItem> GetChunk<TItem>(IEnumerator<TItem> enumerator, long chunkSize)
        {
            do
            {
                yield return enumerator.Current;
            } while (--chunkSize > 0 && enumerator.MoveNext());
        }
    }
}
