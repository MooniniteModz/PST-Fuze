using System;
using System.Runtime.Versioning;

namespace PSTFuze.Services;

public class RedemptionLoader : IDisposable
{
    private bool _disposed;

    public RedemptionLoader()
    {
        // Ensure proper COM initialization
        try
        {
            if (OperatingSystem.IsWindows())
            {
                InitializeRedemption();
            }
            else
            {
                throw new PlatformNotSupportedException("Redemption is only supported on Windows.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing Redemption: {ex}");
            throw;
        }
    }

    [SupportedOSPlatform("windows")]
    private void InitializeRedemption()
    {
        Type.GetTypeFromProgID("Redemption.RDOSession");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            // Cleanup COM resources
            _disposed = true;
        }
    }
}