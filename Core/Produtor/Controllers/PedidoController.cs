using Core.Entidades;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Produtor.Controllers
{
    [ApiController]
    [Route("/Pedido")]
    public class PedidoController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public PedidoController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost]
        public IActionResult Post()
        {
            var fila = _configuration.GetSection("RabbitMQ")["NomeFila"] ?? string.Empty;
            var servidor = _configuration.GetSection("RabbitMQ")["Servidor"] ?? string.Empty;
            var usuario = _configuration.GetSection("RabbitMQ")["Usuario"] ?? string.Empty;
            var senha = _configuration.GetSection("RabbitMQ")["Senha"] ?? string.Empty;

            var factory = new ConnectionFactory() { HostName = servidor, UserName = usuario, Password = senha };
            using var connection = factory.CreateConnection();
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(
                    queue: fila,
                    durable: false,
                    exclusive: false,
                    autoDelete: true, // impede erros nas execuções posteriores
                    arguments: null);

                    // Número de pedidos desejados
                    int numeroDePedidos = 5;

                    for (int i = 1; i <= numeroDePedidos; i++)
                    {
                        // Criar um pedido fictício para ilustração
                        var pedido = new Pedido(i, new Usuario(i, $"Usuário{i}", $"usuario{i}@email.com"));

                        // Serializar o pedido
                        var message = JsonSerializer.Serialize(pedido);
                        var body = Encoding.UTF8.GetBytes(message);

                        // Publicar o pedido no RabbitMQ
                        channel.BasicPublish(
                            exchange: "",
                            routingKey: fila,
                            basicProperties: null,
                            body: body);

                        Console.WriteLine($"Pedido {i} publicado no RabbitMQ.");
                    }
            }

            return Ok();
        }
    }
}