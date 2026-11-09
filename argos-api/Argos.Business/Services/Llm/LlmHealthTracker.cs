using System.Collections.Concurrent;
using Argos.Business.Services.Notifications;
using Argos.Common.Configuration;
using Argos.Common.Time;
using Argos.Entities;
using Microsoft.Extensions.Logging;

namespace Argos.Business.Services.Llm;

public sealed class LlmHealthTracker(
    INotificationService notifications,
    ILlmConfig config,
    IClock clock,
    ILogger<LlmHealthTracker> log) : ILlmHealthTracker
{
    private readonly ConcurrentDictionary<string, LlmHealthState> _states = new();

    public bool IsAvailable(LlmSelection selection)
    {
        if (!_states.TryGetValue(Key(selection), out var state)) return true;
        lock (state) return state.RetryAfter is null || state.RetryAfter <= clock.UtcNow;
    }

    public async Task RecordSuccessAsync(LlmSelection selection, long latencyMs)
    {
        var state = _states.GetOrAdd(Key(selection), _ => new LlmHealthState());
        bool recovered;
        lock (state)
        {
            recovered = state.Status == LlmHealthState.Down;
            var now = clock.UtcNow;
            state.Status = LlmHealthState.Up;
            state.LastError = null;
            state.RetryAfter = null;
            state.LastCheckedAt = now;
            state.LastSuccessAt = now;
            state.LatencyMs = latencyMs;
        }
        if (recovered) await NotifyAsync(selection, available: true, detail: null);
    }

    public async Task RecordFailureAsync(LlmSelection selection, string error, long latencyMs)
    {
        var state = _states.GetOrAdd(Key(selection), _ => new LlmHealthState());
        bool wentDown;
        lock (state)
        {
            wentDown = state.Status != LlmHealthState.Down;
            var now = clock.UtcNow;
            state.Status = LlmHealthState.Down;
            state.LastError = error;
            state.RetryAfter = now.AddSeconds(config.LlmCooldownSeconds);
            state.LastCheckedAt = now;
            state.LatencyMs = latencyMs;
        }
        if (wentDown) await NotifyAsync(selection, available: false, detail: error);
    }

    public LlmProviderHealth Describe(LlmSelection selection, string role, bool configured)
    {
        if (!configured)
            return new(selection.Provider, selection.Model, role, false, "unconfigured", false, null, null, null, null, null);
        if (!_states.TryGetValue(Key(selection), out var state))
            return new(selection.Provider, selection.Model, role, true, LlmHealthState.Unknown, true, null, null, null, null, null);
        lock (state)
        {
            var coolingDown = state.RetryAfter is { } retry && retry > clock.UtcNow;
            return new(selection.Provider, selection.Model, role, true, state.Status, !coolingDown, state.LastError,
                Format(state.LastCheckedAt), Format(state.LastSuccessAt), state.LatencyMs,
                coolingDown ? Format(state.RetryAfter) : null);
        }
    }

    // Un aviso que no se pudo guardar (Qdrant caído) no debe cortar la generación.
    private async Task NotifyAsync(LlmSelection selection, bool available, string? detail)
    {
        try
        {
            await notifications.NotifyLlmStatusChangeAsync(selection, available, detail);
        }
        catch (Exception ex)
        {
            log.LogWarning("[llm-health] no se pudo guardar el aviso de {Provider}: {Error}", selection.Provider, ex.Message);
        }
    }

    private static string Key(LlmSelection selection) => $"{selection.Provider}:{selection.Model}";

    private static string? Format(DateTimeOffset? value) =>
        value is { } v ? IsoDateTime.Format(v.UtcDateTime, aware: true) : null;
}
