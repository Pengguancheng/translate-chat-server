using Newtonsoft.Json;

namespace TranslateChat.Domain.Procedure;

/// <summary>
/// 處理流程上下文
/// </summary>
public interface IProcedureContext
{
    Exception? Exception { get; set; }

    bool IsSuccess { get; }

    bool TryGetException<T>(out T? ex) where T : Exception;

    bool TryGetException(out Exception? ex);
}

/// <summary>
/// 基本型態
/// </summary>
public abstract class BaseProcedureContext : IProcedureContext
{
    [JsonIgnore]
    public Exception? Exception { get; set; }

    public bool IsSuccess => Exception == null;

    public bool TryGetException(out Exception? ex)
    {
        ex = null;
        if (IsSuccess)
        {
            return false;
        }

        ex = Exception;
        return true;
    }

    public bool TryGetException<T>(out T? ex) where T : Exception
    {
        if (Exception is T typedException)
        {
            ex = typedException;
            return true;
        }

        ex = null;
        return false;
    }

    public override string ToString()
    {
        if (!IsSuccess)
        {
            return $"Exception: {Exception?.Message}, From ctx: {JsonConvert.SerializeObject(this)}";
        }
        return $"Success: {JsonConvert.SerializeObject(this)}";
    }
}

public abstract class BaseProcedureException<TCode> : Exception
{
    public TCode Code { get; }

    public string Stage { get; }

    protected BaseProcedureException(TCode code, string stage, string message) : base(
        $"[{stage}][{code.ToString()}]: {message}")
    {
        Code = code;
        Stage = stage;
    }

    protected BaseProcedureException(TCode code, string stage, string message, Exception innerException) : base(message,
        innerException)
    {
        Code = code;
        Stage = stage;
    }
}