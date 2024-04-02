﻿using BepInEx.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;

namespace RemotePlugins
{
    internal class BackendApi
    {
        private ManualLogSource logger = Logger.CreateLogSource("RemotePlugins_BackendApi");
        private HttpClient httpClient = new HttpClient();

        private BackendUrlObj backendUrlObj;
        private bool canConnect = false;
        public bool CanConnect { get { return canConnect; } }


        public class BackendUrlObj
        {
            public string BackendUrl { get; set; }
            public string Version { get; set; }
        }

        public BackendApi()
        {
            Initialize();
            TestConnection();
        }

        private void Initialize()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (args == null)
            {
                logger.LogFatal("No command line arguments found");
                return;
            }

            foreach (string arg in args)
            { // -config={'BackendUrl':'http://tarky.carothers.me:6969/','Version':'live'}
                if (arg.Contains("BackendUrl"))
                {
                    string json = arg.Replace("-config=", string.Empty);
                    backendUrlObj = JsonConvert.DeserializeObject<BackendUrlObj>(json);
                    logger.LogInfo("BackendUrl: " + backendUrlObj.BackendUrl);
                }
            }
        }

        private void TestConnection()
        {
            if (backendUrlObj == null || string.IsNullOrWhiteSpace(backendUrlObj.BackendUrl))
            {
                logger.LogFatal("BackendUrl is not set");
                return;
            }

            httpClient.BaseAddress = new Uri(backendUrlObj.BackendUrl);
            string assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            string assemblyVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            string userAgent = "SITarkov-" + assemblyName + "-" + assemblyVersion;
            httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);
            httpClient.DefaultRequestHeaders.Add("debug", "1"); // if we don't set this then bad requests are zlib'd
            
            // Send a request to the backend to see if we can connect
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "launcher/ping");
                request.Headers.Add("Accept", "application/json");

                HttpResponseMessage response = httpClient.SendAsync(request).Result;
                response.EnsureSuccessStatusCode();
                string responseBody = response.Content.ReadAsStringAsync().Result;

                if (responseBody != "\"pong!\"")
                {
                    logger.LogFatal("Backend responded different than expected: " + responseBody);
                    return;
                }

                canConnect = true;
            }
            catch (Exception e)
            {
                logger.LogFatal("Failed to connect to backend: " + e.Message);
                return;
            }
        }

        internal PluginFileMap GetFileList()
        {
            PluginFileMap pluginFileMap = null;
            try
            {
                HttpResponseMessage response = httpClient.GetAsync("RemotePlugins/FileMap").Result;
                response.EnsureSuccessStatusCode();
                string responseBody = response.Content.ReadAsStringAsync().Result;
                // convert the JSON to a PluginFileMap object
                pluginFileMap = JsonConvert.DeserializeObject<PluginFileMap>(responseBody);
                logger.LogInfo("Got file map: " + pluginFileMap.Files.Count + " files");
            }
            catch (Exception e)
            {
                logger.LogFatal("Failed to get file map: " + e.Message);
            }
            return pluginFileMap;
        }

        private static string RemotePluginsFilename = "RemotePlugins.zip";
        internal PluginUpdateFile GetPluginUpdateFile(string downloadFolder)
        {
            try
            {
                HttpResponseMessage response = httpClient.GetAsync("RemotePlugins/File").Result;
                response.EnsureSuccessStatusCode();
                // this is the file, store it in the download folder
                byte[] fileBytes = response.Content.ReadAsByteArrayAsync().Result;
                if (fileBytes == null || fileBytes.Length <= 100)
                {
                    logger.LogFatal("Failed to get file");
                    return null;
                }
                string filePath = Path.GetFullPath(Path.Combine(downloadFolder, RemotePluginsFilename));

                // create the download folder if it doesn't exist
                if (!Directory.Exists(downloadFolder))
                {
                    Directory.CreateDirectory(downloadFolder);
                }

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                File.WriteAllBytes(filePath, fileBytes);
                return new PluginUpdateFile
                {
                    FileName = RemotePluginsFilename,
                    FilePath = filePath,
                    FileSize = fileBytes.Length
                };
            }
            catch (Exception e)
            {
                logger.LogFatal("Failed to get file list: " + e.Message);
                return null;
            }
        }
    }
}