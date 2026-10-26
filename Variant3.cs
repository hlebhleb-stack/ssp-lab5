using System.Collections.Concurrent;
using System.Diagnostics;

namespace SSP_Lab5;

// Вариант 3: Матричные вычисления (умножение, транспонирование)
public static class Variant3
{
    public static void Run()
    {
        Console.WriteLine("=== ВАРИАНТ 3: Матричные вычисления ===\n");

        const int SIZE = 500;
        double[,] A = new double[SIZE, SIZE];
        double[,] B = new double[SIZE, SIZE];
        Random rand = new Random(42);

        for (int i = 0; i < SIZE; i++)
            for (int j = 0; j < SIZE; j++)
            {
                A[i, j] = rand.NextDouble() * 10;
                B[i, j] = rand.NextDouble() * 10;
            }

        // --- 1. Умножение матриц: последовательно ---
        Console.WriteLine($"Размер матриц: {SIZE}x{SIZE}\n");
        Console.WriteLine("--- Умножение матриц ---");

        var sw = Stopwatch.StartNew();
        double[,] resultSeq = new double[SIZE, SIZE];
        for (int i = 0; i < SIZE; i++)
            for (int j = 0; j < SIZE; j++)
            {
                double sum = 0;
                for (int k = 0; k < SIZE; k++)
                    sum += A[i, k] * B[k, j];
                resultSeq[i, j] = sum;
            }
        sw.Stop();
        Console.WriteLine($"Последовательно:           {sw.ElapsedMilliseconds} мс");

        // --- 2. Умножение: параллельно (Parallel.For по строкам) ---
        sw.Restart();
        double[,] resultPar = new double[SIZE, SIZE];
        Parallel.For(0, SIZE, i =>
        {
            for (int j = 0; j < SIZE; j++)
            {
                double sum = 0;
                for (int k = 0; k < SIZE; k++)
                    sum += A[i, k] * B[k, j];
                resultPar[i, j] = sum;
            }
        });
        sw.Stop();
        Console.WriteLine($"Параллельно (по строкам):  {sw.ElapsedMilliseconds} мс");

        // --- 3. Умножение: с Partitioner ---
        sw.Restart();
        double[,] resultPart = new double[SIZE, SIZE];
        var partitioner = Partitioner.Create(0, SIZE, SIZE / Environment.ProcessorCount + 1);
        Parallel.ForEach(partitioner, range =>
        {
            for (int i = range.Item1; i < range.Item2; i++)
                for (int j = 0; j < SIZE; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < SIZE; k++)
                        sum += A[i, k] * B[k, j];
                    resultPart[i, j] = sum;
                }
        });
        sw.Stop();
        Console.WriteLine($"Параллельно (Partitioner): {sw.ElapsedMilliseconds} мс");

        // Проверка корректности
        bool correct = true;
        for (int i = 0; i < SIZE && correct; i++)
            for (int j = 0; j < SIZE && correct; j++)
                if (Math.Abs(resultSeq[i, j] - resultPar[i, j]) > 1e-6)
                    correct = false;
        Console.WriteLine($"Результаты совпадают: {(correct ? "Да" : "Нет")}");

        // --- 4. Транспонирование: последовательно ---
        Console.WriteLine("\n--- Транспонирование матрицы ---");
        sw.Restart();
        double[,] transSeq = new double[SIZE, SIZE];
        for (int i = 0; i < SIZE; i++)
            for (int j = 0; j < SIZE; j++)
                transSeq[j, i] = A[i, j];
        sw.Stop();
        Console.WriteLine($"Последовательно:           {sw.ElapsedMilliseconds} мс");

        // --- 5. Транспонирование: параллельно ---
        sw.Restart();
        double[,] transPar = new double[SIZE, SIZE];
        Parallel.For(0, SIZE, i =>
        {
            for (int j = 0; j < SIZE; j++)
                transPar[j, i] = A[i, j];
        });
        sw.Stop();
        Console.WriteLine($"Параллельно:               {sw.ElapsedMilliseconds} мс");

        // --- 6. Сумма элементов матрицы: PLINQ ---
        Console.WriteLine("\n--- Сумма элементов (PLINQ) ---");
        sw.Restart();
        double matSum = Enumerable.Range(0, SIZE * SIZE)
            .AsParallel()
            .Sum(idx => A[idx / SIZE, idx % SIZE]);
        sw.Stop();
        Console.WriteLine($"Сумма элементов A: {matSum:F2}, Время: {sw.ElapsedMilliseconds} мс");
    }
}
