using NAudio.Wave;

namespace AudioStreamer;

public static class AudioStreamer
{
    public static async Task Main()
    {
        var outputDevices = new List<IWavePlayer>();

        

        var input = new MediaFoundationReader("path/to/file.mp3"); // replace with actual file path

        // get all available output devices
        var outputDeviceCount = WaveOut.DeviceCount;
        for (int i = 0; i < outputDeviceCount; i++)
        {
            var outputDevice = new WaveOutEvent();
            outputDevice.DeviceNumber = i;
            outputDevices.Add(outputDevice);
        }

        // create a mixer to mix the audio to all output devices
        var mixer = new MixingWaveProvider32(outputDevices.Select(d => d.OutputWaveFormat).ToArray());
        mixer.ConnectInputTo(input);

        // start all output devices
        foreach (var outputDevice in outputDevices)
        {
            outputDevice.Init(mixer);
            outputDevice.Play();
        }

        // wait for user input to stop streaming
        Console.WriteLine("Press any key to stop streaming...");
        Console.ReadKey();

        // stop all output devices
        foreach (var outputDevice in outputDevices)
        {
            outputDevice.Stop();
            outputDevice.Dispose();
        }
    }
}
