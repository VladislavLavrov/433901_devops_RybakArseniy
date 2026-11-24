using Calculator16.Models;
using Calculator16.Services;
using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Calculator16.Controllers
{
    public class CalculatorController : Controller
    {
        private readonly CalculatorContext _context;
        private readonly KafkaProducerService<Null, string> _producer;
        private readonly IConfiguration _config;

        public CalculatorController(
            CalculatorContext context,
            KafkaProducerService<Null, string> producer,
            IConfiguration config)
        {
            _context = context;
            _producer = producer;
            _config = config;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var data = _context.DataInputVariants
                .OrderByDescending(x => x.ID_DataInputVariant)
                .ToList();

            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Calculate(
            double num1, double num2, Operation operation)
        {
            if (!ModelState.IsValid ||
                !Enum.IsDefined(operation) ||
                !double.IsFinite(num1) ||
                !double.IsFinite(num2) ||
                (operation == Operation.Divide && num2 == 0))
            {
                TempData["Error"] = "Проверьте исходные данные.";
                return RedirectToAction(nameof(Index));
            }

            var dataInputVariant = new DataInputVariant
            {
                Operand_1 = num1,
                Operand_2 = num2,
                Type_operation = operation
            };

            await SendDataToKafka(dataInputVariant);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult Callback(
            [FromBody] DataInputVariant inputData)
        {
            SaveDataAndResult(inputData);

            return Ok();
        }

        private DataInputVariant SaveDataAndResult(
            DataInputVariant inputData)
        {
            _context.DataInputVariants.Add(inputData);
            _context.SaveChanges();

            return inputData;
        }

        private async Task SendDataToKafka(
            DataInputVariant dataInputVariant)
        {
            var json = JsonSerializer.Serialize(dataInputVariant);

            var topic = _config.GetValue<string>("Kafka:TopicName")
                ?? throw new InvalidOperationException("Kafka topic is missing");

            await _producer.ProduceAsync(
                topic,
                new Message<Null, string> { Value = json });
        }
    }
}
