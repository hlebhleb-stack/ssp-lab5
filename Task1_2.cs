namespace SSP_Lab5;

// ЛАБА 5.1 — Задание 2: Deadlock — обнаружение и исправление
public static class Task1_2
{
    private static object _lockA = new object();
    private static object _lockB = new object();

    private static object _fixedLockA = new object();
    private static object _fixedLockB = new object();

    private static object _tryLockA = new object();
    private static object _tryLockB = new object();

    public static void Run()
    {
        Console.WriteLine("=== Задание 1.2: Deadlock — обнаружение и исправление ===\n");

        // --- Часть 1: Демонстрация Deadlock ---
        Console.WriteLine("--- Часть 1: Deadlock (ждём 10 сек) ---");
        Console.WriteLine("Thread1: захватывает lockA -> lockB");
        Console.WriteLine("Thread2: захватывает lockB -> lockA");
        Console.WriteLine("Для диагностики: Debug -> Break All -> Parallel Stacks\n");

        var t1 = Task.Run(() => DeadlockThread1());
        var t2 = Task.Run(() => DeadlockThread2());
        bool completed = Task.WaitAll(new[] { t1, t2 }, TimeSpan.FromSeconds(10));
        if (!completed)
            Console.WriteLine(">>> DEADLOCK обнаружен! Потоки зависли и не завершились за 10 сек.\n");

        // --- Часть 2: Исправление ---
        Console.WriteLine("--- Часть 2: Исправленный вариант (одинаковый порядок: lockA -> lockB) ---");
        var t3 = Task.Run(() => FixedThread1());
        var t4 = Task.Run(() => FixedThread2());
        Task.WaitAll(t3, t4);
        Console.WriteLine(">>> Оба потока завершились без deadlock!\n");

        // --- Часть 3: Monitor.TryEnter ---
        Console.WriteLine("--- Часть 3: Monitor.TryEnter с таймаутом (2 сек) ---");
        var t5 = Task.Run(() => TryEnterThread1());
        var t6 = Task.Run(() => TryEnterThread2());
        Task.WaitAll(t5, t6);
        Console.WriteLine(">>> Завершено через таймаут без зависания!");
    }

    static void DeadlockThread1()
    {
        lock (_lockA)
        {
            Console.WriteLine("Thread1: захватил lockA");
            Thread.Sleep(1000);
            lock (_lockB)
                Console.WriteLine("Thread1: захватил lockB (не будет выведено)");
        }
    }

    static void DeadlockThread2()
    {
        lock (_lockB)
        {
            Console.WriteLine("Thread2: захватил lockB");
            Thread.Sleep(1000);
            lock (_lockA)
                Console.WriteLine("Thread2: захватил lockA (не будет выведено)");
        }
    }

    static void FixedThread1()
    {
        lock (_fixedLockA)
        {
            Console.WriteLine("FixedThread1: захватил lockA");
            Thread.Sleep(500);
            lock (_fixedLockB)
                Console.WriteLine("FixedThread1: захватил lockB — работает!");
        }
    }

    static void FixedThread2()
    {
        lock (_fixedLockA)
        {
            Console.WriteLine("FixedThread2: захватил lockA");
            Thread.Sleep(500);
            lock (_fixedLockB)
                Console.WriteLine("FixedThread2: захватил lockB — работает!");
        }
    }

    static void TryEnterThread1()
    {
        bool gotA = false, gotB = false;
        try
        {
            Monitor.TryEnter(_tryLockA, TimeSpan.FromSeconds(2), ref gotA);
            if (!gotA) { Console.WriteLine("TryThread1: не удалось захватить lockA — таймаут!"); return; }
            Console.WriteLine("TryThread1: захватил lockA");
            Thread.Sleep(1000);

            Monitor.TryEnter(_tryLockB, TimeSpan.FromSeconds(2), ref gotB);
            if (!gotB) { Console.WriteLine("TryThread1: не удалось захватить lockB — таймаут!"); return; }
            Console.WriteLine("TryThread1: захватил lockB — работает!");
        }
        finally
        {
            if (gotB) Monitor.Exit(_tryLockB);
            if (gotA) Monitor.Exit(_tryLockA);
        }
    }

    static void TryEnterThread2()
    {
        bool gotA = false, gotB = false;
        try
        {
            Monitor.TryEnter(_tryLockB, TimeSpan.FromSeconds(2), ref gotB);
            if (!gotB) { Console.WriteLine("TryThread2: не удалось захватить lockB — таймаут!"); return; }
            Console.WriteLine("TryThread2: захватил lockB");
            Thread.Sleep(1000);

            Monitor.TryEnter(_tryLockA, TimeSpan.FromSeconds(2), ref gotA);
            if (!gotA) { Console.WriteLine("TryThread2: не удалось захватить lockA — таймаут!"); return; }
            Console.WriteLine("TryThread2: захватил lockA — работает!");
        }
        finally
        {
            if (gotA) Monitor.Exit(_tryLockA);
            if (gotB) Monitor.Exit(_tryLockB);
        }
    }
}
