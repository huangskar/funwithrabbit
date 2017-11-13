using System;
using RabbitMqClientTest.Messages;

namespace RabbitMqClientTest.Services
{
	public interface IMessageProducer : IDisposable
	{
		void Initialize();
	}

	public interface IMessageProducer<in TMessage> : IMessageProducer where TMessage : IMessage
	{
		void SendMessage(TMessage message);
	}
}