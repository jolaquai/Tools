using System.Data;
using System.Diagnostics;

namespace Monitors.Shared.Services;

public class IntegratedGraphicsChipProcessor : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            UninstallIntegratedGraphicsChip();
            await Task.Delay(60_000, stoppingToken);
        }
    }

    private static volatile bool graphicsChipProcessingRunning;
    /// <summary>
    /// Uninstalls the integrated graphics chip device and drivers.
    /// </summary>
    private static void UninstallIntegratedGraphicsChip()
    {
        if (graphicsChipProcessingRunning)
        {
            return;
        }

        graphicsChipProcessingRunning = true;

        try
        {
            var p = Process.Start(new ProcessStartInfo()
            {
                FileName = "pnputil.exe",
                Arguments = @"/enum-devices /class Display",
                RedirectStandardOutput = true
            });
            p.WaitForExit();
            var t1 = p.StandardOutput.ReadToEnd().Trim();

            // Parse the command's output into a list of devices
            var split = t1.Split(Environment.NewLine + Environment.NewLine)[1..];
            var devices = split.Select(block =>
            {
                var lines = block.Split(Environment.NewLine);
                var device = new Device();
                foreach (var line in lines)
                {
                    var split = line.Split(": ");
                    var key = split[0].Trim();
                    var value = split[1].Trim();
                    switch (key)
                    {
                        case "Instance ID":
                        {
                            device.InstanceID = value;
                            break;
                        }
                        case "Device Description":
                        {
                            device.DeviceDescription = value;
                            break;
                        }
                        case "Class Name":
                        {
                            device.ClassName = value;
                            break;
                        }
                        case "Class GUID":
                        {
                            device.ClassGUID = value;
                            break;
                        }
                        case "Manufacturer Name":
                        {
                            device.ManufacturerName = value;
                            break;
                        }
                        case "Status":
                        {
                            device.Status = value;
                            break;
                        }
                        case "Driver Name":
                        {
                            device.DriverName = value;
                            break;
                        }
                    }
                }
                return device;
            }).ToArray();

            var device = devices.SingleOrDefault(device => device.DeviceDescription.Equals("AMD Radeon(TM) Graphics", StringComparison.Ordinal));
            if (device is null)
            {
                return;
            }

            // Uninstall the device
            var uninstallation = Process.Start(new ProcessStartInfo()
            {
                FileName = "pnputil.exe",
                Arguments = $"/remove-device \"{device.InstanceID}\"",
                RedirectStandardOutput = true
            });
            p.WaitForExit();
        }
        finally
        {
            graphicsChipProcessingRunning = false;
        }
    }
    #region private record class Device
    private record class Device
    {
        public string InstanceID
        {
            get; set;
        }
        public string DeviceDescription
        {
            get; set;
        }
        public string ClassName
        {
            get; set;
        }
        public string ClassGUID
        {
            get; set;
        }
        public string ManufacturerName
        {
            get; set;
        }
        public string Status
        {
            get; set;
        }
        public string DriverName
        {
            get; set;
        }
    }
    #endregion
}
