using System.Collections.Concurrent;
using System.Diagnostics;

namespace SSP_Lab5;

// Вариант 2: Параллельная фильтрация коллекций (PLINQ, пользовательские условия)
public static class Variant2
{
    record Product(string Name, string Category, double Price, int Stock, DateTime Added);

    public static void Run()
    {
        Console.WriteLine("=== ВАРИАНТ 2: Параллельная фильтрация коллекций ===\n");

        const int N = 5_000_000;
        string[] categories = { "Электроника", "Одежда", "Еда", "Книги", "Спорт", "Дом", "Авто" };
        Random rand = new Random(42);

        var products = Enumerable.Range(0, N).Select(i => new Product(
            $"Товар-{i}",
            categories[rand.Next(categories.Length)],
            Math.Round(rand.NextDouble() * 10000, 2),
            rand.Next(0, 500),
            DateTime.Now.AddDays(-rand.Next(0, 365))
        )).ToList();

        // --- 1. Последовательная фильтрация (LINQ) ---
        var sw = Stopwatch.StartNew();
        var seqResult = products
            .Where(p => p.Category == "Электроника" && p.Price > 5000 && p.Stock > 0)
            .OrderByDescending(p => p.Price)
            .ToList();
        sw.Stop();
        Console.WriteLine($"LINQ фильтрация:           {sw.ElapsedMilliseconds} мс, Найдено: {seqResult.Count}");

        // --- 2. Параллельная фильтрация (PLINQ) ---
        sw.Restart();
        var parResult = products
            .AsParallel()
            .Where(p => p.Category == "Электроника" && p.Price > 5000 && p.Stock > 0)
            .OrderByDescending(p => p.Price)
            .ToList();
        sw.Stop();
        Console.WriteLine($"PLINQ фильтрация:          {sw.ElapsedMilliseconds} мс, Найдено: {parResult.Count}");

        // --- 3. Многоусловная фильтрация ---
        Console.WriteLine("\n--- Многоусловная фильтрация ---");
        sw.Restart();
        var complex = products
            .AsParallel()
            .Where(p => p.Price >= 1000 && p.Price <= 3000)
            .Where(p => p.Stock > 100)
            .Where(p => p.Added > DateTime.Now.AddDays(-180))
            .GroupBy(p => p.Category)
            .Select(g => new { Category = g.Key, Count = g.Count(), AvgPrice = g.Average(p => p.Price) })
            .OrderByDescending(g => g.Count)
            .ToList();
        sw.Stop();
        Console.WriteLine($"Время: {sw.ElapsedMilliseconds} мс");
        foreach (var g in complex)
            Console.WriteLine($"  {g.Category}: {g.Count} товаров, средняя цена: {g.AvgPrice:F2}");

        // --- 4. Фильтрация с Parallel.ForEach + локальные списки ---
        Console.WriteLine("\n--- Parallel.ForEach с локальными списками ---");
        var filtered = new ConcurrentBag<Product>();
        sw.Restart();
        Parallel.ForEach(products,
            () => new List<Product>(),
            (p, state, local) =>
            {
                if (p.Price > 8000 && p.Stock < 10)
                    local.Add(p);
                return local;
            },
            local =>
            {
                foreach (var p in local)
                    filtered.Add(p);
            }
        );
        sw.Stop();
        Console.WriteLine($"Время: {sw.ElapsedMilliseconds} мс, Дорогие с малым остатком: {filtered.Count}");

        // --- 5. Сравнение степеней параллелизма ---
        Console.WriteLine("\n--- Влияние степени параллелизма ---");
        foreach (int degree in new[] { 1, 2, 4, 8 })
        {
            sw.Restart();
            var res = products
                .AsParallel()
                .WithDegreeOfParallelism(degree)
                .Where(p => p.Category == "Книги" && p.Price < 500)
                .Select(p => p.Price * 1.2)
                .ToList();
            sw.Stop();
            Console.WriteLine($"  Степень {degree}: {sw.ElapsedMilliseconds} мс, Результатов: {res.Count}");
        }
    }
}
