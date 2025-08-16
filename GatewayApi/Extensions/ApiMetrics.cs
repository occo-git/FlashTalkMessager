using Prometheus;

namespace GatewayApi.Extensions
{
    public static class ApiMetrics
    {
        // Гистограмма времени обработки сообщений (в миллисекундах)
        public static readonly Histogram MessageProcessingDuration = Metrics.CreateHistogram(
            "messenger_message_processing_duration_ms",
            "Message processing time in milliseconds",
            new HistogramConfiguration
            {
                Buckets = Histogram.LinearBuckets(start: 1, width: 2, count: 100) // 1, 3, 5 ... 201 ms
            });

        // Счётчик новых сообщений (увеличивается при отправке каждого сообщения)
        public static readonly Counter NewMessagesTotal = Metrics.CreateCounter(
            "messenger_new_messages_total",
            "Total number of messages sent");

        // Счётчик ошибок отправки сообщений
        public static readonly Counter MessageSendErrorsTotal = Metrics.CreateCounter(
            "messenger_message_send_errors_total",
            "Number of errors when sending messages");
    }
}
