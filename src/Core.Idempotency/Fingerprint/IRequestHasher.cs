namespace Core.Idempotency.Fingerprint;

internal interface IRequestHasher
{
    string Name { get; }

    string Compute(string input);
}