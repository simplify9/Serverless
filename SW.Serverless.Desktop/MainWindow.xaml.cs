using Amazon.S3.Model;
using Microsoft.Win32;
using Newtonsoft.Json;
using SW.CloudFiles;
using SW.PrimitiveTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SW.Serverless.Desktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private IDictionary<string, CloudConnection> connections;
        private CloudConnection chosenConnection;
        private string chosenAdapterPath;
        private Options options;

        private void addConnection(CloudConnection con)
        {
            var key = $"{con.BucketName}.{con.ServiceUrl}";
            if (connections.TryAdd(key, con))
            {
                connectionListBox.Items.Add(new ListBoxItem { Content = key });
            }
        }

        private void initConnections()
        {
            this.options = GetOptionsFromJson();
            connections = new Dictionary<string, CloudConnection>();
            foreach(var con in options.CloudConnections)
                addConnection(con);
        }
        public MainWindow()
        {
            InitializeComponent();
            initConnections();
        }

        public static bool BuildPublish(string projectPath, string outputPath)
        {
            Console.WriteLine("Building and publishing...");

            var process = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = new ProcessStartInfo("dotnet")
                {
                    Arguments = $"publish \"{projectPath}\" -o \"{outputPath}\"",
                    //WorkingDirectory = Path.GetDirectoryName(adapterpath),
                    //UseShellExecute = false,
                    //RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();

            var result = process.ExitCode == 0;

            Console.WriteLine($"Building and publishing {(result ? "succeeded" : "failed")}.");

            return result;
        }


        static bool Compress(string path, string zipFileName)
        {
            try
            {
                Console.WriteLine("Compressing files...");

                var filesToCompress = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);

                {
                    using var stream = File.OpenWrite(zipFileName);
                    using var archive = new ZipArchive(stream, ZipArchiveMode.Create);

                    foreach (var file in filesToCompress)
                    {
                        var entryName = System.IO.Path.GetRelativePath(path, file);
                        archive.CreateEntryFromFile(file, entryName);
                    }

                }

                Console.WriteLine("Compressing files succeeded.");
                return true;
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Compressing files failed: {ex}");
                return false;

            }


        }

        private void chooseConnection(object sender, RoutedEventArgs e)
        {
            var item = (ListBoxItem)connectionListBox.SelectedItem;
            string key = item.Content.ToString();
            
            chosenConnection = connections[key];
        }
        private void addConnectionToJson(object sender, RoutedEventArgs e)
        {

            Options current = GetOptionsFromJson();
            var connection = new CloudConnection
            {
                AccessKeyId = accessKeyText.Text,
                BucketName = bucketNameText.Text,
                SecretAccessKey = secretAccessText.Text,
                ServiceUrl = serviceUrlText.Text
            };
            current.CloudConnections.Add(connection);
            string optionsJson = JsonConvert.SerializeObject(current);
            File.WriteAllText("./settings.json", optionsJson);
            addConnection(connection);

        }

        private Options GetOptionsFromJson()
        {
            if (File.Exists("./settings.json"))
            {
                string optionsJson = File.ReadAllText("./settings.json");
                return JsonConvert.DeserializeObject<Options>(optionsJson);
            }
            else
            {
                File.WriteAllText("./settings.json", JsonConvert.SerializeObject(new Options()));
                return new Options();
            }
            
        }

        static bool PushToCloud(string zipFielPath, string adapterId, string entryAssembly, string accessKeyId,
                                string secretAccessKey, string serviceUrl, string bucketName)
        {

            try
            {

                Console.WriteLine("Pushing to cloud...");


                using var cloudService = new CloudFilesService(new CloudFilesOptions
                {
                    AccessKeyId = accessKeyId,
                    SecretAccessKey = secretAccessKey,
                    ServiceUrl = serviceUrl,
                    BucketName = bucketName
                });

                using var zipFileStream = File.OpenRead(zipFielPath);

                cloudService.WriteAsync(zipFileStream, new WriteFileSettings
                {
                    ContentType = "application/zip",
                    Key = $"adapters/{adapterId}".ToLower(),
                    Metadata = new Dictionary<string, string>
                        {
                            {"EntryAssembly", entryAssembly},
                            {"Lang", "dotnet" }
                        }
                }).Wait();

                Console.WriteLine("Pushing to cloud succeeded.");
                return true;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Pushing to cloud failed: {ex}");
                return false;
            }
        }

        private void installAdapter(object sender, RoutedEventArgs e)
        {
            string projectPath = chosenAdapterPath;
            if(chosenConnection == null)
            {
                throw new Exception("Invalid connection");
            }

            var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));

            if (!BuildPublish(projectPath, tempPath)) return;

            string adapterId = adapterIdText.Text;

            var zipFileName = System.IO.Path.Combine(tempPath, $"{adapterId}");

            if (!Compress(tempPath, zipFileName)) return;

            var projectFileName = System.IO.Path.GetFileName(projectPath);
            var entryAssembly = $"{projectFileName.Remove(projectFileName.LastIndexOf('.'))}.dll";

            if (!PushToCloud(zipFileName, adapterId, entryAssembly, chosenConnection.AccessKeyId, chosenConnection.SecretAccessKey, chosenConnection.ServiceUrl, chosenConnection.BucketName)) return;

            if (!Cleanup(tempPath)) return;

        }

        private void chooseAdapter(object sender, RoutedEventArgs args)
        {
            var dialogue = new OpenFileDialog();
            dialogue.Multiselect = false;
            dialogue.CheckFileExists = true;
            dialogue.ValidateNames = true;
            dialogue.ShowDialog();
            if(dialogue.FileNames != null && dialogue.FileNames.Length > 0)
                chosenAdapterPath = dialogue.FileName;
        }

        static bool Cleanup(string tempPath)
        {
            try
            {
                Console.WriteLine("Cleaning up...");
                Directory.Delete(tempPath, true);
                return true;

            }
            catch (Exception ex)
            {

                Console.WriteLine($"Cleaning up failed: {ex}");
                return false;
            }

        }

        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {

            }

        }

    }
}
