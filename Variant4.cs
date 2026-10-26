using System.Collections.Concurrent;
using System.Diagnostics;

namespace SSP_Lab5;

// Вариант 4: Обработка изображений (фильтры, преобразования)
public static class Variant4
{
    static double ProcessPixel(double pixel)
    {
        return Math.Sqrt(Math.Abs(Math.Sin(pixel) * Math.Cos(pixel) * 1000));
    }

    public static void Run()
    {
        Console.WriteLine("=== ВАРИАНТ 4: Обработка изображений ===\n");

        const int width = 2000;
        const int height = 2000;
        double[,] image = new double[height, width];
        double[,] result = new double[height, width];
        Random rand = new Random();

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                image[y, x] = rand.NextDouble();

        // --- Последовательная обработка ---
        var sw = Stopwatch.StartNew();
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                result[y, x] = ProcessPixel(image[y, x]);
        sw.Stop();
        long seqMs = sw.ElapsedMilliseconds;
        Console.WriteLine($"Последовательно:               {seqMs} мс");

        // --- Параллельная по строкам ---
        sw.Restart();
        Parallel.For(0, height, y =>
        {
            for (int x = 0; x < width; x++)
                result[y, x] = ProcessPixel(image[y, x]);
        });
        sw.Stop();
        long parMs = sw.ElapsedMilliseconds;
        Console.WriteLine($"Параллельно (по строкам):      {parMs} мс  <- в {(double)seqMs / parMs:F1}x быстрее");

        // --- Параллельная с разделением ---
        sw.Restart();
        var partitioner = Partitioner.Create(0, height, 100);
        Parallel.ForEach(partitioner, range =>
        {
            for (int y = range.Item1; y < range.Item2; y++)
                for (int x = 0; x < width; x++)
                    result[y, x] = ProcessPixel(image[y, x]);
        });
        sw.Stop();
        long partMs = sw.ElapsedMilliseconds;
        Console.WriteLine($"Параллельно (с разделением):   {partMs} мс  <- в {(double)seqMs / partMs:F1}x быстрее");

        // --- PLINQ ---
        sw.Restart();
        var pixels = Enumerable.Range(0, height * width)
            .AsParallel()
            .Select(i => ProcessPixel(image[i / width, i % width]))
            .ToArray();
        sw.Stop();
        long plinqMs = sw.ElapsedMilliseconds;
        Console.WriteLine($"PLINQ:                         {plinqMs} мс  <- в {(double)seqMs / plinqMs:F1}x быстрее");

        Console.WriteLine($"\nИтого пикселей обработано: {width * height:N0}");
        Console.WriteLine("Параллельная обработка по строкам — оптимальный вариант для изображений,");
        Console.WriteLine("так как строки независимы и данные расположены последовательно в памяти.");
    }
}
