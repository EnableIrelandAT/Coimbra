// Licensed under the MIT License.

namespace Coimbra.Extensions
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;

    /// <summary>
    /// Provides extension methods for ObservableCollection.
    /// </summary>
    public static class ObservableCollectionExtensions
    {
        /// <summary>
        /// Sorts a value in.
        /// </summary>
        /// <typeparam name="T">Type of value.</typeparam>
        /// <param name="collection">Collection to sort value in to.</param>
        /// <param name="value">Value to sort in to collection.</param>
        /// <param name="position">Func determining position of value.</param>
        public static void SortIn<T>(this ObservableCollection<T> collection, T value, Func<T, int> position)
        {
            if (collection == null)
            {
                return;
            }

            if (collection.Count == 0)
            {
                collection.Add(value);
                return;
            }

            var indexLower = 0;
            for (var index = 0; index < collection.Count; index++)
            {
                if (position(collection[index]) < position(value))
                {
                    indexLower = index;
                }
                else
                {
                    break;
                }
            }

            collection.Insert(indexLower, value);
        }
    }
}
