using Confluent.Kafka;
using Microsoft.Extensions.Hosting;

namespace DirectKafkaTracingExample;

public class ProduceHostedService(
    InstrumentedProducerBuilder<string, string> instrumentedProducerBuilder)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IProducer<string, string> producer = instrumentedProducerBuilder.Build();

        for (int j = 0; j < 100; j++)
        {
            await producer.ProduceAsync(
                Constants.Topic,
                new Message<string, string> { Key = "any_key", Value = $"any_value_{j}" },
                stoppingToken);
            
            Console.WriteLine($"Producer {producer.Name} produced message: any_value_{j}");
            
            // Simulate intermittent messages opposed to all produced at once.
            await Task.Delay(500, stoppingToken);
        }

        for (int j = 0; j < 100; j++)
        {
            producer.Produce(
                Constants.Topic,
                new Message<string, string> { Key = "any_key", Value = $"any_value_{j}" });
        }

        producer.Flush(stoppingToken);
    }
}