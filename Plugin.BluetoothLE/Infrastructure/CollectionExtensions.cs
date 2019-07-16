using System;
using System.Collections.Generic;
using System.Linq;


namespace Acr.Collections
{
    public static class EnumerableExtensions
    {
        public static bool IsEmpty<T>(this IEnumerable<T> en)
            => en == null || !en.Any();


        public static void Each<T>(this IEnumerable<T> en, Action<T> action)
        {
            if (en == null)
                return;

            foreach (var obj in en)
                action(obj);
        }


        public static void Each<T>(this IEnumerable<T> en, Action<int, T> action)
        {
            if (en == null)
                return;

            var i = 0;
            foreach (var obj in en)
            {
                action(i, obj);
                i++;
            }
        }
    }
}
