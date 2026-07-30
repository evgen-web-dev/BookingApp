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
    public TValue Value { get; private set; } = default!;

    private OperationResult() { }
    
    public static OperationResult<TValue> Success(TValue value)
    {
        /*
         Checking for null here to make sure that when Succeeded=true - returned instance of OperationResult always have non-null Value,
         to prevent cases like:
         
            OperationResult<string>.Success(null);
            or
            OperationResult<string>.Success(null!);
            or
            string? resultStr = null;
            OperationResult<string>.Success(resultStr);
            
            where consumer of OperationResult instance will get/use Value=null when relying on Succeeded=true */
        return value != null
            ? new OperationResult<TValue> { Succeeded = true, Value = value }
            : new OperationResult<TValue> { Succeeded = false };
    }

    public new static OperationResult<TValue> Failure(IReadOnlyList<string> errors) => new OperationResult<TValue> { Succeeded = false, Errors = errors };
}