using System.Diagnostics;

namespace SSP_Lab5;

// ЛАБА 5.2 — Задание 3: PLINQ анализ данных
public static class Task2_3
{
    public static void Run()
    {
        Console.WriteLine("=== Задание 2.3: PLINQ анализ данных ===\n");

        const int N = 10_000_000;
        Random rand = new Random();
        var data = Enumerable.Range(0, N)
            .Select(i => rand.NextDouble() * 1000)
            .ToList();

        // --- Последовательный LINQ ---
        var sw = Stopwatch.StartNew();
        var seqResult = data
            .Where(x => x > 500)
            .Select(x => Math.Sqrt(x))
            .Average();
        sw.Stop();
        Console.WriteLine($"Последовательный LINQ:        {sw.ElapsedMilliseconds} мс, Результат: {seqResult:F4}");

        // --- PLINQ по умолчанию ---
        sw.Restart();
        var plinqResult = data
            .AsParallel()
            .Where(x => x > 500)
            .Select(x => Math.Sqrt(x))
            .Average();
        sw.Stop();
        Console.WriteLine($"PLINQ (по умолчанию):         {sw.ElapsedMilliseconds} мс, Результат: {plinqResult:F4}");

        // --- PLINQ со степенью параллелизма ---
        sw.Restart();
        var plinqResult2 = data
            .AsParallel()
            .WithDegreeOfParallelism(4)
            .Where(x => x > 500)
            .Select(x => Math.Sqrt(x))
            .Average();
        sw.Stop();
        Console.WriteLine($"PLINQ (степень=4):            {sw.ElapsedMilliseconds} мс, Результат: {plinqResult2:F4}");

        // --- PLINQ с сохранением порядка ---
        sw.Restart();
        var plinqOrdered = data
            .AsParallel()
            .AsOrdered()
            .Where(x => x > 500)
            .Select(x => x * 2)
            .Take(5)
            .ToList();
        sw.Stop();
        Console.WriteLine($"PLINQ (упорядоченный):        {sw.ElapsedMilliseconds} мс");
        Console.WriteLine($"Первые 5 результатов: {string.Join(", ", plinqOrdered.Select(x => x.ToString("F2")))}");
    }
}
