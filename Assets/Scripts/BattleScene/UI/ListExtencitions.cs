using System.Collections.Generic;
using UnityEngine;

public static class ListExtencitions
{
    private static System.Random rng = new System.Random();

    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            // �v�f�̓���ւ�
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}
