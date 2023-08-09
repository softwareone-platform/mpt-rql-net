using System.Diagnostics;
using Xunit;

namespace Rql.Tests.Integration.Fixtures;

public class SampleApiInstanceFixture : IAsyncLifetime
{
    public HttpClient Client => _client!;

    private HttpClient? _client;

    private static Process? _apiProcess;
    private const int Port = 50011;

    public async Task InitializeAsync()
    {
        _apiProcess = RunApi();

        await Task.Delay(1000);

        if (_apiProcess.HasExited  && _apiProcess.ExitCode != 0)
        {
            var stdError = await _apiProcess.StandardError.ReadToEndAsync();
            var stdOutput = await _apiProcess.StandardOutput.ReadToEndAsync();
            Console.WriteLine(stdError);
            Console.WriteLine(stdOutput);

            throw new ApplicationException($@"Cannot start sample API!
======================================================================================
{stdOutput}
{stdError}
======================================================================================
");
        }

        _client = new HttpClient
        {
            BaseAddress = new Uri($"http://127.0.0.1:{Port}")
        };
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _apiProcess?.Kill();
        _apiProcess?.Close();
        _apiProcess?.Dispose();
    }

    private static Process RunApi()
    {
        var process = new Process();
        var startInfo = new ProcessStartInfo
        {
            WindowStyle = ProcessWindowStyle.Hidden,
            FileName = "dotnet",
            Arguments = $"Rql.Sample.Api.dll  --urls \"http://localhost:{Port}\" ",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        process.StartInfo = startInfo;
        process.Start();

        return process;
    }
}