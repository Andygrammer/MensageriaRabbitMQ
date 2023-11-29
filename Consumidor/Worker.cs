using Core.Entidades;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Consumidor
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var fila = _configuration.GetSection("RabbitMQ")["NomeFila"] ?? string.Empty;
            var servidor = _configuration.GetSection("RabbitMQ")["Servidor"] ?? string.Empty;
            var usuario = _configuration.GetSection("RabbitMQ")["Usuario"] ?? string.Empty;
            var senha = _configuration.GetSection("RabbitMQ")["Senha"] ?? string.Empty;

            while (!stoppingToken.IsCancellationRequested)
            {
                var factory = new ConnectionFactory() { HostName = servidor, UserName = usuario, Password = senha };
                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();
                channel.QueueDeclare(queue: fila,
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: true, // impede erros nas execuções posteriores
                                 arguments: null);

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (sender, eventArgs) =>
                {
                    var body = eventArgs.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var pedido = JsonSerializer.Deserialize<Pedido>(body);

                    Console.WriteLine(pedido?.ToString());
                };

                channel.BasicConsume(
                    queue: fila,
                    autoAck: true,
                    consumer: consumer);
                await Task.Delay(2000, stoppingToken);
            }
        }
    }
}