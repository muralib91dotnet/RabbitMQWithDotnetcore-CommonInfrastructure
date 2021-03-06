﻿using System;
using Api.BackgroundServices;
using Api.Services;
using Infrastructure.EventBus.Abstractions;
using Infrastructure.EventBus.Configuration;
using Infrastructure.EventBus.QueueServices;
using Integration.Abstractions;
using Integration.Abstractions.QueueServices;
using Integration.Configuration;
using Integration.EventHandlers;
using Integration.QueueServices;
using Integration.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Api.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static void AddQueueSettings(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<ConnectionSettings>(configuration.GetSection(nameof(ConnectionSettings)));
            services.Configure<ConnectionStrings>(configuration.GetSection(nameof(ConnectionStrings)));

            services.Configure<DocumentPublishQueueSettings>(
                configuration.GetSection(nameof(DocumentPublishQueueSettings)));

            services.Configure<DocumentPublishUpdateQueueSettings>(
                configuration.GetSection(nameof(DocumentPublishUpdateQueueSettings)));

            services.Configure<DocumentPublishCancelQueueSettings>(
                configuration.GetSection(nameof(DocumentPublishCancelQueueSettings)));

            services.Configure<DocumentPublishResultQueueSettings>(
                configuration.GetSection(nameof(DocumentPublishResultQueueSettings)));
        }

        public static void AddDocumentPublicationServices(this IServiceCollection services)
        {
            services.AddSingleton<IPersistentConnection>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<DefaultPersistentConnection>>();
                var config = sp.GetRequiredService<IOptions<ConnectionSettings>>().Value;

                var factory = new ConnectionFactory
                {
                    HostName = config.HostName,
                    Port = config.Port ?? 5672,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(config.NetworkRecoveryInterval ?? 10),
                    RequestedHeartbeat = config.RequestedHeartbeat,
                    AutomaticRecoveryEnabled = true,
                    ContinuationTimeout = TimeSpan.FromSeconds(60)
                };

                //if (!String.IsNullOrEmpty(config.UserName))
                //{
                //    factory.UserName = config.UserName;
                //}

                //if (!String.IsNullOrEmpty(config.Password))
                //{
                //    factory.Password = config.Password;
                //}

                return new DefaultPersistentConnection(factory, logger, config.RetryConnectionAttempt ?? 5);
            });

            services.AddScoped<IDocumentPublishQueueService, DocumentPublishQueueService>();
            services.AddScoped<IDocumentPublishUpdateQueueService, DocumentPublishUpdateQueueService>();
            services.AddScoped<IDocumentPublishCancelQueueService, DocumentPublishPublishCancelQueueService>();
            services.AddScoped<IDocumentPublishResultQueueService, DocumentPublishResultQueueService>();
        }

        public static void AddDocumentEventMessageHandlers(this IServiceCollection services)
        {
            services.AddScoped<DocumentPublishMessageEventHandler>();
            services.AddScoped<DocumentPublishUpdateMessageEventHandler>();
            services.AddScoped<DocumentPublishCancelMessageEventHandler>();
            services.AddScoped<DocumentPublishResultMessageEventHandler>();
        } 

        public static void AddBackgroundWorkers(this IServiceCollection services)
        {
            services.AddHostedService<DocumentPublishBackgroundService>();
        }

        public static void AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IPublishDocumentTaskRepository, PublishDocumentTaskEfRepository>();
        }

        public static void AddServices(this IServiceCollection services)
        {
            services.AddSingleton<INotifyService, NotifyService>();
        }
    }
}