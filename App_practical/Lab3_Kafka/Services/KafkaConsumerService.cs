using Confluent.Kafka;
using Calculator16.Models;
using System.Text.Json;
using System.Net.Http.Json;

namespace Calculator16.Services
{
    public class KafkaConsumerService : BackgroundService
    {
        private readonly string _topic;
        private readonly IConsumer<Null, string> _kafkaConsumer;
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<KafkaConsumerService> _logger;

        public KafkaConsumerService(
            IConfiguration config,
            IHttpClientFactory clientFactory,
            ILogger<KafkaConsumerService> logger)
        {
            var consumerConfig = new ConsumerConfig();

            config.GetSection("Kafka:ConsumerSettings")
                .Bind(consumerConfig);

            _topic = config.GetValue<string>("Kafka:TopicName")
                ?? throw new InvalidOperationException("Kafka topic is missing");

            _kafkaConsumer =
                new ConsumerBuilder<Null, string>(
                    consumerConfig).Build();

            _clientFactory = clientFactory;
            _logger = logger;
        }

        protected override Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            return Task.Run(
                () => StartConsumerLoop(stoppingToken),
                stoppingToken);
        }

        private async Task StartConsumerLoop(
            CancellationToken cancellationToken)
        {
            _kafkaConsumer.Subscribe(_topic);

            _logger.LogInformation(
                "Kafka Consumer started. Topic: {Topic}", _topic);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var cr = _kafkaConsumer.Consume(
                        cancellationToken);

                    var inputData =
                        JsonSerializer.Deserialize<DataInputVariant>(
                            cr.Message.Value);

                    if (inputData == null)
                        continue;

                    var result =
                        CalculatorLibrary.CalculateOperation(
                            inputData.Operand_1,
                            inputData.Operand_2,
                            inputData.Type_operation);

                    inputData.Result = result.ToString();

                    var httpClient = _clientFactory.CreateClient();

                    using var response =
                        await httpClient.PostAsJsonAsync(
                            "http://localhost:5016/Calculator/Callback",
                            inputData,
                            cancellationToken);

                    response.EnsureSuccessStatusCode();

                    _logger.LogInformation(
                        "Message: {Message}; Result: {Result}",
                        cr.Message.Value,
                        inputData.Result);
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (ConsumeException e)
                {
                    _logger.LogWarning(
                        "Kafka: {Error}", e.Error.Reason);

                    if (e.Error.IsFatal)
                        break;

                    await Task.Delay(2000, cancellationToken);
                }
                catch (Exception e)
                {
                    _logger.LogError(
                        e, "Error processing Kafka message");

                    await Task.Delay(2000, cancellationToken);
                }
            }
        }

        public override void Dispose()
        {
            _kafkaConsumer.Close();
            _kafkaConsumer.Dispose();

            base.Dispose();
        }
    }
}
