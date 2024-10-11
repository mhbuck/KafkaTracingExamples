using Akka.Hosting;
using Akka.Logger.Serilog;
using Akka.Streams.Kafka.Settings;
using AkkaKafkaTracingExample;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;

var hostBuilder = new HostBuilder();

hostBuilder.ConfigureServices((context, services) =>
{
    services.AddSerilog();
    services.AddAkka("MyActorSystem", (builder, sp) =>
    {
        builder
            .ConfigureLoggers(setup =>
            {
                setup.LogLevel = Akka.Event.LogLevel.DebugLevel;
                setup.AddLogger<SerilogLogger>();
            })
            .AddHocon(KafkaExtensions.DefaultSettings, HoconAddMode.Prepend)
            .WithActors((system, registry, resolver) =>
            {
                var props = resolver.Props<PublisherActor>();
                var publisherActor = system.ActorOf(props, "publisher");
                
                registry.Register<PublisherActor>(publisherActor);
            })
            .WithActors((system, registry, resolver) =>
            {
                var props = resolver.Props<ConsumerActor>();
                var consumerActor = system.ActorOf(props, "consumer");
                
                registry.Register<ConsumerActor>(consumerActor);
            });
    });
    
    services.AddSingleton(_ =>
    {
        ProducerConfig producerConfig = new() {BootstrapServers = Constants.BootstrapServers};
        return new InstrumentedProducerBuilder<string, string>(producerConfig);
    });
    services.AddSingleton(_ =>
    {
        ConsumerConfig consumerConfig = new()
        {
            BootstrapServers = Constants.BootstrapServers,
            GroupId = "group-a",
        };
        return new InstrumentedConsumerBuilder<string, string>(consumerConfig);
    });
    
    services
        .AddOpenTelemetry()
        .WithTracing(tracing =>
        {
            tracing
                // Noisy so commented out while not working
                //.AddConsoleExporter()
                .AddOtlpExporter()
                .AddKafkaProducerInstrumentation<string, string>()
                .AddKafkaConsumerInstrumentation<string, string>();
        })
        .WithMetrics(metering =>
        {
            metering
                // Noisy so commented out while not working
                //.AddConsoleExporter()
                .AddOtlpExporter()
                .AddKafkaProducerInstrumentation<string, string>()
                .AddKafkaConsumerInstrumentation<string, string>();
        });
    
});



var host = hostBuilder.Build();

await host.RunAsync();