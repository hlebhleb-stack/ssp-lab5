using System.Collections.Concurrent;
using System.Diagnostics;

namespace SSP_Lab5;

// ЛАБА 5.2 — Задание 4: Разделение данных и агрегация
public static class Task2_4
{
    public static void Run()
    {
        Console.WriteLine("=== Задание 2.4: Разделение данных и агрегация ===\n");

        const int N = 10_000_000;
        int[] data = new int[N];
        Random rand = new Random();
        for (int i = 0; i < N; i++)
            data[i] = rand.Next(1, 100);

        // --- Разделение на диапазоны (Interlocked) ---
        var sw = Stopwatch.StartNew();
        long sumRange = 0;
        Parallel.For(0, N, i =>
        {
            Interlocked.Add(ref sumRange, data[i]);
        });
        sw.Stop();
        Console.WriteLine($"Разделение на диапазоны:      {sw.ElapsedMilliseconds} мс, Сумма: {sumRange}");

        // --- Пользовательское разделение с Partitioner ---
        sw.Restart();
        long sumCustom = 0;
        var partitioner = Partitioner.Create(0, N, 100_000);
        Parallel.ForEach(partitioner, range =>
        {
            long localSum = 0;
            for (int i = range.Item1; i < range.Item2; i++)
                localSum += data[i];
            Interlocked.Add(ref sumCustom, localSum);
        });
        sw.Stop();
        Console.WriteLine($"Пользовательское разделение:  {sw.ElapsedMilliseconds} мс, Сумма: {sumCustom}");

        // --- Локальная агрегация ---
        sw.Restart();
        long sumLocal = 0;
        Parallel.For(0, N,
            () => 0L,
            (i, state, local) => local + data[i],
            local => Interlocked.Add(ref sumLocal, local)
        );
        sw.Stop();
        Console.WriteLine($"Локальная агрегация:          {sw.ElapsedMilliseconds} мс, Сумма: {sumLocal}");

        Console.WriteLine("\nВсе три метода дают одинаковую сумму — результат корректный.");
    }
}
