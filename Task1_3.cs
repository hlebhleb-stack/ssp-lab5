namespace SSP_Lab5;

// ЛАБА 5.1 — Задание 3: Mutex для синхронизации
public static class Task1_3
{
    public static void Run()
    {
        Console.WriteLine("=== Задание 1.3: Mutex для синхронизации ===\n");

        Console.WriteLine("Симуляция двух экземпляров программы через потоки:\n");

        var t1 = Task.Run(() => RunInstance(1));
        Thread.Sleep(500);
        var t2 = Task.Run(() => RunInstance(2));

        Task.WaitAll(t1, t2);

        Console.WriteLine("\nОба экземпляра завершены.");
    }

    static void RunInstance(int instanceId)
    {
        Console.WriteLine($"[Экземпляр {instanceId}] Попытка захвата глобального мьютекса...");

        using (Mutex mutex = new Mutex(false, "Global\\SSP_Lab5_Mutex"))
        {
            if (mutex.WaitOne(TimeSpan.FromSeconds(3)))
            {
                try
                {
                    Console.WriteLine($"[Экземпляр {instanceId}] Мьютекс захвачен. Программа выполняется.");
                    Thread.Sleep(5000);
                    Console.WriteLine($"[Экземпляр {instanceId}] Работа завершена.");
                }
                finally
                {
                    mutex.ReleaseMutex();
                    Console.WriteLine($"[Экземпляр {instanceId}] Мьютекс освобождён.");
                }
            }
            else
            {
                Console.WriteLine($"[Экземпляр {instanceId}] Не удалось захватить мьютекс — другой экземпляр уже запущен!");
            }
        }
    }
}
