using Akka;
using Akka.Actor;
using Akka.Streams.Dsl;
using Akka.Streams.Kafka.Dsl;
using Akka.Streams.Kafka.Settings;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;

namespace AkkaKafkaTracingExample;

public class ConsumerActor : ReceiveActor
{
    private readonly ActorSystem _actorSystem;
    private readonly IServiceScope _scopedServiceProvider;

    public ConsumerActor(IServiceProvider serviceProvider, ActorSystem actorSystem)
    {
        _actorSystem = actorSystem;
        _scopedServiceProvider = serviceProvider.CreateScope();
    }

    protected override void PreStart()
    {
        var instrumentedConsumerBuilder = _scopedServiceProvider.ServiceProvider.GetRequiredService<InstrumentedConsumerBuilder<string, string>>();
        var useInstrumentedConsumer = true;

        var settings = ConsumerSettings<string, string>
            .Create(_actorSystem, Deserializers.Utf8, Deserializers.Utf8)
            .WithBootstrapServers(Constants.BootstrapServers)
            .WithGroupId("group-b")
            .WithClientId($"{Environment.MachineName}-{Environment.ProcessId}-consumer");

        if (useInstrumentedConsumer)
        {
            var instrumentedConsumer = instrumentedConsumerBuilder
                .Build();
            
            
            settings = settings.WithConsumerFactory(consumerSettings =>
            {
                return instrumentedConsumer;
            });
        }
        
        Console.WriteLine("Consumer: Started");
        var subscription = Subscriptions.Topics(Constants.Topic);
        
        var bob = KafkaConsumer.CommittableSource(settings, subscription)
            .SelectAsync(1, async message =>
            {
                Console.WriteLine($"Consumer: Consumed {message.Record.Message.Value}");
                await message.CommitableOffset.Commit();
                return Done.Instance;
            })
            .RunWith(Sink.Ignore<Done>(), _actorSystem);
    }
}