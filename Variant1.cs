using System.Diagnostics;

namespace SSP_Lab5;

// Вариант 1: Обработка больших массивов данных (суммирование, поиск максимума)
public static class Variant1
{
    public static void Run()
    {
        Console.WriteLine("=== ВАРИАНТ 1: Обработка больших массивов данных ===\n");

        const int N = 20_000_000;
        double[] data = new double[N];
        Random rand = new Random(42);
        for (int i = 0; i < N; i++)
            data[i] = rand.NextDouble() * 10000 - 5000;

        // --- 1. Суммирование: последовательно ---
        var sw = Stopwatch.StartNew();
        double sumSeq = 0;
        for (int i = 0; i < N; i++)
            sumSeq += data[i];
        sw.Stop();
        Console.WriteLine($"Сумма (последовательно):   {sw.ElapsedMilliseconds} мс, Результат: {sumSeq:F2}");

        // --- 2. Суммирование: параллельно (локальные переменные) ---
        sw.Restart();
        double sumPar = 0;
        object lockObj = new object();
        Parallel.For(0, N,
            () => 0.0,
            (i, state, local) => local + data[i],
            local => { lock (lockObj) { sumPar += local; } }
        );
        sw.Stop();
        Console.WriteLine($"Сумма (параллельно):       {sw.ElapsedMilliseconds} мс, Результат: {sumPar:F2}");

        // --- 3. Суммирование: PLINQ ---
        sw.Restart();
        double sumPlinq = data.AsParallel().Sum();
        sw.Stop();
        Console.WriteLine($"Сумма (PLINQ):             {sw.ElapsedMilliseconds} мс, Результат: {sumPlinq:F2}");

        Console.WriteLine();

        // --- 4. Поиск максимума: последовательно ---
        sw.Restart();
        double maxSeq = double.MinValue;
        int maxIdxSeq = 0;
        for (int i = 0; i < N; i++)
        {
            if (data[i] > maxSeq)
            {
                maxSeq = data[i];
                maxIdxSeq = i;
            }
        }
        sw.Stop();
        Console.WriteLine($"Макс (последовательно):    {sw.ElapsedMilliseconds} мс, Значение: {maxSeq:F2} (индекс {maxIdxSeq})");

        // --- 5. Поиск максимума: параллельно (Partitioner) ---
        sw.Restart();
        double maxPar = double.MinValue;
        int maxIdxPar = 0;
        var partitioner = System.Collections.Concurrent.Partitioner.Create(0, N, N / Environment.ProcessorCount);
        Parallel.ForEach(partitioner, () => (Val: double.MinValue, Idx: 0),
            (range, state, local) =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    if (data[i] > local.Val)
                        local = (data[i], i);
                }
                return local;
            },
            local =>
            {
                lock (lockObj)
                {
                    if (local.Val > maxPar)
                    {
                        maxPar = local.Val;
                        maxIdxPar = local.Idx;
                    }
                }
            }
        );
        sw.Stop();
        Console.WriteLine($"Макс (параллельно):        {sw.ElapsedMilliseconds} мс, Значение: {maxPar:F2} (индекс {maxIdxPar})");

        // --- 6. Поиск максимума: PLINQ ---
        sw.Restart();
        double maxPlinq = data.AsParallel().Max();
        sw.Stop();
        Console.WriteLine($"Макс (PLINQ):              {sw.ElapsedMilliseconds} мс, Значение: {maxPlinq:F2}");

        // --- 7. Поиск минимума + среднее: PLINQ ---
        sw.Restart();
        var stats = data.AsParallel().Aggregate(
            () => (Min: double.MaxValue, Max: double.MinValue, Sum: 0.0, Count: 0L),
            (local, val) => (
                Math.Min(local.Min, val),
                Math.Max(local.Max, val),
                local.Sum + val,
                local.Count + 1
            ),
            (a, b) => (
                Math.Min(a.Min, b.Min),
                Math.Max(a.Max, b.Max),
                a.Sum + b.Sum,
                a.Count + b.Count
            ),
            final => final
        );
        sw.Stop();
        Console.WriteLine($"\nПараллельная агрегация (мин/макс/среднее за 1 проход): {sw.ElapsedMilliseconds} мс");
        Console.WriteLine($"  Мин: {stats.Min:F2}, Макс: {stats.Max:F2}, Среднее: {stats.Sum / stats.Count:F2}");
    }
}
