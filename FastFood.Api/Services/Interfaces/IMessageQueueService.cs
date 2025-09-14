namespace FoodFast.Services.Interfaces
{
    public interface IMessageQueueService
    {
        Task EnqueueAsync<T>(T message);
        Task ConsumeAsync<T>(Func<T, Task> handler, CancellationToken cancellationToken);
    }
}
