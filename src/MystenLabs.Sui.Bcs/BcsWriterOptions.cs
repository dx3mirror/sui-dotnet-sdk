namespace MystenLabs.Sui.Bcs;

/// <summary>
/// Options for <see cref="BcsWriter"/> buffer allocation and growth.
/// </summary>
public sealed class BcsWriterOptions
{
    /// <summary>
    /// Default initial buffer size in bytes (1 KB).
    /// </summary>
    public const int DefaultInitialSize = 1024;

    /// <summary>
    /// Default size to allocate when the buffer needs to grow.
    /// </summary>
    public const int DefaultAllocateSize = 1024;

    /// <summary>
    /// Initial size (in bytes) of the buffer. Default is <see cref="DefaultInitialSize"/>.
    /// </summary>
    public int InitialSize { get; set; } = DefaultInitialSize;

    /// <summary>
    /// Maximum size (in bytes) the buffer is allowed to grow to. Use <see cref="int.MaxValue"/> for no limit.
    /// </summary>
    public int MaxSize { get; set; } = int.MaxValue;

    /// <summary>
    /// Extra bytes to allocate when the buffer must grow. Default is <see cref="DefaultAllocateSize"/>.
    /// </summary>
    public int AllocateSize { get; set; } = DefaultAllocateSize;
}
