using System.Collections.Concurrent;
using System.Diagnostics;

namespace SSP_Lab5;

// Вариант 5: Анализ текстовых данных (параллельный поиск, подсчёт)
public static class Variant5
{
    public static void Run()
    {
        Console.WriteLine("=== ВАРИАНТ 5: Анализ текстовых данных ===\n");

        string[] words = { "программирование", "параллельный", "поток", "задача", "данные",
                           "результат", "обработка", "алгоритм", "функция", "массив",
                           "сервер", "клиент", "запрос", "ответ", "ошибка" };
        Random rand = new Random(42);

        const int LineCount = 2_000_000;
        string[] lines = new string[LineCount];
        for (int i = 0; i < LineCount; i++)
        {
            int wordCount = rand.Next(5, 15);
            var lineWords = new string[wordCount];
            for (int w = 0; w < wordCount; w++)
                lineWords[w] = words[rand.Next(words.Length)];
            lines[i] = string.Join(" ", lineWords);
        }
        Console.WriteLine($"Сгенерировано {LineCount:N0} строк текста\n");

        // --- 1. Подсчёт вхождений слова: последовательно ---
        string target = "параллельный";
        Console.WriteLine($"--- Поиск слова \"{target}\" ---");

        var sw = Stopwatch.StartNew();
        long countSeq = 0;
        for (int i = 0; i < LineCount; i++)
        {
            int idx = 0;
            while ((idx = lines[i].IndexOf(target, idx, StringComparison.Ordinal)) >= 0)
            {
                countSeq++;
                idx += target.Length;
            }
        }
        sw.Stop();
        Console.WriteLine($"Последовательно:           {sw.ElapsedMilliseconds} мс, Найдено: {countSeq}");

        // --- 2. Параллельно (Parallel.For + локальные переменные) ---
        sw.Restart();
        long countPar = 0;
        Parallel.For(0, LineCount,
            () => 0L,
            (i, state, local) =>
            {
                int idx = 0;
                while ((idx = lines[i].IndexOf(target, idx, StringComparison.Ordinal)) >= 0)
                {
                    local++;
                    idx += target.Length;
                }
                return local;
            },
            local => Interlocked.Add(ref countPar, local)
        );
        sw.Stop();
        Console.WriteLine($"Параллельно:               {sw.ElapsedMilliseconds} мс, Найдено: {countPar}");

        // --- 3. PLINQ ---
        sw.Restart();
        long countPlinq = lines.AsParallel().Sum(line =>
        {
            long c = 0;
            int idx = 0;
            while ((idx = line.IndexOf(target, idx, StringComparison.Ordinal)) >= 0)
            {
                c++;
                idx += target.Length;
            }
            return c;
        });
        sw.Stop();
        Console.WriteLine($"PLINQ:                     {sw.ElapsedMilliseconds} мс, Найдено: {countPlinq}");

        // --- 4. Частотный анализ всех слов ---
        Console.WriteLine("\n--- Частотный анализ слов ---");
        sw.Restart();
        var freqSeq = new Dictionary<string, long>();
        foreach (var line in lines)
            foreach (var word in line.Split(' '))
            {
                if (freqSeq.ContainsKey(word)) freqSeq[word]++;
                else freqSeq[word] = 1;
            }
        sw.Stop();
        long seqMs = sw.ElapsedMilliseconds;
        Console.WriteLine($"Последовательно:           {seqMs} мс");

        sw.Restart();
        var freqPar = new ConcurrentDictionary<string, long>();
        Parallel.ForEach(lines,
            () => new Dictionary<string, long>(),
            (line, state, local) =>
            {
                foreach (var word in line.Split(' '))
                {
                    if (local.ContainsKey(word)) local[word]++;
                    else local[word] = 1;
                }
                return local;
            },
            local =>
            {
                foreach (var kvp in local)
                    freqPar.AddOrUpdate(kvp.Key, kvp.Value, (_, old) => old + kvp.Value);
            }
        );
        sw.Stop();
        Console.WriteLine($"Параллельно:               {sw.ElapsedMilliseconds} мс");

        Console.WriteLine("\nТоп-5 слов:");
        foreach (var kvp in freqPar.OrderByDescending(x => x.Value).Take(5))
            Console.WriteLine($"  {kvp.Key}: {kvp.Value:N0}");

        // --- 5. Поиск строк по шаблону ---
        Console.WriteLine("\n--- Поиск строк, содержащих \"поток\" И \"задача\" ---");
        sw.Restart();
        var matched = lines.AsParallel()
            .Where(l => l.Contains("поток") && l.Contains("задача"))
            .Count();
        sw.Stop();
        Console.WriteLine($"Найдено строк: {matched:N0}, Время: {sw.ElapsedMilliseconds} мс");
    }
}
