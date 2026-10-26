using System.Diagnostics;

namespace SSP_Lab5;

// ЛАБА 5.2 — Задание 1: Параллельные циклы (Parallel.For)
public static class Task2_1
{
    public static void Run()
    {
        Console.WriteLine("=== Задание 2.1: Параллельные циклы ===\n");

        const int N = 10_000_000;
        double[] data = new double[N];
        Random rand = new Random();

        for (int i = 0; i < N; i++)
            data[i] = rand.NextDouble() * 1000;

        object lockObj = new object();

        // --- Последовательный цикл ---
        var sw = Stopwatch.StartNew();
        double sumSeq = 0;
        for (int i = 0; i < N; i++)
            sumSeq += Math.Sqrt(Math.Log(data[i] + 1));
        sw.Stop();
        Console.WriteLine($"Последовательно:                    {sw.ElapsedMilliseconds} мс, Результат: {sumSeq:F2}");

        // --- Параллельный цикл с блокировкой ---
        sw.Restart();
        double sumPar = 0;
        Parallel.For(0, N, i =>
        {
            double value = Math.Sqrt(Math.Log(data[i] + 1));
            lock (lockObj) { sumPar += value; }
        });
        sw.Stop();
        Console.WriteLine($"Параллельно (с блокировкой):        {sw.ElapsedMilliseconds} мс, Результат: {sumPar:F2}");

        // --- Параллельный цикл с локальными переменными ---
        sw.Restart();
        double sumParLocal = 0;
        Parallel.For(0, N,
            () => 0.0,
            (i, state, local) => local + Math.Sqrt(Math.Log(data[i] + 1)),
            local => { lock (lockObj) { sumParLocal += local; } }
        );
        sw.Stop();
        Console.WriteLine($"Параллельно (локальные переменные): {sw.ElapsedMilliseconds} мс, Результат: {sumParLocal:F2}");

        Console.WriteLine("\nПочему с блокировкой медленнее:");
        Console.WriteLine("  - lock вызывается 10 млн раз — огромные накладные расходы");
        Console.WriteLine("  - Все потоки конкурируют за один lockObj");
        Console.WriteLine("  - Локальные переменные: каждый поток считает свою сумму,");
        Console.WriteLine("    блокировка нужна только 1 раз в конце для агрегации");
    }
}
