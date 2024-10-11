using Confluent.Kafka;
using DirectKafkaTracingExample;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace;
using Serilog;

var hostBuilder = new HostBuilder();


hostBuilder.ConfigureServices((context, services) =>
{
    services.AddSerilog();
    services.AddSingleton(_ =>
    {
        ProducerConfig producerConfig = new() { BootstrapServers = Constants.BootstrapServers };
        return new InstrumentedProducerBuilder<string, string>(producerConfig);
    });
    services.AddSingleton(_ =>
    {
        ConsumerConfig consumerConfigA = new()
        {
            BootstrapServers = Constants.BootstrapServers,
            GroupId = "group-a",
            AutoOffsetReset = AutoOffsetReset.Earliest,
        };
        return new InstrumentedConsumerBuilder<string, string>(consumerConfigA);
    });

    services.AddOpenTelemetry()
        .WithTracing(tracing =>
        {
            tracing
                .AddConsoleExporter()
                .AddOtlpExporter()
                .AddKafkaProducerInstrumentation<string, string>()
                .AddKafkaConsumerInstrumentation<string, string>();
        })
        .WithMetrics(metering =>
        {
            // metering.AddConsoleExporter()
            //     .AddOtlpExporter()
            //     .AddKafkaProducerInstrumentation<string, string>()
            //     .AddKafkaConsumerInstrumentation<string, string>();
        });

    services.AddHostedService<ConsumeHostedService>();
    services.AddHostedService<ProduceHostedService>();
    
});


var host = hostBuilder.Build();

await host.RunAsync();
