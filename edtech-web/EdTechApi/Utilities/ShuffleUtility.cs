using System;
using System.Collections.Generic;

namespace EdTechApi.Utilities;

public static class ShuffleUtility
{
    private static readonly ThreadLocal<Random> _random = new(() => new Random());

    public static List<T> FisherYatesShuffle<T>(List<T> list)
    {
        var shuffled = new List<T>(list);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            var j = _random.Value!.Next(i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }
        return shuffled;
    }
}