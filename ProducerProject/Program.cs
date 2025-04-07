using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;

namespace ProducerProject
{
    public class Producer
    {
        private List<string> folderPaths;
        private string serverIp;
        private int serverPort;

        public Producer(List<string> folderPaths, string serverIp, int serverPort)
        {
            this.folderPaths = folderPaths;
            this.serverIp = serverIp;
            this.serverPort = serverPort;
        }

        public void Start()
        {
            List<Thread> threads = new List<Thread>();

            foreach (var folderPath in folderPaths)
            {
                Thread thread = new Thread(() => ProcessFolder(folderPath));
                threads.Add(thread);
                thread.Start();
            }

            // Wait for all threads to complete to ensure all videos are processed
            foreach (var thread in threads)
            {
                thread.Join();
            }
        }

        private void ProcessFolder(string folderPath)
        {
            var videoFiles = Directory.GetFiles(folderPath, "*.mp4"); // Assuming video files are .mp4
            Console.WriteLine($"[DEBUG] {videoFiles.Length} number of videos found in folder {folderPath}");

            foreach (var videoFile in videoFiles)
            {
                UploadVideo(videoFile);
            }
        }

        private void UploadVideo(string videoFilePath)
        {
            try
            {
                using (TcpClient client = new TcpClient(serverIp, serverPort))
                using (NetworkStream stream = client.GetStream())
                using (FileStream fs = new FileStream(videoFilePath, FileMode.Open, FileAccess.Read))
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead;

                    while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        stream.Write(buffer, 0, bytesRead);
                    }

                    Console.WriteLine($"Sent video {videoFilePath} to consumer.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading video: {ex.Message}");
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Enter the number of producer threads:");
            int numberOfProducers = int.Parse(Console.ReadLine());

            Console.WriteLine("Enter the IP address of the consumer:");
            string serverIp = Console.ReadLine();

            Console.WriteLine("Enter the port number for communication:");
            int serverPort = int.Parse(Console.ReadLine());

            List<string> folderPaths = new List<string>();

            for (int i = 0; i < numberOfProducers; i++)
            {
                string defaultPath = AppDomain.CurrentDomain.BaseDirectory;
                Console.WriteLine($"Use default path for thread {i + 1} ({defaultPath})? Y/n");
                string useDefaultPath = Console.ReadLine();

                if (useDefaultPath.Equals("Y", StringComparison.OrdinalIgnoreCase))
                {
                    folderPaths.Add(defaultPath);
                }
                else
                {
                    Console.WriteLine($"Enter the folder path for thread {i + 1} where video files are stored:");
                    string folderPath = Console.ReadLine();
                    folderPaths.Add(folderPath);
                }
            }

            Producer producer = new Producer(folderPaths, serverIp, serverPort);
            producer.Start();

            Console.WriteLine("Producer started. Press any key to exit...");
            Console.ReadKey();
        }
    }
}
