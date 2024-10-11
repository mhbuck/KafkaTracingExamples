using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.Streams.Kafka.Dsl;
using Akka.Streams.Kafka.Messages;
using Akka.Streams.Kafka.Settings;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;

namespace AkkaKafkaTracingExample;

public class PublisherActor : ReceiveActor, IWithTimers
{
    private readonly IServiceScope _scopedServiceProvider;
    private readonly ISourceQueueWithComplete<string> _queue;

    public PublisherActor(IServiceProvider serviceProvider, ActorSystem actorSystem)
    {
        _scopedServiceProvider = serviceProvider.CreateScope();
        
        var instrumentedBuilder = _scopedServiceProvider.ServiceProvider.GetRequiredService<InstrumentedProducerBuilder<string, string>>();
        var producer = instrumentedBuilder.Build();

        var producerConfig = new ProducerConfig()
        {
            BootstrapServers = Constants.BootstrapServers,
            ClientId = $"{Environment.MachineName}-{Environment.ProcessId}-producer",
            CompressionType = CompressionType.Lz4,
            AllowAutoCreateTopics = true
        };

        var settings = ProducerSettings<string, string>
            .Create(actorSystem, Serializers.Utf8, Serializers.Utf8)
            .WithProducerConfig(producerConfig);
        
        _queue = Source.Queue<string>(200, OverflowStrategy.Backpressure)
            .Select(elem => new ProducerRecord<string, string>(Constants.Topic, elem.ToString()))
            .To(KafkaProducer.PlainSink(settings, producer))
            .Run(actorSystem);

        ReceiveAsync<string>(Handle);
    }
    
    protected override void PreStart()
    {
        Timers.StartPeriodicTimer("Tick", "Tick", TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10));
    }

    public ITimerScheduler Timers { get; set; } = null!;
    
    public async Task Handle(string message)
    {
        Console.WriteLine("Publisher: Published");
        
        var dateTime = DateTime.UtcNow;
        var formattedMessage = $"{dateTime:O}";

        await _queue.OfferAsync(formattedMessage);
    }
}