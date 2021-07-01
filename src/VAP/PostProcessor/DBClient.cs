// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using PostProcessor.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;

namespace PostProcessor
{
    public class DBClient
    {
        private static readonly string s_dBServer = ConfigurationManager.AppSettings["DBServer"];
        private static readonly string s_dBCred = ConfigurationManager.AppSettings["DBCred"];
        private static readonly string s_dBName = ConfigurationManager.AppSettings["DBName"];

        private static readonly HttpClient s_client = new();

        public static async Task<Camera> GetDocumentCamera(string id)
        {
            var serializer = new DataContractJsonSerializer(typeof(Camera));

            s_client.DefaultRequestHeaders.Accept.Clear();
            s_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", s_dBCred);

            System.IO.Stream streamTask = null;
            Camera jsonCamera = null;
            try
            {
                streamTask = await s_client.GetStreamAsync(s_dBServer + "_db/" + s_dBName + "/_api/document/camera/" + id);
                jsonCamera = serializer.ReadObject(streamTask) as Camera;
            }
            catch (Exception)
            {

            }

            /* Using GetStringAsync */
            //var stringTask = client.GetStringAsync(DBServer + "_db/" + DBName + "/_api/document/camera/" + id);
            //var msg = await stringTask;
            //var msgStream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(msg));
            //var jsonCamera = serializer.ReadObject(msgStream) as Camera;

            return jsonCamera;
        }

        public static async Task<Model.Object> GetDocumentObject(string id)
        {
            var serializer = new DataContractJsonSerializer(typeof(Model.Object));

            s_client.DefaultRequestHeaders.Accept.Clear();
            s_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", s_dBCred);

            System.IO.Stream streamTask = null;
            Model.Object jsonObject = null;
            try
            {
                streamTask = await s_client.GetStreamAsync(s_dBServer + "_db/" + s_dBName + "/_api/document/object/" + id);
                jsonObject = serializer.ReadObject(streamTask) as Model.Object;
            }
            catch (Exception)
            {

            }

            return jsonObject;
        }

        public static async Task<Detection> GetDocumentDetection(string id)
        {
            var serializer = new DataContractJsonSerializer(typeof(Detection));

            s_client.DefaultRequestHeaders.Accept.Clear();
            s_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", s_dBCred);

            System.IO.Stream streamTask = null;
            Detection jsonDetection = null;
            try
            {
                streamTask = await s_client.GetStreamAsync(s_dBServer + "_db/" + s_dBName + "/_api/document/detection/" + id);
                jsonDetection = serializer.ReadObject(streamTask) as Detection;
            }
            catch (Exception)
            {

            }

            return jsonDetection;
        }

        public static async Task<string[]> ProcessQueryRaw(string content)
        {
            string[] result = new string[2];
            var serializer = new DataContractJsonSerializer(typeof(QueryRaw));

            s_client.DefaultRequestHeaders.Accept.Clear();
            s_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", s_dBCred);
            var buffer = System.Text.Encoding.UTF8.GetBytes(content);
            var byteContent = new ByteArrayContent(buffer);

            HttpResponseMessage response = null;
            try
            {
                var stopWatch = Stopwatch.StartNew();
                response = await s_client.PostAsync(s_dBServer + "_db/" + s_dBName + "/_api/cursor", byteContent);
                result[0] = stopWatch.Elapsed.ToString();
            }
            catch (Exception)
            {
                return null;
            }


            //var message = new HttpRequestMessage(HttpMethod.Post, DBServer + "_db/" + DBName + "/_api/cursor");
            //message.Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json");
            //var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead);

            var stream = await response.Content.ReadAsStreamAsync();
            System.IO.StreamReader reader = new System.IO.StreamReader(stream);
            result[1] = reader.ReadToEnd();
            stream.Position = 0;

            return result;
        }

        public static async Task<List<Consolidation>> ProcessQuery(string content)
        {
            var serializer = new DataContractJsonSerializer(typeof(QueryRaw));

            s_client.DefaultRequestHeaders.Accept.Clear();
            s_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", s_dBCred);
            var buffer = System.Text.Encoding.UTF8.GetBytes(content);
            var byteContent = new ByteArrayContent(buffer);

            HttpResponseMessage response = null;
            try
            {
                response = await s_client.PostAsync(s_dBServer + "_db/" + s_dBName + "/_api/cursor", byteContent);
            }
            catch (Exception)
            {
                return null;
            }

            //var message = new HttpRequestMessage(HttpMethod.Post, DBServer + "_db/" + DBName + "/_api/cursor");
            //message.Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json");
            //var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead);

            var stream = await response.Content.ReadAsStreamAsync();
            //System.IO.StreamReader reader = new System.IO.StreamReader(stream);
            //string text = reader.ReadToEnd();
            //stream.Position = 0;
            var queryRaw = serializer.ReadObject(stream) as QueryRaw;
            return queryRaw.QResult;
        }

        public static async Task<System.Net.HttpStatusCode> CreateCollection(string cltName)
        {
            s_client.DefaultRequestHeaders.Accept.Clear();
            s_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", s_dBCred);
            string content = "{\"name\": \"" + cltName + "\"}";
            var buffer = System.Text.Encoding.UTF8.GetBytes(content);
            var byteContent = new ByteArrayContent(buffer);

            HttpResponseMessage response = null;
            try
            {
                response = await s_client.PostAsync(s_dBServer + "_db/" + s_dBName + "/_api/collection", byteContent);
            }
            catch (Exception)
            {
                return 0;
            }

            return response.StatusCode;
        }

        public static async Task<System.Net.HttpStatusCode> CreateDocument(string cltName, string content)
        {
            s_client.DefaultRequestHeaders.Accept.Clear();
            s_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", s_dBCred);
            var buffer = System.Text.Encoding.UTF8.GetBytes(content);
            var byteContent = new ByteArrayContent(buffer);

            HttpResponseMessage response = null;
            try
            {
                response = await s_client.PostAsync(s_dBServer + "_db/" + s_dBName + "/_api/document/" + cltName, byteContent);
            }
            catch (Exception)
            {
                return 0;
            }

            return response.StatusCode;
        }
    }
}
