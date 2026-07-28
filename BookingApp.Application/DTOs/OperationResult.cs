namespace BookingApp.Application.DTOs;

public record OperationResult
{
    public bool Succeeded { get; protected init;  }
    public IReadOnlyList<string> Errors { get; protected init; } = [];

    protected OperationResult() {}
    
    public static OperationResult Success () => new OperationResult { Succeeded = true };
    
    public static OperationResult Failure(IReadOnlyList<string> errors) => new OperationResult { Succeeded = false, Errors = errors };
}

public record OperationResult<TValue> : OperationResult where TValue : notnull
{
    private TValue? _value;

    private OperationResult() { }
    
    // OperationResult<T> is guaranteed to have a non-null _value returned only when Succeeded=true, or throw otherwise
    // (non-nullability is guaranteed in OperationResult<T>.Success)
    public TValue GetValueOrThrow() => Succeeded ? _value! : throw new InvalidOperationException("Cannot access the value when the operation did not succeed.");

    // Throws ArgumentNullException if null is passed (including OperationResult<TValue>.Success(null!))
    // to guarantee non-nullability of _value for result with Succeeded=true
    public static OperationResult<TValue> Success(TValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new OperationResult<TValue> { Succeeded = true, _value = value };
    }

    public static OperationResult<TValue> Failure(IReadOnlyList<string> errors) => new OperationResult<TValue> { Succeeded = false, Errors = errors };
}