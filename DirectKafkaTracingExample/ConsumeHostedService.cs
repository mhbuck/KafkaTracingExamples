using Confluent.Kafka;
using Microsoft.Extensions.Hosting;

namespace DirectKafkaTracingExample;

public class ConsumeHostedService(
    InstrumentedConsumerBuilder<string, string> instrumentedConsumerBuilder)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Give time for the topic to be created.
        await Task.Delay(1000, stoppingToken);

        IConsumer<string, string> consumer = instrumentedConsumerBuilder.Build();
        consumer.Subscribe(Constants.Topic);

        Console.WriteLine($"Consumer {consumer.Name} subscribed to topic: {string.Join(",", consumer.Subscription)}");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(stoppingToken);
                if (consumeResult == null || consumeResult.Message == null)
                {
                    Console.WriteLine($"Consumer {consumer.Name} received null message.");
                    continue;
                }

                Console.WriteLine($"Consumer {consumer.Name} received message: {consumeResult.Message.Value}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        consumer.Close();
        Console.WriteLine($"Consumer {consumer.Name} closed.");
    }
}