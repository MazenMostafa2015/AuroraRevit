using System;
using System.Net;
using System.Net.Sockets;

namespace AuroraRevit.AiProxy;

public static class ProxyPortResolver
{
    public static string ResolveUrl(string[] args)
    {
        var requestedUrl = GetRequestedUrl(args) ?? "http://localhost:5000";
        if (!Uri.TryCreate(requestedUrl, UriKind.Absolute, out var requestedUri))
        {
            throw new InvalidOperationException($"Invalid proxy URL: {requestedUrl}");
        }

        if (IsPortAvailable(requestedUri.Port))
        {
            return requestedUrl.TrimEnd('/');
        }

        var fallback = new UriBuilder(requestedUri) { Port = 5001 }.Uri.ToString().TrimEnd('/');
        if (IsPortAvailable(5001))
        {
            return fallback;
        }

        throw new InvalidOperationException(
            $"Ports {requestedUri.Port} and 5001 are unavailable. Stop the conflicting process and retry.");
    }

    private static string? GetRequestedUrl(string[] args)
    {
        for (var index = 0; index < args.Length; index++)
        {
            if (string.Equals(args[index], "--urls", StringComparison.OrdinalIgnoreCase)
                && index + 1 < args.Length)
            {
                return args[index + 1];
            }

            if (args[index].StartsWith("--urls=", StringComparison.OrdinalIgnoreCase))
            {
                return args[index].Substring("--urls=".Length);
            }
        }

        return null;
    }

    private static bool IsPortAvailable(int port)
    {
        try
        {
            using var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }
}
