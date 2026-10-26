using System.Diagnostics;

namespace SSP_Lab5;

// Вариант 7: Сортировка и поиск (параллельные алгоритмы)
public static class Variant7
{
    public static void Run()
    {
        Console.WriteLine("=== ВАРИАНТ 7: Сортировка и поиск ===\n");

        const int N = 10_000_000;
        Random rand = new Random(42);

        // --- 1. Параллельная сортировка слиянием ---
        Console.WriteLine("--- Сортировка слиянием ---");
        int[] dataSeq = new int[N];
        int[] dataPar = new int[N];
        for (int i = 0; i < N; i++)
            dataSeq[i] = rand.Next();
        Array.Copy(dataSeq, dataPar, N);

        var sw = Stopwatch.StartNew();
        Array.Sort(dataSeq);
        sw.Stop();
        Console.WriteLine($"Array.Sort (последовательно): {sw.ElapsedMilliseconds} мс");

        sw.Restart();
        ParallelMergeSort(dataPar, 0, N - 1, 0);
        sw.Stop();
        Console.WriteLine($"Parallel MergeSort:           {sw.ElapsedMilliseconds} мс");

        bool sorted = true;
        for (int i = 1; i < N && sorted; i++)
            if (dataPar[i] < dataPar[i - 1]) sorted = false;
        Console.WriteLine($"Результат отсортирован: {(sorted ? "Да" : "Нет")}");

        // --- 2. Параллельный бинарный поиск по нескольким значениям ---
        Console.WriteLine("\n--- Параллельный поиск нескольких значений ---");
        int[] targets = new int[1000];
        for (int i = 0; i < targets.Length; i++)
            targets[i] = dataSeq[rand.Next(N)];

        sw.Restart();
        int[] positionsSeq = new int[targets.Length];
        for (int i = 0; i < targets.Length; i++)
            positionsSeq[i] = Array.BinarySearch(dataSeq, targets[i]);
        sw.Stop();
        Console.WriteLine($"Последовательный поиск 1000 значений: {sw.ElapsedTicks} тиков");

        sw.Restart();
        int[] positionsPar = new int[targets.Length];
        Parallel.For(0, targets.Length, i =>
        {
            positionsPar[i] = Array.BinarySearch(dataSeq, targets[i]);
        });
        sw.Stop();
        Console.WriteLine($"Параллельный поиск 1000 значений:     {sw.ElapsedTicks} тиков");

        // --- 3. Параллельный подсчёт элементов в диапазонах ---
        Console.WriteLine("\n--- Подсчёт по диапазонам ---");
        int buckets = 10;
        int min = dataSeq[0], max = dataSeq[N - 1];
        long range = (long)max - min + 1;

        sw.Restart();
        long[] countsSeq = new long[buckets];
        for (int i = 0; i < N; i++)
        {
            int bucket = (int)((long)(dataSeq[i] - min) * buckets / range);
            if (bucket >= buckets) bucket = buckets - 1;
            countsSeq[bucket]++;
        }
        sw.Stop();
        Console.WriteLine($"Последовательно: {sw.ElapsedMilliseconds} мс");

        sw.Restart();
        long[] countsPar = new long[buckets];
        object lockObj = new object();
        Parallel.For(0, N,
            () => new long[buckets],
            (i, state, local) =>
            {
                int bucket = (int)((long)(dataSeq[i] - min) * buckets / range);
                if (bucket >= buckets) bucket = buckets - 1;
                local[bucket]++;
                return local;
            },
            local =>
            {
                lock (lockObj)
                {
                    for (int b = 0; b < buckets; b++)
                        countsPar[b] += local[b];
                }
            }
        );
        sw.Stop();
        Console.WriteLine($"Параллельно:     {sw.ElapsedMilliseconds} мс");

        Console.WriteLine("\nРаспределение по диапазонам:");
        for (int b = 0; b < buckets; b++)
            Console.WriteLine($"  Диапазон {b + 1}: {countsPar[b]:N0} элементов");

        // --- 4. PLINQ: Top-K элементов ---
        Console.WriteLine("\n--- Top-10 максимальных (PLINQ) ---");
        int[] unsorted = new int[N];
        for (int i = 0; i < N; i++)
            unsorted[i] = rand.Next();

        sw.Restart();
        var top10 = unsorted.AsParallel()
            .OrderByDescending(x => x)
            .Take(10)
            .ToList();
        sw.Stop();
        Console.WriteLine($"Время: {sw.ElapsedMilliseconds} мс");
        Console.WriteLine($"Top-10: {string.Join(", ", top10)}");
    }

    static void ParallelMergeSort(int[] arr, int left, int right, int depth)
    {
        if (right - left < 10000 || depth > 4)
        {
            Array.Sort(arr, left, right - left + 1);
            return;
        }

        int mid = left + (right - left) / 2;
        var t1 = Task.Run(() => ParallelMergeSort(arr, left, mid, depth + 1));
        var t2 = Task.Run(() => ParallelMergeSort(arr, mid + 1, right, depth + 1));
        Task.WaitAll(t1, t2);

        Merge(arr, left, mid, right);
    }

    static void Merge(int[] arr, int left, int mid, int right)
    {
        int[] temp = new int[right - left + 1];
        int i = left, j = mid + 1, k = 0;

        while (i <= mid && j <= right)
            temp[k++] = arr[i] <= arr[j] ? arr[i++] : arr[j++];
        while (i <= mid) temp[k++] = arr[i++];
        while (j <= right) temp[k++] = arr[j++];

        Array.Copy(temp, 0, arr, left, temp.Length);
    }
}
