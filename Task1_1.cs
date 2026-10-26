using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SSP_Lab5;

// ЛАБА 5.1 — Задание 1: Producer-Consumer с Semaphore
public class DataItem
{
    public int Id { get; set; }
    public string Payload { get; set; }

    public override string ToString() => $"[Id={Id}, Payload={Payload}]";
}

public static class Task1_1
{
    private static Queue<DataItem> _buffer = new Queue<DataItem>();
    private const int BufferSize = 5;

    private static SemaphoreSlim _emptySlots = new SemaphoreSlim(BufferSize, BufferSize);
    private static SemaphoreSlim _filledSlots = new SemaphoreSlim(0, BufferSize);

    private static Mutex _mutex = new Mutex();

    private static bool _isRunning = true;
    private static int _itemId = 0;

    public static void Run()
    {
        Console.WriteLine("=== Задание 1.1: Producer-Consumer с Semaphore ===\n");

        // 3 производителя и 4 потребителя
        Task p1 = Task.Run(() => Producer(1));
        Task p2 = Task.Run(() => Producer(2));
        Task p3 = Task.Run(() => Producer(3));
        Task c1 = Task.Run(() => Consumer(1));
        Task c2 = Task.Run(() => Consumer(2));
        Task c3 = Task.Run(() => Consumer(3));
        Task c4 = Task.Run(() => Consumer(4));

        Thread.Sleep(5000);
        _isRunning = false;

        Task.WaitAll(p1, p2, p3, c1, c2, c3, c4);
        Console.WriteLine("\nВсе потоки завершены.");
    }

    static async Task Producer(int id)
    {
        var rand = new Random(id);
        while (_isRunning)
        {
            await _emptySlots.WaitAsync();
            _mutex.WaitOne();
            try
            {
                var item = new DataItem
                {
                    Id = Interlocked.Increment(ref _itemId),
                    Payload = $"DATA-{rand.Next(1000, 9999)}"
                };
                _buffer.Enqueue(item);
                Console.WriteLine($"[Producer {id}] Добавлен: {item} | В буфере: {_buffer.Count}");
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
            _filledSlots.Release();
            await Task.Delay(rand.Next(200, 600));
        }
        Console.WriteLine($"[Producer {id}] Завершён.");
    }

    static async Task Consumer(int id)
    {
        var rand = new Random(id + 10);
        while (_isRunning)
        {
            await _filledSlots.WaitAsync();
            _mutex.WaitOne();
            DataItem item;
            try
            {
                item = _buffer.Dequeue();
                Console.WriteLine($"[Consumer {id}] Извлечён: {item} | В буфере: {_buffer.Count}");
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
            _emptySlots.Release();
            await Task.Delay(rand.Next(300, 800));
        }
        Console.WriteLine($"[Consumer {id}] Завершён.");
    }
}
