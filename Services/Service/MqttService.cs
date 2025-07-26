using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Models;
using MQTTnet;
using Services.Interface;
using System.Text.Json;

namespace Services.Service
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
                var server = _configuration["Mqtt:Server"] ?? "broker.emqx.io";
                var port = int.Parse(_configuration["Mqtt:Port"] ?? "1883");
                var clientId = $"SyntaxFlow_Server_{Environment.MachineName}_{Guid.NewGuid():N}";
                
                _logger.LogInformation("Starting MQTT Service - Server: {Server}, Port: {Port}, ClientId: {ClientId}", 
                    server, port, clientId);

                // Create MQTT client using the static factory method
                _mqttClient = new MqttClientFactory().CreateMqttClient();

                // Configure MQTT options
                _options = new MqttClientOptionsBuilder()
                    .WithTcpServer(server, port)
                    .WithClientId(clientId)
                    .WithCleanSession()
                    .Build();

                // Setup event handlers
                _mqttClient.ConnectedAsync += (args) =>
                {
                    _logger.LogInformation("✅ MQTT Client connected successfully to {Server}:{Port}", server, port);
                    return Task.CompletedTask;
                };

                _mqttClient.DisconnectedAsync += async (args) =>
                {
                    _logger.LogWarning("❌ MQTT Client disconnected: {Reason}", args.Reason);
                    
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
                _logger.LogInformation("🔄 Connecting to MQTT broker...");
                await _mqttClient.ConnectAsync(_options, cancellationToken);
                _logger.LogInformation("✅ MQTT Service started and connected to broker");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to start MQTT service");
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
                    _logger.LogWarning("🔌 MQTT client not connected, skipping comment publish for comment ID: {CommentId}", comment.Id);
                    return;
                }

                var topic = $"syntaxflow/comments/{comment.BlockId}";
                var payload = new
                {
                    action = "create",
                    comment,
                    timestamp = DateTime.UtcNow
                };

                var payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
                });
                _logger.LogInformation("📤 Publishing new comment to MQTT - Topic: {Topic}, CommentId: {CommentId}, BlockId: {BlockId}", 
                    topic, comment.Id, comment.BlockId);
                _logger.LogDebug("📤 MQTT Payload: {Payload}", payloadJson);

                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(payloadJson)
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build();

                await _mqttClient.PublishAsync(message);
                _logger.LogInformation("✅ Successfully published new comment to MQTT topic: {Topic}", topic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to publish comment to MQTT - CommentId: {CommentId}", comment.Id);
            }
        }

        public async Task PublishCommentUpdateAsync(CommentDto comment)
        {
            try
            {
                if (_mqttClient?.IsConnected != true)
                {
                    _logger.LogWarning("🔌 MQTT client not connected, skipping comment update publish for comment ID: {CommentId}", comment.Id);
                    return;
                }

                var topic = $"syntaxflow/comments/{comment.BlockId}";
                var payload = new
                {
                    action = "update",
                    comment,
                    timestamp = DateTime.UtcNow
                };

                var payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
                });
                _logger.LogInformation("📤 Publishing comment update to MQTT - Topic: {Topic}, CommentId: {CommentId}, BlockId: {BlockId}", 
                    topic, comment.Id, comment.BlockId);
                _logger.LogDebug("📤 MQTT Payload: {Payload}", payloadJson);

                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(payloadJson)
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build();

                await _mqttClient.PublishAsync(message);
                _logger.LogInformation("✅ Successfully published comment update to MQTT topic: {Topic}", topic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to publish comment update to MQTT - CommentId: {CommentId}", comment.Id);
            }
        }

        public async Task PublishCommentDeleteAsync(int commentId)
        {
            try
            {
                if (_mqttClient?.IsConnected != true)
                {
                    _logger.LogWarning("🔌 MQTT client not connected, skipping comment delete publish for comment ID: {CommentId}", commentId);
                    return;
                }

                // We need to publish to a general topic since we don't know the blockId
                var topic = "syntaxflow/comments/deleted";
                var payload = new
                {
                    action = "delete",
                    commentId,
                    timestamp = DateTime.UtcNow
                };

                var payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
                });
                _logger.LogInformation("📤 Publishing comment deletion to MQTT - Topic: {Topic}, CommentId: {CommentId}", 
                    topic, commentId);
                _logger.LogDebug("📤 MQTT Payload: {Payload}", payloadJson);

                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(payloadJson)
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build();

                await _mqttClient.PublishAsync(message);
                _logger.LogInformation("✅ Successfully published comment deletion to MQTT topic: {Topic}", topic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to publish comment deletion to MQTT - CommentId: {CommentId}", commentId);
            }
        }
    }
}