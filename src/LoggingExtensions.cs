using Microsoft.Extensions.Logging;

namespace Pinecone;

internal static partial class LoggingExtensions
{
    [LoggerMessage(1, LogLevel.Trace, "{operationName} started.")]
    public static partial void OperationStarted(this ILogger logger, string operationName);

    [LoggerMessage(2, LogLevel.Debug, "{operationName} completed.")]
    public static partial void OperationCompleted (this ILogger logger, string operationName);

    [LoggerMessage(3, LogLevel.Debug, "{operationName} completed - {outcome}")]
    public static partial void OperationCompletedWithOutcome(this ILogger logger, string operationName, string outcome);

    [LoggerMessage(4, LogLevel.Error, "{operationName} failed: {reason}")]
    public static partial void OperationFailed(this ILogger logger, string operationName, string reason);
}
