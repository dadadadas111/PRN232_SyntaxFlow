using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Models;
using MQTTnet;
using System.Text.Json;

namespace Services
{
    public class MqttService : BackgroundService, IMqttService
    {
        private readonly ILogger<MqttService> _logger;
        private readonly IConfiguration _configuration;
        private IMqttClient? _mqttClient;
        private MqttClientOptions? _options;

        public MqttService(ILogger<MqttService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Create MQTT client using the static factory method
                _mqttClient = new MqttClientFactory().CreateMqttClient();

                // Configure MQTT options
                _options = new MqttClientOptionsBuilder()
                    .WithTcpServer(_configuration["Mqtt:Server"] ?? "broker.emqx.io", 
                                   int.Parse(_configuration["Mqtt:Port"] ?? "1883"))
                    .WithClientId($"SyntaxFlow_Server_{Environment.MachineName}_{Guid.NewGuid():N}")
                    .WithCleanSession()
                    .Build();

                // Setup event handlers
                _mqttClient.ConnectedAsync += (args) =>
                {
                    _logger.LogInformation("MQTT Client connected successfully");
                    return Task.CompletedTask;
                };

                _mqttClient.DisconnectedAsync += async (args) =>
                {
                    _logger.LogWarning("MQTT Client disconnected: {Reason}", args.Reason);
                    
                    // Try to reconnect after 5 seconds
                    await Task.Delay(5000, cancellationToken);
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            await _mqttClient.ConnectAsync(_options, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to reconnect to MQTT broker");
                        }
                    }
                };

                // Connect to MQTT broker
                await _mqttClient.ConnectAsync(_options, cancellationToken);
                _logger.LogInformation("MQTT Service started and connected to broker");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start MQTT service");
                // Don't throw here - let the service start anyway for graceful degradation
            }

            await base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (_mqttClient?.IsConnected == true)
                {
                    await _mqttClient.DisconnectAsync(cancellationToken: cancellationToken);
                }
                _mqttClient?.Dispose();
                _logger.LogInformation("MQTT Service stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping MQTT service");
            }

            await base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Keep the service running and handle reconnection
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_mqttClient?.IsConnected != true)
                    {
                        _logger.LogWarning("MQTT client not connected, attempting to reconnect...");
                        if (_mqttClient != null && _options != null)
                        {
                            await _mqttClient.ConnectAsync(_options, stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in MQTT service execution loop");
                }

                await Task.Delay(30000, stoppingToken); // Check every 30 seconds
            }
        }

        public async Task PublishCommentAsync(CommentDto comment)
        {
            try
            {
                if (_mqttClient?.IsConnected != true)
                {
                    _logger.LogWarning("MQTT client not connected, skipping comment publish");
                    return;
                }

                var topic = $"syntaxflow/comments/{comment.BlockId}";
                var payload = new
                {
                    action = "create",
                    comment = comment,
                    timestamp = DateTime.UtcNow
                };

                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(JsonSerializer.Serialize(payload))
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build();

                await _mqttClient.PublishAsync(message);
                _logger.LogDebug("Published new comment to MQTT topic: {Topic}", topic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish comment to MQTT");
            }
        }

        public async Task PublishCommentUpdateAsync(CommentDto comment)
        {
            try
            {
                if (_mqttClient?.IsConnected != true)
                {
                    _logger.LogWarning("MQTT client not connected, skipping comment update publish");
                    return;
                }

                var topic = $"syntaxflow/comments/{comment.BlockId}";
                var payload = new
                {
                    action = "update",
                    comment = comment,
                    timestamp = DateTime.UtcNow
                };

                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(JsonSerializer.Serialize(payload))
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build();

                await _mqttClient.PublishAsync(message);
                _logger.LogDebug("Published comment update to MQTT topic: {Topic}", topic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish comment update to MQTT");
            }
        }

        public async Task PublishCommentDeleteAsync(int commentId)
        {
            try
            {
                if (_mqttClient?.IsConnected != true)
                {
                    _logger.LogWarning("MQTT client not connected, skipping comment delete publish");
                    return;
                }

                // We need to publish to a general topic since we don't know the blockId
                var topic = "syntaxflow/comments/deleted";
                var payload = new
                {
                    action = "delete",
                    commentId = commentId,
                    timestamp = DateTime.UtcNow
                };

                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(JsonSerializer.Serialize(payload))
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build();

                await _mqttClient.PublishAsync(message);
                _logger.LogDebug("Published comment deletion to MQTT topic: {Topic}", topic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish comment deletion to MQTT");
            }
        }
    }
}