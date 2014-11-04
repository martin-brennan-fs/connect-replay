using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Configuration;
using System.Net.Http;
using System.Globalization;

namespace DocuSignConnectReplay
{
    class Program
    {
        public static Amazon.S3.AmazonS3Client S3 = new Amazon.S3.AmazonS3Client(Amazon.RegionEndpoint.APSoutheast2);

        static void Main(string[] args)
        {
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            Console.WriteLine("Paste a specific log UUID or leave blank for multiple logs.");
            string logID = Console.ReadLine();

            if (!string.IsNullOrEmpty(logID))
            {
                watch.Start();
                Console.WriteLine("Replaying Connect Log {0}", logID);
                ReplayLog(logID);
                watch.Stop();
                TimeSpan ts = watch.Elapsed;
                Console.WriteLine("{0}ms taken to replay.", ts.TotalMilliseconds);
            }
            else
            {
                Console.WriteLine("Enter the start date and time of logs to replay in the format of YYYY-MM-dd hh:mm");
                string start = Console.ReadLine();

                Console.WriteLine("Enter the end date and time of logs to replay in the format of YYYY-MM-dd hh:mm");
                string end = Console.ReadLine();

                watch.Start();
                ReplayLogRange(start, end);
                watch.Stop();
                TimeSpan ts = watch.Elapsed;
                Console.WriteLine("{0}ms taken to replay.", ts.TotalMilliseconds);
            }

            Console.ReadKey();
        }

        /// <summary>
        /// Gets all XML logs in a datetime range,
        /// loops through and replays them on the provided
        /// endpoint.
        /// </summary>
        /// <param name="start">The start datetime in the format yyyy-MM-dd HH:mm</param>
        /// <param name="end">The end datetime in the format yyyy-MM-dd HH:mm</param>
        private static void ReplayLogRange(string start, string end)
        {
            var request = new Amazon.S3.Model.ListObjectsRequest()
            {
                Prefix = ConfigurationManager.AppSettings["ConnectPrefix"],
                BucketName = ConfigurationManager.AppSettings["Bucket"]
            };

            DateTime startDateTime = DateTime.ParseExact(start, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            DateTime endDateTime = DateTime.ParseExact(end, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

            // Get all objects in the connect_log folder, then filter by datetime range.
            var response = S3.ListObjects(request);
            var objects = response.S3Objects.Where(o => o.LastModified >= startDateTime && o.LastModified <= endDateTime).ToList();

            Console.WriteLine("There are {0} matching objects matching the date range {1} to {2}. Replaying connect logs now.", objects.Count, start, end);

            foreach (var s3object in objects.OrderBy(o => o.LastModified).ToList())
            {
                Console.WriteLine("[{0:yyyy-MM-dd HH:mm:ss}] {1}", s3object.LastModified, s3object.Key);
                ReplayLog(s3object.Key);
                System.Threading.Thread.Sleep(500);
            }
        }


        /// <summary>
        /// Replays a single XML log with the provided id.
        /// </summary>
        private static void ReplayLog(string logID)
        {
            // Append .xml if required.
            if (!logID.EndsWith(".xml")) {
                logID += ".xml";
            }

            // Append connect_log/ if required.
            if (!logID.StartsWith(ConfigurationManager.AppSettings["ConnectPrefix"]))
            {
                logID = ConfigurationManager.AppSettings["ConnectPrefix"] + logID;
            }

            // Create the request with the xml key.
            var request = new Amazon.S3.Model.GetObjectRequest()
            {
                Key = String.Format("{0}", logID),
                BucketName = ConfigurationManager.AppSettings["Bucket"]
            };

            // Get the object from S3.
            var response = S3.GetObject(request);

            // Read the bytes of the XML file.
            byte[] connectXML;
            using (var ms = new MemoryStream())
            {
                response.ResponseStream.CopyTo(ms);
                connectXML = ms.ToArray();
            }

            // Send the XML to the XML Passthru.
            PassthruXML(connectXML, logID);
        }

        /// <summary>
        /// Sends the Connect XML to the XML passthru endpoint, recording
        /// any errors that are encountered.
        /// </summary>
        /// <param name="connectXML">The connect XML bytes from S3.</param>
        /// <param name="xmlFileName">The filename of the connect xml from S3.</param>
        private static void PassthruXML(byte[] connectXML, string xmlFileName)
        {
            string endpoint = ConfigurationManager.AppSettings["Endpoint"];

            using (var client = new HttpClient())
            {
                try
                {
                    var response = client.PostAsync(endpoint, new ByteArrayContent(connectXML)).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("{0} POSTed to {1} successfully.", xmlFileName, endpoint);
                    }
                    else
                    {
                        Console.WriteLine("{0} FAILED POST to {1}. {2}", xmlFileName, endpoint, response.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("{0} FAILED POST to {1}. {2}", xmlFileName, endpoint, ex.Message);
                }
            }
        }
    }
}
