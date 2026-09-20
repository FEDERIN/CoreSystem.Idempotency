namespace Core.Idempotency.DependencyInjection;

internal static class IdempotencyMessages
{
    public const string MissingRegistration =
        "Core.Idempotency has not been registered. Call services.AddCoreIdempotency(...) before app.UseCoreIdempotency().";
}