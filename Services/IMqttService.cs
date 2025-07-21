using Models;

namespace Services
{
    public interface IMqttService
    {
        /// <summary>
        /// Publish a new comment to MQTT for real-time updates
        /// </summary>
        Task PublishCommentAsync(CommentDto comment);

        /// <summary>
        /// Publish comment update to MQTT for real-time updates
        /// </summary>
        Task PublishCommentUpdateAsync(CommentDto comment);

        /// <summary>
        /// Publish comment deletion to MQTT for real-time updates
        /// </summary>
        Task PublishCommentDeleteAsync(int commentId);

        /// <summary>
        /// Start the MQTT service connection
        /// </summary>
        Task StartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stop the MQTT service connection
        /// </summary>
        Task StopAsync(CancellationToken cancellationToken = default);
    }
}
