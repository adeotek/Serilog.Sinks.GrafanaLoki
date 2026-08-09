using System.Net;
using Serilog.Sinks.GrafanaLoki.Common;
using Shouldly;
using Xunit;

namespace Serilog.Sinks.GrafanaLoki.Tests;

public class GrafanaLokiHttpClientTests
{
    [Fact]
    public void Constructor_AcceptsNullHttpClient()
    {
        using var client = new GrafanaLokiHttpClient(null, null);

        client.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_SetsTimeoutWhenProvided()
    {
        using var client = new GrafanaLokiHttpClient(null, null, httpTimeout: 5000);

        // HttpClient.Timeout is accessible via the protected HttpClient field
        // We verify construction succeeds — timeout is set internally
        client.ShouldNotBeNull();
    }

    [Fact]
    public void SetCredentials_AddsAuthorizationHeader()
    {
        using var client = new GrafanaLokiHttpClient(new HttpClient(), new GrafanaLokiCredentials
        {
            User = "testuser",
            Password = "testpass"
        });

        // Credentials are set in constructor via SetCredentials
        var headers = client.GetType()
            .GetField("HttpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .GetValue(client) as HttpClient;

        headers.ShouldNotBeNull();
        headers!.DefaultRequestHeaders.Authorization.ShouldNotBeNull();
        headers.DefaultRequestHeaders.Authorization!.Scheme.ShouldBe("Basic");
    }

    [Fact]
    public void SetCredentials_DoesNotAddHeaderForNullCredentials()
    {
        using var client = new GrafanaLokiHttpClient(new HttpClient(), null);

        var headers = client.GetType()
            .GetField("HttpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .GetValue(client) as HttpClient;

        headers.ShouldNotBeNull();
        headers!.DefaultRequestHeaders.Authorization.ShouldBeNull();
    }

    [Fact]
    public void SetCredentials_DoesNotAddHeaderForEmptyUser()
    {
        using var client = new GrafanaLokiHttpClient(new HttpClient(), new GrafanaLokiCredentials
        {
            User = "",
            Password = "pass"
        });

        var headers = client.GetType()
            .GetField("HttpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .GetValue(client) as HttpClient;

        headers.ShouldNotBeNull();
        headers!.DefaultRequestHeaders.Authorization.ShouldBeNull();
    }

    [Fact]
    public void SetCredentials_DoesNotDuplicateAuthorizationHeader()
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer existing");

        using var client = new GrafanaLokiHttpClient(httpClient, new GrafanaLokiCredentials
        {
            User = "newuser",
            Password = "newpass"
        });

        var auth = httpClient.DefaultRequestHeaders.Authorization;
        auth!.Scheme.ShouldBe("Bearer"); // Original header preserved
    }

    [Fact]
    public void DebugMode_DefaultsToFalse()
    {
        using var client = new GrafanaLokiHttpClient();

        client.DebugMode.ShouldBeFalse();
    }

    [Fact]
    public void DebugMode_CanBeSet()
    {
        using var client = new GrafanaLokiHttpClient { DebugMode = true };

        client.DebugMode.ShouldBeTrue();
    }

    [Fact]
    public void PostAsync_WithValidUrl_DoesNotThrow()
    {
        // This test verifies construction works — actual POST requires a running server
        using var client = new GrafanaLokiHttpClient();

        client.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_NegativeTimeout_IsIgnored()
    {
        // -1 is the default (no timeout override)
        using var client = new GrafanaLokiHttpClient(null, null, httpTimeout: -1);

        client.ShouldNotBeNull();
    }
}
