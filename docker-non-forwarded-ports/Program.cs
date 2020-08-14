using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace docker_port
{
    internal class Program
    {
        private static readonly string DOCKER_TEMP_FILE_LOCATION = Path.GetTempFileName();

        private static async Task Main(string[] args)
        {
            var client = new DockerClientConfiguration(
                    new Uri("npipe://./pipe/docker_engine"))
                .CreateClient();

            var cancellation = new CancellationTokenSource();
            var stream = await client.System.MonitorEventsAsync(new ContainerEventsParameters(), cancellation.Token);
            var reader = new StreamReader(stream);

            var buffer = "";

            while (true)
            {
                try
                {
                    // check if it might be a JSON string to reduce parsing frequency
                    if (buffer.Count(f => f == '}') == buffer.Count(f => f == '{'))
                    {
                        var parsedBuffer = JToken.Parse(buffer);
                        if (parsedBuffer["status"]?.ToString() == "start")
                            await HandleContainerInjection(client, parsedBuffer);
                        buffer = "";
                    }
                }
                catch (JsonReaderException)
                {
                    // ignore as the buffer is still being created
                }

                buffer += Convert.ToString(Convert.ToChar(reader.Read()));
            }
        }

        private static async Task HandleContainerInjection(DockerClient client, JToken parsedBuffer)
        {
            Console.WriteLine(parsedBuffer["status"] + "->" + parsedBuffer["id"]);

            // wait for container to finish booting
            await Task.Delay(5000);
            var strCmdText = "docker exec -it " + parsedBuffer["id"] +
                             " /bin/bash -c \"echo YXdrICdmdW5jdGlvbiBoZXh0b2RlYyhzdHIscmV0LG4saSxrLGMpewogICAgcmV0ID0gMAogICAgbiA9IGxlbmd0aChzdHIpCiAgICBmb3IgKGkgPSAxOyBpIDw9IG47IGkrKykgewogICAgICAgIGMgPSB0b2xvd2VyKHN1YnN0cihzdHIsIGksIDEpKQogICAgICAgIGsgPSBpbmRleCgiMTIzNDU2Nzg5YWJjZGVmIiwgYykKICAgICAgICByZXQgPSByZXQgKiAxNiArIGsKICAgIH0KICAgIHJldHVybiByZXQKfQpmdW5jdGlvbiBnZXRJUChzdHIscmV0KXsKICAgIHJldD1oZXh0b2RlYyhzdWJzdHIoc3RyLGluZGV4KHN0ciwiOiIpLTIsMikpOyAKICAgIGZvciAoaT01OyBpPjA7IGktPTIpIHsKICAgICAgICByZXQgPSByZXQiLiJoZXh0b2RlYyhzdWJzdHIoc3RyLGksMikpCiAgICB9CiAgICByZXQgPSByZXQiOiJoZXh0b2RlYyhzdWJzdHIoc3RyLGluZGV4KHN0ciwiOiIpKzEsNCkpCiAgICByZXR1cm4gcmV0Cn0gCk5SID4gMSB7e2xvY2FsPWdldElQKCQyKTtyZW1vdGU9Z2V0SVAoJDMpfXtwcmludCBsb2NhbCIgLSAicmVtb3RlfX0nIC9wcm9jL25ldC90Y3AgL3Byb2MvbmV0L3RjcDY= | base64 --decode | bash\" > " +
                             DOCKER_TEMP_FILE_LOCATION;
            RunCommand(strCmdText);
            var lines = File.ReadAllLines(DOCKER_TEMP_FILE_LOCATION);
            var ports = new List<int>();
            ExtractDockerPorts(lines, ports);
            var output = client.Containers.InspectContainerAsync(parsedBuffer["id"].ToString(), CancellationToken.None);

            var matchingPorts = new List<int>();
            try
            {
                matchingPorts = output.Result.NetworkSettings.Ports.Keys.Select(s => int.Parse(s.Split("/")[0]))
                    .Intersect(ports).ToList();
            }
            catch (FormatException)
            {
                Console.WriteLine(JsonConvert.SerializeObject(output.Result.NetworkSettings.Ports.Keys));
                matchingPorts = null;
            }

            if (matchingPorts != null && matchingPorts.Count == 0)
            {
                // there are no ports inside of the container that are forwarded; alert user
                Console.Beep();
                Console.Beep();
                Console.Beep();
                Console.WriteLine("Warning: container " + parsedBuffer["id"] + " does not have any forwarded ports.");
            }
        }

        private static void ExtractDockerPorts(string[] lines, List<int> ports)
        {
            foreach (var line in lines)
                try
                {
                    ports.Add(int.Parse(Regex.Match(line, @"\:([0-9]+) ").Groups[0].ToString().Replace(":", "")));
                }
                catch (Exception)
                {
                    // if the ports don't exist, don't crash as it might not have a /bin/bash shell
                }
        }

        private static void RunCommand(string command)
        {
            var proc1 = new ProcessStartInfo();
            proc1.UseShellExecute = true;
            proc1.WorkingDirectory = Path.GetDirectoryName(DOCKER_TEMP_FILE_LOCATION);

            proc1.FileName = @"C:\Windows\System32\cmd.exe";
            proc1.Arguments = "/c " + command;
            proc1.WindowStyle = ProcessWindowStyle.Hidden;
            Process.Start(proc1);
        }
    }
}