using System.Collections.Concurrent;
using System.Diagnostics;

namespace SSP_Lab5;

// Вариант 6: Численные методы (интегрирование, оптимизация)
public static class Variant6
{
    static double F(double x) => Math.Sin(x) * Math.Exp(-x * x / 10);

    public static void Run()
    {
        Console.WriteLine("=== ВАРИАНТ 6: Численные методы ===\n");

        double a = 0, b = 10;
        const int N = 100_000_000;
        double h = (b - a) / N;

        // --- 1. Интегрирование методом трапеций: последовательно ---
        Console.WriteLine($"Интеграл sin(x)*exp(-x²/10) на [{a}, {b}], N={N:N0}\n");

        var sw = Stopwatch.StartNew();
        double sumSeq = (F(a) + F(b)) / 2;
        for (int i = 1; i < N; i++)
            sumSeq += F(a + i * h);
        sumSeq *= h;
        sw.Stop();
        Console.WriteLine($"Последовательно (трапеции): {sw.ElapsedMilliseconds} мс, Результат: {sumSeq:F10}");

        // --- 2. Параллельно (Parallel.For + локальные) ---
        sw.Restart();
        double sumPar = 0;
        object lockObj2 = new object();
        Parallel.For(1, N,
            () => 0.0,
            (i, state, local) => local + F(a + i * h),
            local => { lock (lockObj2) { sumPar += local; } }
        );
        sumPar = (sumPar + (F(a) + F(b)) / 2) * h;
        sw.Stop();
        Console.WriteLine($"Параллельно (Parallel.For): {sw.ElapsedMilliseconds} мс, Результат: {sumPar:F10}");

        // --- 3. Partitioner ---
        sw.Restart();
        double sumPart = 0;
        object lockObj = new object();
        var partitioner = Partitioner.Create(1, N, N / Environment.ProcessorCount + 1);
        Parallel.ForEach(partitioner, () => 0.0,
            (range, state, local) =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                    local += F(a + i * h);
                return local;
            },
            local => { lock (lockObj) { sumPart += local; } }
        );
        sumPart = (sumPart + (F(a) + F(b)) / 2) * h;
        sw.Stop();
        Console.WriteLine($"Параллельно (Partitioner):  {sw.ElapsedMilliseconds} мс, Результат: {sumPart:F10}");

        // --- 4. PLINQ ---
        sw.Restart();
        double sumPlinq = Enumerable.Range(1, N - 1)
            .AsParallel()
            .Sum(i => F(a + i * h));
        sumPlinq = (sumPlinq + (F(a) + F(b)) / 2) * h;
        sw.Stop();
        Console.WriteLine($"PLINQ:                      {sw.ElapsedMilliseconds} мс, Результат: {sumPlinq:F10}");

        // --- 5. Метод Монте-Карло: параллельная оценка числа Пи ---
        Console.WriteLine("\n--- Метод Монте-Карло: оценка числа Пи ---");
        const int Points = 50_000_000;

        sw.Restart();
        long insideSeq = 0;
        var rnd = new Random(42);
        for (int i = 0; i < Points; i++)
        {
            double x = rnd.NextDouble();
            double y = rnd.NextDouble();
            if (x * x + y * y <= 1) insideSeq++;
        }
        double piSeq = 4.0 * insideSeq / Points;
        sw.Stop();
        Console.WriteLine($"Последовательно:            {sw.ElapsedMilliseconds} мс, Pi ≈ {piSeq:F6}");

        sw.Restart();
        long insidePar = 0;
        Parallel.For(0, Environment.ProcessorCount, () => (Inside: 0L, Rnd: new Random(Thread.CurrentThread.ManagedThreadId)),
            (core, state, local) =>
            {
                int perCore = Points / Environment.ProcessorCount;
                for (int i = 0; i < perCore; i++)
                {
                    double x = local.Rnd.NextDouble();
                    double y = local.Rnd.NextDouble();
                    if (x * x + y * y <= 1) local.Inside++;
                }
                return local;
            },
            local => Interlocked.Add(ref insidePar, local.Inside)
        );
        double piPar = 4.0 * insidePar / Points;
        sw.Stop();
        Console.WriteLine($"Параллельно:                {sw.ElapsedMilliseconds} мс, Pi ≈ {piPar:F6}");
        Console.WriteLine($"Погрешность: {Math.Abs(piPar - Math.PI):E2}");
    }
}
