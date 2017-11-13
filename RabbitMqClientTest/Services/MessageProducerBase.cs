using System;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMqClientTest.Messages;
using RabbitMQ.Client;

namespace RabbitMqClientTest.Services
{
	/// <summary>
	/// Produces (sends) a <see cref="TMessage"/> object
	/// </summary>
	/// <typeparam name="TMessage"></typeparam>
	public abstract class MessageProducerBase<TMessage> : IMessageProducer<TMessage> where TMessage : IMessage
	{
		private const string RoutingKey = "/";
		private readonly IConnectionFactory _connectionFactory;
		private readonly object _lockObject = new object();
		private readonly ILogger _logger;
		private IModel _channel;
		private IConnection _connection;
		private string _exchangeName;
		private bool _isInitialized;
		private bool _isInitializing;
		private string _queueName;

		protected MessageProducerBase(IConnectionFactory connectionFactory, ILoggerFactory loggerFactory)
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
				_isInitialized = true;
			}
			catch (Exception ex)
			{
				_logger.LogCritical(new EventId(), ex, "Failed to initialize the message producer.");
				throw;
			}
			finally
			{
				_isInitializing = false;
			}
		}

		public virtual void SendMessage(TMessage message)
		{
			try
			{
				var serialized = JsonConvert.SerializeObject(message);
				var bytes = Encoding.UTF8.GetBytes(serialized);
				_channel.BasicPublish(_exchangeName, RoutingKey, null, bytes);
			}
			catch (Exception ex)
			{
				_logger.LogError(new EventId(), ex, "Failed to send the message.  There may be a connection issue.");
				throw;
			}
		}

		/// <inheritdoc />
		/// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
		public void Dispose()
		{
			_connection?.Dispose();
			_channel?.Dispose();
		}

		~MessageProducerBase() { Dispose(); }
	}
}