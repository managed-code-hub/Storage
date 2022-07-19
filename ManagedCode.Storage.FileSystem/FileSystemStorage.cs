﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.MimeTypes;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.FileSystem.Options;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Storage.FileSystem;

public class FileSystemStorage : BaseStorage<FileSystemStorageOptions>, IFileSystemStorage
{
    private readonly string _path;
    private readonly Dictionary<string, FileStream> _lockedFiles = new();

    public FileSystemStorage(FileSystemStorageOptions options) : base(options)
    {
        _path = StorageOptions.BaseFolder ?? Environment.CurrentDirectory;
    }

    protected override async Task<Result> CreateContainerInternalAsync(CancellationToken cancellationToken = default)
    {
        await Task.Yield();

        if (!Directory.Exists(_path))
        {
            Directory.CreateDirectory(_path);
        }

        return Result.Succeed();
    }

    public override async Task<Result> RemoveContainerAsync(CancellationToken cancellationToken = default)
    {
        await Task.Yield();

        if (Directory.Exists(_path))
        {
            Directory.Delete(_path);
        }

        return Result.Succeed();
    }

    protected override async Task<Result<string>> UploadInternalAsync(Stream stream, UploadOptions options,
        CancellationToken cancellationToken = default)
    {
        await EnsureContainerExist();
        var filePath = Path.Combine(_path, options.FileName);

        using (var fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write))
        {
            stream.Seek(0, SeekOrigin.Begin);
            await stream.CopyToAsync(fs, 81920, cancellationToken);
        }

        return Result<string>.Succeed(filePath);
    }

    protected override async Task<Result<LocalFile>> DownloadInternalAsync(LocalFile localFile, string blob,
        CancellationToken cancellationToken = default)
    {
        await EnsureContainerExist();

        var filePath = Path.Combine(_path, blob);

        if (File.Exists(filePath))
        {
            return Result<LocalFile>.Succeed(new LocalFile(filePath));
        }

        return Result<LocalFile>.Fail();
    }

    public override async Task<Result<bool>> DeleteAsync(string blob, CancellationToken cancellationToken = default)
    {
        await EnsureContainerExist();

        var filePath = Path.Combine(_path, blob);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            return Result<bool>.Succeed(true);
        }

        return Result<bool>.Succeed(false);
    }

    public override async Task<Result<bool>> ExistsAsync(string blob, CancellationToken cancellationToken = default)
    {
        await EnsureContainerExist();
        var filePath = Path.Combine(_path, blob);
        return Result<bool>.Succeed(File.Exists(filePath));
    }

    public override async Task<Result<BlobMetadata>> GetBlobMetadataAsync(string blob, CancellationToken cancellationToken = default)
    {
        await EnsureContainerExist();
        var fileInfo = new FileInfo(Path.Combine(_path, blob));
        if (fileInfo.Exists)
        {
            var result = new BlobMetadata
            {
                Name = fileInfo.Name,
                Uri = new Uri(Path.Combine(_path, blob)),
                MimeType = MimeHelper.GetMimeType(fileInfo.Extension),
                Length = fileInfo.Length
            };

            return Result<BlobMetadata>.Succeed(result);
        }

        return Result<BlobMetadata>.Fail
            ();
    }

    public override async IAsyncEnumerable<BlobMetadata> GetBlobMetadataListAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await EnsureContainerExist();
        foreach (var file in Directory.EnumerateFiles(_path))
        {
            var blobMetadata = await GetBlobMetadataAsync(file, cancellationToken);

            if (blobMetadata.IsSuccess)
            {
                yield return blobMetadata.Value!;
            }
        }
    }

    public override async Task<Result> SetLegalHoldAsync(string blob, bool hasLegalHold, CancellationToken cancellationToken = default)
    {
        await EnsureContainerExist();
        if (hasLegalHold && !_lockedFiles.ContainsKey(blob))
        {
            var file = await DownloadAsync(blob, cancellationToken);

            if (file.IsError)
                return Result.Fail();

            var fileStream = File.OpenRead(file.Value!.FilePath); // Opening with FileAccess.Read only
            fileStream.Lock(0, fileStream.Length); // Attempting to lock a region of the read-only file

            _lockedFiles.Add(blob, fileStream);

            return Result.Succeed();
        }

        if (!hasLegalHold)
        {
            if (_lockedFiles.ContainsKey(blob))
            {
                _lockedFiles[blob].Unlock(0, _lockedFiles[blob].Length);
                _lockedFiles[blob].Dispose();
                _lockedFiles.Remove(blob);
            }
        }

        return Result.Succeed();
    }

    public override async Task<Result<bool>> HasLegalHoldAsync(string blob, CancellationToken cancellationToken = default)
    {
        await EnsureContainerExist();
        return Result<bool>.Succeed(_lockedFiles.ContainsKey(blob));
    }
}