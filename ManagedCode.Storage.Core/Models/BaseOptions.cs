using System;

namespace ManagedCode.Storage.Core.Models;

public abstract class BaseOptions
{
    public string FileName { get; set; } = $"{Guid.NewGuid():N}";
    public string? Directory { get; set; }

    // TODO: Check this
    public string FullPath => string.IsNullOrWhiteSpace(Directory) ? FileName : $"{Directory}/{FileName}";
}