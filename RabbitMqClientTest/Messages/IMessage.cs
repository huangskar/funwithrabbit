using System;

namespace RabbitMqClientTest.Messages
{
	public interface IMessage
	{
		string HostName { get; set; }
		DateTime TimestampUtc { get; set; }

		//
		}
}