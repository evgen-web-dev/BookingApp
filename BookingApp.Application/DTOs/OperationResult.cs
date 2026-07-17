namespace BookingApp.Application.DTOs;

public record OperationResult
{
    public bool Succeeded { get; protected init;  }
    public IReadOnlyList<string> Errors { get; protected init; } = [];

    protected OperationResult() {}
    
    public static OperationResult Success () => new OperationResult { Succeeded = true };
    
    public static OperationResult Failure(IReadOnlyList<string> errors) => new OperationResult { Succeeded = false, Errors = errors };
}

public record OperationResult<TValue> : OperationResult
{
    public TValue? Value { get; protected init; }

    private OperationResult() { }

    public static OperationResult<TValue> Success(TValue value) => new OperationResult<TValue> { Succeeded = true, Value = value };
    
    public static OperationResult<TValue> Failure(IReadOnlyList<string> errors) => new OperationResult<TValue> { Succeeded = false, Errors = errors };
}