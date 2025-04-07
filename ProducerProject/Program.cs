using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;

namespace ProducerProject
{
    public class Producer
    {
        private string folderPath;
        private string serverIp;
        private int serverPort;
        private int numberOfThreads;

        public Producer(string folderPath, string serverIp, int serverPort, int numberOfThreads)
        {
            this.folderPath = folderPath;
            this.serverIp = serverIp;
            this.serverPort = serverPort;
            this.numberOfThreads = numberOfThreads;
        }

        public void Start()
        {
            var videoFiles = Directory.GetFiles(folderPath, "*.mp4"); // Assuming video files are .mp4
            Console.WriteLine($"[DEBUG] {videoFiles.Length} number of videos found");

            int threadCount = 0;
            List<Thread> threads = new List<Thread>();

            foreach (var videoFile in videoFiles)
            {
                if (threadCount >= numberOfThreads)
                    break;

                Thread thread = new Thread(() => UploadVideo(videoFile));
                threads.Add(thread);
                thread.Start();
                threadCount++;
            }

            // Wait for all threads to complete to ensure all videos are processed
            foreach (var thread in threads)
            {
                thread.Join();
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

            string defaultPath = AppDomain.CurrentDomain.BaseDirectory;
            Console.WriteLine($"Use default path ({defaultPath})? Y/n");
            string useDefaultPath = Console.ReadLine();

            string folderPath;
            if (useDefaultPath.Equals("Y", StringComparison.OrdinalIgnoreCase))
            {
                folderPath = defaultPath;
            }
            else
            {
                Console.WriteLine("Enter the folder path where video files are stored:");
                folderPath = Console.ReadLine();
            }

            Producer producer = new Producer(folderPath, serverIp, serverPort, numberOfProducers);
            producer.Start();

            Console.WriteLine("Producer started. Press any key to exit...");
            Console.ReadKey();
        }
    }
}
