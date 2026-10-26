using System.Collections.Concurrent;
using System.Diagnostics;

namespace SSP_Lab5;

// ЛАБА 5.2 — Задание 2: Parallel.ForEach с коллекциями
public static class Task2_2
{
    public static void Run()
    {
        Console.WriteLine("=== Задание 2.2: Parallel.ForEach ===\n");

        const int N = 10_000_000;
        int[] numbers = new int[N];
        Random rand = new Random();
        for (int i = 0; i < N; i++)
            numbers[i] = rand.Next(1, 1000);

        object lockObj = new object();

        // --- 1. Трансформация (Parallel.For) ---
        Console.WriteLine("--- Трансформация данных ---");
        double[] transformed = new double[N];
        var sw = Stopwatch.StartNew();
        Parallel.For(0, N, i => transformed[i] = Math.Sqrt(Math.Pow(numbers[i], 2) + 1));
        sw.Stop();
        Console.WriteLine($"Время: {sw.ElapsedMilliseconds} мс");
        Console.WriteLine($"Пример: {transformed[0]:F2}, {transformed[1000]:F2}, {transformed[5000]:F2}\n");

        // --- 2. Фильтрация (>500 и кратные 7) ---
        Console.WriteLine("--- Фильтрация (>500 и кратные 7) ---");
        var filtered = new ConcurrentBag<int>();
        sw.Restart();
        Parallel.ForEach(numbers, n =>
        {
            if (n > 500 && n % 7 == 0)
                filtered.Add(n);
        });
        sw.Stop();
        Console.WriteLine($"Найдено: {filtered.Count}, Время: {sw.ElapsedMilliseconds} мс");
        Console.WriteLine($"Примеры: {string.Join(", ", filtered.Take(5))}\n");

        // --- 3. Поиск с отменой (CancellationToken + state.Break) ---
        Console.WriteLine("--- Поиск с отменой ---");
        int[] targets = { 777, 999, 123 };
        bool[] found = new bool[targets.Length];
        int[] positions = new int[targets.Length];
        var cts = new CancellationTokenSource();

        sw.Restart();
        try
        {
            Parallel.For(0, N, new ParallelOptions { CancellationToken = cts.Token }, (i, state) =>
            {
                for (int j = 0; j < targets.Length; j++)
                {
                    if (!found[j] && numbers[i] == targets[j])
                    {
                        lock (found)
                        {
                            if (!found[j])
                            {
                                found[j] = true;
                                positions[j] = i;
                                Console.WriteLine($"  ✅ Найдено {targets[j]} на позиции {i}");
                                if (found.All(f => f))
                                {
                                    cts.Cancel();
                                    state.Break();
                                    return;
                                }
                            }
                        }
                    }
                }
            });
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("✅ Все значения найдены, поиск отменён!");
        }
        sw.Stop();
        Console.WriteLine($"Время поиска: {sw.ElapsedMilliseconds} мс\n");

        // --- 4. Сравнение: блокировка vs локальные переменные ---
        Console.WriteLine("--- Сравнение: блокировка vs локальные переменные ---");
        long sumLock = 0;
        sw.Restart();
        Parallel.For(0, N, i => { lock (lockObj) sumLock += numbers[i] * numbers[i]; });
        sw.Stop();
        Console.WriteLine($"С блокировкой:        {sw.ElapsedMilliseconds} мс");

        long sumLocal = 0;
        sw.Restart();
        Parallel.For(0, N,
            () => 0L,
            (i, state, local) => local + numbers[i] * numbers[i],
            local => Interlocked.Add(ref sumLocal, local)
        );
        sw.Stop();
        Console.WriteLine($"С локальными:         {sw.ElapsedMilliseconds} мс  <- быстрее!");
        Console.WriteLine($"Результаты: {sumLock:N0} = {sumLocal:N0}");
    }
}
