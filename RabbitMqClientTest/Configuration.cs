using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace RabbitMqClientTest
{
	public static class Configuration
	{
		public static IServiceCollection ConfigureRabbitMq(this IServiceCollection services, IConfiguration config)
		{
			services.AddSingleton<IConnectionFactory>(new ConnectionFactory
			{
				Uri = new Uri(config.GetSection("ConnectionStrings:RabbitServer").Value)
			});

			return services;
		}
	}
}