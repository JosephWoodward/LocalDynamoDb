using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.Runtime;

namespace LocalDynamoDb.Builder.JavaBinaries.Internals
{
    internal sealed class DynamoProcessHandler
    {
        private readonly JarBinariesConfiguration _configuration;
        private readonly Process _dynamoProcess;

        public DynamoProcessHandler(JarBinariesConfiguration configuration)
        {
            _configuration = configuration;
            _dynamoProcess = CreateProcess(configuration.PortNumber, configuration.Path);
        }

        private static Process CreateProcess(int portNumber, string path)
        {
            var processJar = new Process();
            var arguments = $"-Djava.library.path=.{Path.DirectorySeparatorChar.ToString()}DynamoDBLocal_lib -jar DynamoDBLocal.jar -sharedDb -inMemory -port {portNumber.ToString()}";

            processJar.StartInfo.FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "\"" + @"java" + "\"" : "java";;
            processJar.StartInfo.Arguments = arguments;

            var rootFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var relativePath = $"{Path.DirectorySeparatorChar.ToString()}dynamodblocal";
            var osFriendlyAbsPath = Path.GetFullPath(rootFolder + relativePath).Replace('\\', Path.DirectorySeparatorChar);
            var jarFilePath = Path.Combine(osFriendlyAbsPath, "DynamoDBLocal.jar");

            Console.WriteLine("Jar file path - " + jarFilePath);

            if (!File.Exists(jarFilePath))
            {
                throw new FileNotFoundException($"DynamoDBLocal.jar not found in {osFriendlyAbsPath}. Please review the README.txt for setup instructions.", jarFilePath);
            }

            processJar.StartInfo.WorkingDirectory = osFriendlyAbsPath;
            processJar.StartInfo.UseShellExecute = false;
            processJar.StartInfo.RedirectStandardOutput = true;
            processJar.StartInfo.RedirectStandardError = true;
            
            return processJar;
        }

        public async Task<bool> StartAsync()
        {
            Console.WriteLine("Starting in memory DynamoDb");
            var success = _dynamoProcess.Start();
            
            if (!success)
                throw new Exception($"Error starting dynamo: {_dynamoProcess.StandardError.ReadToEnd()}");

            var config = new AmazonDynamoDBConfig {ServiceURL = $"http://localhost:{_configuration.PortNumber.ToString()}"};
            var credentials = new BasicAWSCredentials("abc", "abc");
            var client = new AmazonDynamoDBClient(credentials, config);

            var tokenSource = new CancellationTokenSource();
            tokenSource.CancelAfter(TimeSpan.FromSeconds(10));
            var token = tokenSource.Token;

            while (!token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                
                if (await IsReady(client, token).ConfigureAwait(false))
                    return true;
            }

            return false;
        }

        private static async Task<bool> IsReady(IAmazonDynamoDB client, CancellationToken token)
        {
            try
            {
                var t = await client.ListTablesAsync(token);
                return t.HttpStatusCode == HttpStatusCode.OK;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool IsResponding()
            => _dynamoProcess.Responding;

        public Task Stop()
        {
            Console.WriteLine("Stopping in memory DynamoDb");
            try
            {
                _dynamoProcess.Kill();
            }
            catch (Win32Exception)
            {
                Console.WriteLine(_dynamoProcess.StandardError.ReadToEnd());
                throw;
            }

            return Task.CompletedTask;
        }
    }
}