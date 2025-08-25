//using Azure.Messaging.ServiceBus;
//using Microsoft.Extensions.Options;
//using ScenariosWHwar.API.Core.Common.Configuration;
//using ScenariosWHwar.API.Core.Common.Interfaces;
//using System.Text.Json;

//namespace ScenariosWHwar.API.Core.Common.Services;

//public class ServiceBusPublisher : IServiceBusPublisher, IDisposable
//{
//    private readonly ServiceBusClient _client;
//    private readonly ServiceBusConfig _config;

//    public ServiceBusPublisher(IOptions<ServiceBusConfig> config)
//    {
//        _config = config.Value;
//        _client = new ServiceBusClient(_config.ConnectionString);
//    }

//    public async Task PublishAsync<T>(T message, string queueName, CancellationToken cancellationToken = default)
//    {
//        var sender = _client.CreateSender(queueName);

//        try
//        {
//            var messageBody = JsonSerializer.Serialize(message);
//            var serviceBusMessage = new ServiceBusMessage(messageBody)
//            {
//                ContentType = "application/json",
//                Subject = typeof(T).Name
//            };

//            await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
//        }
//        finally
//        {
//            await sender.DisposeAsync();
//        }
//    }

//    public void Dispose()
//    {
//        _client?.DisposeAsync().AsTask().Wait();
//    }
//}
