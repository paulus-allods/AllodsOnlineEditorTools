using Microsoft.Extensions.Logging;

namespace ClientResources.Tests;

/// <summary>An <see cref="ILogger"/> that captures warning messages so tests can assert on them.</summary>
internal sealed class CollectingLogger : ILogger
{
    public List<string> Warnings { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (logLevel == LogLevel.Warning)
        {
            Warnings.Add(formatter(state, exception));
        }
    }
}
