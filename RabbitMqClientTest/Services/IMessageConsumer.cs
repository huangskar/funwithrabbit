using System;
using RabbitMqClientTest.Messages;

namespace RabbitMqClientTest.Services
{
	public interface IMessageConsumer : IDisposable
	{
		void Initialize();
	}

	public interface IMessageConsumer<in TMessage> : IMessageConsumer where TMessage : IMessage
	{
		void Consume(TMessage message);
	}
}