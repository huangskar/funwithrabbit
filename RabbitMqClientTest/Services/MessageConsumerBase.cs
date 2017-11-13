using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMqClientTest.Messages;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMqClientTest.Services
{
	/// <summary>
	/// Consumes a <see cref="TMessage"/> subscription.
	/// </summary>
	/// <typeparam name="TMessage"></typeparam>
	public abstract class MessageConsumerBase<TMessage> : IMessageConsumer<TMessage> where TMessage : IMessage
	{
		private const string RoutingKey = "/";
		private readonly IConnectionFactory _connectionFactory;
		private readonly object _lockObject = new object();
		private readonly ILogger _logger;
		private IModel _channel;
		private IConnection _connection;
		private string _consumerTag;
		private string _exchangeName;
		private bool _isInitialized;
		private bool _isInitializing;
		private string _queueName;

		protected MessageConsumerBase(IConnectionFactory connectionFactory, ILoggerFactory loggerFactory)
		{
			_logger = loggerFactory.CreateLogger(GetType());
			_connectionFactory = connectionFactory;
		}

		public void Initialize()
		{
			lock (_lockObject)
			{
				if (_isInitialized || _isInitializing) return;
				_isInitializing = true;
			}

			try
			{
				_connection = _connectionFactory.CreateConnection();
				_channel = _connection.CreateModel();
				_exchangeName = $"EXCH_{typeof(TMessage).FullName}";
				_queueName = $"QUEUE_{typeof(TMessage).FullName}";
				_channel.ExchangeDeclare(_exchangeName, ExchangeType.Direct);
				_channel.QueueDeclare(_queueName, false, false, false, null);
				_channel.QueueBind(_queueName, _exchangeName, RoutingKey, null);
				var consumer = new AsyncEventingBasicConsumer(_channel);

				consumer.Received += (channel, eventArgs) =>
				{
					if (!(channel is IModel model))
						throw new InvalidCastException("Could not cast channel argument as an IModel.");
					var body = eventArgs.Body;
					var bodyText = Encoding.UTF8.GetString(body);
					var message = JsonConvert.DeserializeObject<TMessage>(bodyText);
					try
					{
						Consume(message);
						model.BasicAck(eventArgs.DeliveryTag, false);
					}
					catch (Exception ex)
					{
						_logger.LogError(new EventId(), ex, $"Failed to consume the message for consumer tag {_consumerTag}.");
						model.BasicNack(eventArgs.DeliveryTag, false, true);
						throw;
					}
					return Task.CompletedTask;
				};

				_consumerTag = _channel.BasicConsume(_queueName, false, consumer);
				_isInitialized = true;
			}
			catch (Exception ex)
			{
				_logger.LogCritical(new EventId(), ex, "Failed to initialize the message consumer.");
				throw;
			}
			finally
			{
				_isInitializing = false;
			}
		}

		public abstract void Consume(TMessage message);

		/// <inheritdoc />
		/// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
		public void Dispose()
		{
			_channel?.Dispose();
			_connection?.Dispose();
		}

		~MessageConsumerBase() { Dispose(); }
	}
}