using Prometheus;

namespace GatewayApi.Extensions
{
    public static class ChatMetrics
    {
        // Gauge — текущие активные сессии пользователей
        public static readonly Gauge ActiveUserSessions = Metrics.CreateGauge(
            "messenger_active_user_sessions",
            "Current number of active user session");
    }
}
