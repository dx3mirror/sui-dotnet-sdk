namespace MystenLabs.Sui.JsonRpc;

/// <summary>
/// Base type for Sui RPC/transport errors.
/// </summary>
public class SuiRpcException : Exception
{
    /// <summary>
    /// Creates an exception with the given message.
    /// </summary>
    public SuiRpcException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Creates an exception with the given message and inner exception.
    /// </summary>
    public SuiRpcException(string message, Exception inner)
        : base(message, inner)
    {
    }
}

/// <summary>
/// JSON-RPC error response (error code and message from the server).
/// </summary>
public sealed class SuiJsonRpcException : SuiRpcException
{
    private const int CodeParseError = -32700;
    private const int CodeOversizedRequest = -32701;
    private const int CodeOversizedResponse = -32702;
    private const int CodeInvalidRequest = -32600;
    private const int CodeMethodNotFound = -32601;
    private const int CodeInvalidParams = -32602;
    private const int CodeInternalError = -32603;
    private const int CodeServerBusy = -32604;
    private const int CodeCallExecutionFailed = -32000;
    private const int CodeUnknownError = -32001;
    private const int CodeTransactionExecutionClientError = -32002;
    private const int CodeSubscriptionClosed = -32003;
    private const int CodeSubscriptionClosedWithError = -32004;
    private const int CodeBatchesNotSupported = -32005;
    private const int CodeTooManySubscriptions = -32006;
    private const int CodeTransientError = -32050;

    /// <summary>
    /// JSON-RPC error code (e.g. -32603 for InternalError).
    /// </summary>
    public int Code { get; }

    /// <summary>
    /// Error type label derived from code when known.
    /// </summary>
    public string ErrorType { get; }

    /// <summary>
    /// Creates an exception from the RPC error payload.
    /// </summary>
    public SuiJsonRpcException(string message, int code)
        : base(message)
    {
        Code = code;
        ErrorType = GetErrorType(code);
    }

    private static string GetErrorType(int code)
    {
        return code switch
        {
            CodeParseError => "ParseError",
            CodeOversizedRequest => "OversizedRequest",
            CodeOversizedResponse => "OversizedResponse",
            CodeInvalidRequest => "InvalidRequest",
            CodeMethodNotFound => "MethodNotFound",
            CodeInvalidParams => "InvalidParams",
            CodeInternalError => "InternalError",
            CodeServerBusy => "ServerBusy",
            CodeCallExecutionFailed => "CallExecutionFailed",
            CodeUnknownError => "UnknownError",
            CodeTransactionExecutionClientError => "TransactionExecutionClientError",
            CodeSubscriptionClosed => "SubscriptionClosed",
            CodeSubscriptionClosedWithError => "SubscriptionClosedWithError",
            CodeBatchesNotSupported => "BatchesNotSupported",
            CodeTooManySubscriptions => "TooManySubscriptions",
            CodeTransientError => "TransientError",
            _ => "ServerError"
        };
    }
}

/// <summary>
/// HTTP-level error (non-2xx status).
/// </summary>
public sealed class SuiHttpStatusException : SuiRpcException
{
    /// <summary>
    /// HTTP status code (e.g. 404, 500).
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// HTTP status text.
    /// </summary>
    public string StatusText { get; }

    /// <summary>
    /// Creates an exception for the given HTTP response.
    /// </summary>
    public SuiHttpStatusException(string message, int statusCode, string statusText)
        : base(message)
    {
        StatusCode = statusCode;
        StatusText = statusText ?? string.Empty;
    }
}
