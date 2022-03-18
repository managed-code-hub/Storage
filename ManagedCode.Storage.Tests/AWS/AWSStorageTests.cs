﻿using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Storage.Aws;
using ManagedCode.Storage.Aws.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ManagedCode.Storage.Tests.AWS;

public class AWSStorageTests : StorageBaseTests
{
    public AWSStorageTests()
    {
        var services = new ServiceCollection();

        services.AddAWSStorage(opt =>
        {
            opt.PublicKey = "localkey";
            opt.SecretKey = "localsecret";
            opt.Bucket = "my-docs-1";
        });

        var provider = services.BuildServiceProvider();

        Storage = provider.GetService<IAWSStorage>();
    }

    [Fact]
    public void WhenDIInitialized()
    {
        DIInitialized();
    }

    [Fact]
    public async Task WhenSingleBlobExistsIsCalled()
    {
        await SingleBlobExistsIsCalled("a.txt");
    }

    [Fact]
    public async Task WhenDownloadAsyncIsCalled()
    {
        var stream = await Storage.DownloadAsStreamAsync("a.txt");
        using var sr = new StreamReader(stream, Encoding.UTF8);

        var content = sr.ReadToEnd();

        content.Should().NotBeNull();
    }

    [Fact]
    public async Task WhenDownloadAsyncToFileIsCalled()
    {
        await DownloadAsyncToFileIsCalled("a.txt");
    }

    [Fact]
    public async Task WhenUploadAsyncIsCalled()
    {
        await UploadAsyncIsCalled("d.txt");
    }

    [Fact]
    public async Task WhenDeleteAsyncIsCalled()
    {
        await DeleteAsyncIsCalled("a.txt");
    }

    [Fact]
    protected async Task WhenGetBlobListAsyncIsCalled()
    {
        await GetBlobListAsyncIsCalled();
    }
}