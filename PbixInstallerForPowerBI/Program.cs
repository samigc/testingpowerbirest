using System;

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Net.Http;
using System.Net.Http.Headers;
using RestPowerBI.Model;
using Newtonsoft.Json;
using System.Net;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace RestPowerBI
{

    class ProgramConstants
    {

        // update client id to reference an application registred with Azure
        public const string ClientID = "bb0b2fc4-c7c5-4075-bcc5-2004d6329cce";

        // Redirect URL needs to match reply URL in Azure registration
        public const string RedirectUri = "https://localhost/ForPowerBI";

        // URLs for working with the Power BI REST API
        public const string AzureAuthorizationEndpoint = "https://login.microsoftonline.com/common";
        public const string PowerBiServiceResourceUri = "https://analysis.windows.net/powerbi/api";
        public const string PowerBiServiceRootUrl = "https://api.powerbi.com/v1.0/myorg/";

        // Commonly-used Power BI REST URLs
        public const string restUrlWorkspaces = "https://api.powerbi.com/v1.0/myorg/groups/";
        public const string restUrlDatasets = "https://api.powerbi.com/v1.0/myorg/datasets/";
        public const string restUrlReports = "https://api.powerbi.com/v1.0/myorg/reports/";
        public const string restUrlImports = "https://api.powerbi.com/v1.0/myorg/imports/";

        // login credentials for Azure SQL database
        public const string AzureSqlDatabaseLogin = "CptStudent";
        public const string AzureSqlDatabasePassword = "pass@word1";

    }

    class Program
    {

        protected static string AccessToken = string.Empty;

        protected static void AcquireAccessToken()
        {

            Console.WriteLine("Calling to acquire access token...");

            // create new ADAL authentication context 
            var authenticationContext =
              new AuthenticationContext(ProgramConstants.AzureAuthorizationEndpoint);

            //use authentication context to trigger user login and then acquire access token
            //var userAuthnResult =
            //  authenticationContext.AcquireTokenAsync(ProgramConstants.PowerBiServiceResourceUri,
            //                                          ProgramConstants.ClientID,
            //                                          new Uri(ProgramConstants.RedirectUri),
            //                                          new PlatformParameters(PromptBehavior.Auto)).Result;


            //// use authentication context to trigger user sign-in and return access token 
            var userCreds = new UserPasswordCredential("useracccount", "Password");

            var userAuthnResult = authenticationContext.AcquireTokenAsync(ProgramConstants.PowerBiServiceResourceUri,
                                                                          ProgramConstants.ClientID,
                                                                          userCreds).Result;


            // cache access token in AccessToken field
            AccessToken = userAuthnResult.AccessToken;

            Console.WriteLine(" - access token successfully acquired");
            Console.WriteLine();
        }

        #region "REST operation utility methods"

        private static string ExecuteGetRequest(string restUri)
        {

            HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + AccessToken);
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            HttpResponseMessage response = client.GetAsync(restUri).Result;
            Console.WriteLine(response.StatusCode);
            if (response.IsSuccessStatusCode)
            {
                return response.Content.ReadAsStringAsync().Result;
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("OUCH! - error occurred during GET REST call");
                Console.WriteLine();
                return string.Empty;
            }
        }

        private static string GetRequest(string restUri)
        {
            HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + AccessToken);

            client.DefaultRequestHeaders.Add("Accept", "application/json");

            HttpResponseMessage response = client.GetAsync(restUri).Result;
            Console.WriteLine(response.StatusCode);
            if (response.IsSuccessStatusCode)
            {
                return response.Content.ReadAsStringAsync().Result;
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("OUCH! - error occurred during GET REST call");
                Console.WriteLine();
                return string.Empty;
            }
        }

        private static string ExecuteGetWebRequest(string restUri)
        {
            HttpWebRequest request = System.Net.WebRequest.Create(restUri) as System.Net.HttpWebRequest;
            request.KeepAlive = true;
            request.Method = "GET";
            request.ContentLength = 0;
            request.ContentType = "application/json";
            request.Headers.Add("Authorization", String.Format("Bearer {0}", AccessToken));

            string datasetId = string.Empty;
            //Get HttpWebResponse from GET request
            using (HttpWebResponse httpResponse = request.GetResponse() as System.Net.HttpWebResponse)
            {
                //Get StreamReader that holds the response stream
                using (StreamReader reader = new System.IO.StreamReader(httpResponse.GetResponseStream()))
                {
                    string responseContent = reader.ReadToEnd();


                    if (responseContent == null)
                    {
                        return "";
                    }
                    else
                    {

                        var results = JsonConvert.DeserializeObject<dynamic>(responseContent);

                        datasetId = results["value"][0]["id"];

                        Console.WriteLine(String.Format("Dataset ID: {0}", datasetId));


                        return datasetId;
                    }

                }
            }





        }

        private static string ExecutePostRequest(string restUri, string postBody)
        {

            try
            {
                HttpContent body = new StringContent(postBody);
                body.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json");
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + AccessToken);
                HttpResponseMessage response = client.PostAsync(restUri, body).Result;

                if (response.IsSuccessStatusCode)
                {
                    return response.Content.ReadAsStringAsync().Result;
                }
                else
                {
                    Console.WriteLine();
                    Console.WriteLine("OUCH! - error occurred during POST REST call");
                    Console.WriteLine();
                    return string.Empty;
                }
            }
            catch
            {
                Console.WriteLine();
                Console.WriteLine("OUCH! - error occurred during POST REST call");
                Console.WriteLine();
                return string.Empty;
            }
        }

        private static string ExecuteDeleteRequest(string restUri)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + AccessToken);
            HttpResponseMessage response = client.DeleteAsync(restUri).Result;

            if (response.IsSuccessStatusCode)
            {
                return response.Content.ReadAsStringAsync().Result;
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("OUCH! - error occurred during Delete REST call");
                Console.WriteLine();
                return string.Empty;
            }
        }

        #endregion

        public static void DisplayWorkspaceContents()
        {

            string jsonWorkspaces = ExecuteGetRequest(ProgramConstants.restUrlWorkspaces);
            WorkspaceCollection workspaces = JsonConvert.DeserializeObject<WorkspaceCollection>(jsonWorkspaces);
            Console.WriteLine("Group Workspaces:");
            Console.WriteLine("-----------------");
            foreach (Workspace workspace in workspaces.value)
            {
                Console.WriteLine(" - " + workspace.name + "(" + workspace.id + ")");
            }
            Console.WriteLine();
            Console.WriteLine("Now examining content in your personal workspace...");
            Console.WriteLine();

            string jsonDatasets = ExecuteGetRequest(ProgramConstants.restUrlDatasets);
            DatasetCollection datasets = JsonConvert.DeserializeObject<DatasetCollection>(jsonDatasets);
            Console.WriteLine("Datasets:");
            Console.WriteLine("---------");
            foreach (var ds in datasets.value)
            {
                Console.WriteLine(" - " + ds.name + "(" + ds.id + ")");
            }
            Console.WriteLine();

            string jsonReports = ExecuteGetRequest(ProgramConstants.restUrlReports);
            ReportCollection reports = JsonConvert.DeserializeObject<ReportCollection>(jsonReports);
            Console.WriteLine("Reports:");
            Console.WriteLine("---------");
            if (reports != null)
            {
                foreach (var report in reports.value)
                {
                    Console.WriteLine(" - " + report.name + ":   " + report.embedUrl);
                }
            }

            Console.WriteLine();

            string jsonImports = ExecuteGetRequest(ProgramConstants.restUrlImports);
            ImportCollection imports = JsonConvert.DeserializeObject<ImportCollection>(jsonImports);
            Console.WriteLine("Imports:");
            Console.WriteLine("---------");
            foreach (var import in imports.value)
            {
                Console.WriteLine(" - " + import.name + ":   " + import.source);
            }
            Console.WriteLine();

        }

        public static void DeleteImport(string importName)
        {
            // check to see if import already exists by inspecting dataset names
            string restUrlDatasets = ProgramConstants.PowerBiServiceRootUrl + "datasets/";
            string jsonDatasets = ExecuteGetRequest(restUrlDatasets);
            DatasetCollection datasets = JsonConvert.DeserializeObject<DatasetCollection>(jsonDatasets);
            foreach (var dataset in datasets.value)
            {
                if (importName.Equals(dataset.name))
                {
                    // if dataset name matches, delete dataset which will effective delete the entire import
                    Console.WriteLine("Deleting existing import named " + dataset.name);
                    string restUrlDatasetToDelete = ProgramConstants.PowerBiServiceRootUrl + "datasets/" + dataset.id;
                    ExecuteDeleteRequest(restUrlDatasetToDelete);
                }
            }
        }

        public static void ImportPBIX(string pbixFilePath, string importName)
        {
            // delete exisitng import of the same name if on exists
            DeleteImport(importName);
            // create REST URL with import name in quer string
            string restUrlImportPbix = ProgramConstants.PowerBiServiceRootUrl + "imports?datasetDisplayName=" + importName;
            // load PBIX file into StreamContent object
            var pbixBodyContent = new StreamContent(File.Open(pbixFilePath, FileMode.Open));
            // add headers for request bod content
            pbixBodyContent.Headers.Add("Content-Type", "application/octet-stream");
            pbixBodyContent.Headers.Add("Content-Disposition",
                                         @"form-data; name=""file""; filename=""" + pbixFilePath + @"""");
            // load PBIX content into body using multi-part form data
            MultipartFormDataContent requestBody = new MultipartFormDataContent(Guid.NewGuid().ToString());
            requestBody.Add(pbixBodyContent);
            // create and configure HttpClient
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + AccessToken);
            // post request
            var response = client.PostAsync(restUrlImportPbix, requestBody).Result;
            // check for success
            if (response.StatusCode.ToString().Equals("Accepted"))
            {
                Console.WriteLine("Import process complete: " + response.Content.ReadAsStringAsync().Result);
            }
        }

        #region "Handling datasets"
        public static String GetID(string DatasetName)
        {
            string restUrlDatasets = ProgramConstants.PowerBiServiceRootUrl + "datasets/";
            string jsonDatasets = ExecuteGetRequest(restUrlDatasets);
            DatasetCollection datasets = JsonConvert.DeserializeObject<DatasetCollection>(jsonDatasets);
            foreach (var dataset in datasets.value)
            {
                if (DatasetName.Equals(dataset.name))
                {
                    string x = dataset.id;
                    return x;
                }
            }
            return "";
        }

        public static JObject ColumnJs(string ColumnName, string DataType)
        {
            dynamic Js = new JObject();
            Js.name = ColumnName;
            Js.dataType = DataType;
            return Js;
        }

        public static JObject TableJs(string TableName, string[] ColumnNames, string[] DataTypes, string[] FormatStrings, string isHidden = "false")
        {
            JArray ColumnArray = new JArray();
            for (int i = 0; i < ColumnNames.Length; i++)
            {
                ColumnArray.Add(ColumnJs(ColumnNames[i], DataTypes[i]));
            }
            JObject Js = new JObject(
                new JProperty("name", TableName),
                new JProperty("columns", ColumnArray));
            return Js;
        }

        public static JObject DatasetJs(string DatasetName, string defaultmode, JObject[] Tables)
        {
            JArray TableArray = new JArray();
            for (int i = 0; i < Tables.Length; i++)
            {
                TableArray.Add(Tables[i]);
            }
            JObject Js = new JObject(
                new JProperty("name", DatasetName),
                new JProperty("defaultMode", defaultmode),
                new JProperty("tables", TableArray));
            return Js;
        }

        public static JObject RowJs(string[] keys, string[] values, string title = "rows")
        {
            JProperty[] k = new JProperty[keys.Length];

            for (int i = 0; i < keys.Length; i++)
            {
                k[i] = new JProperty(keys[i], values[i]);

            }
            JObject Js = new JObject(k);
            JArray rowarray = new JArray();
            rowarray.Add(Js);
            JObject row = new JObject(
                new JProperty(title, rowarray));
            return row;
        }
        #endregion

        public static async Task CreateDataset(string datasetName, string DefaultMode, JObject[] tables)
        {
            var baseAddress = new Uri("https://api.powerbi.com/");
            JObject datasetjson = DatasetJs(datasetName, DefaultMode, tables);
            string datasetstring = JsonConvert.SerializeObject(datasetjson);

            using (var client = new HttpClient { BaseAddress = baseAddress })
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + AccessToken);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                using (var content = new StringContent(datasetstring, System.Text.Encoding.Default, "application/json"))
                {
                    using (var response = await client.PostAsync("v1.0/myorg/datasets", content))
                    {
                        string responseData = await response.Content.ReadAsStringAsync();
                    }
                }
            }
        }

        public static async Task ListTables(string DatasetName)
        {
            string restUrlDatasets = ProgramConstants.PowerBiServiceRootUrl + "datasets/";
            string jsonDatasets = ExecuteGetRequest(restUrlDatasets);
            DatasetCollection datasets = JsonConvert.DeserializeObject<DatasetCollection>(jsonDatasets);
            foreach (var dataset in datasets.value)
            {
                if (DatasetName.Equals(dataset.name))
                {
                    var baseAddress = new Uri("https://api.powerbi.com/");
                    using (var client = new HttpClient { BaseAddress = baseAddress })
                    {
                        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + AccessToken);
                        client.DefaultRequestHeaders.Add("Accept", "application/json");

                        using (var response = await client.GetAsync("v1.0/myorg/datasets/" + dataset.id + "/tables"))
                        {
                            if (response.IsSuccessStatusCode)
                            {
                                string responseData = await response.Content.ReadAsStringAsync();
                                Console.WriteLine(responseData);
                                Console.ReadLine();
                            }
                            else
                            {
                                Console.WriteLine();
                                Console.WriteLine("The response error code is " + response.StatusCode);
                                Console.WriteLine();
                            }

                        }
                    }
                }
            }

        }

        public static async void AddRows(string DatasetName, string TableName)
        {
            string restUrlDatasets = ProgramConstants.PowerBiServiceRootUrl + "datasets/";
            string jsonDatasets = ExecuteGetRequest(restUrlDatasets);
            DatasetCollection datasets = JsonConvert.DeserializeObject<DatasetCollection>(jsonDatasets);
            foreach (var dataset in datasets.value)
            {
                if (DatasetName.Equals(dataset.name))
                {
                    var baseAddress = new Uri("https://api.powerbi.com/");
                    using (var client = new HttpClient { BaseAddress = baseAddress })
                    {
                        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + AccessToken);
                        client.DefaultRequestHeaders.Add("Accept", "application/json");

                        using (var content = new StringContent("{  \"rows\": [    {      \"ProductID\": 5,      \"Name\": \"El gato12\",      \"Category\": \"holaFelin3\",      \"IsCompete\": true,      \"ManufacturedOn\": \"07/30/2014\"    }  ]}", System.Text.Encoding.Default, "application/json"))
                        {
                            using (var response = await client.PostAsync("v1.0/myorg/datasets/" + dataset.id + "/tables/" + TableName + "/rows", content))
                            {
                                string responseData = await response.Content.ReadAsStringAsync();
                                Console.WriteLine(responseData);

                            }
                        }
                    }
                }
            }
        }

        public static async Task DeleteRows(string DatasetName, string Tablename)
        {
            string restUrlDatasets = ProgramConstants.PowerBiServiceRootUrl + "datasets/";
            string jsonDatasets = ExecuteGetRequest(restUrlDatasets);
            DatasetCollection datasets = JsonConvert.DeserializeObject<DatasetCollection>(jsonDatasets);
            foreach (var dataset in datasets.value)
            {
                if (DatasetName.Equals(dataset.name))
                {
                    var baseAddress = new Uri("https://api.powerbi.com/");
                    using (var client = new HttpClient { BaseAddress = baseAddress })
                    {
                        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + AccessToken);
                        client.DefaultRequestHeaders.Add("Accept", "application/json");
                        using (var response = await client.DeleteAsync(restUrlDatasets + dataset.id + "/tables/" + Tablename + "/rows"))
                        {
                            string responseData = await response.Content.ReadAsStringAsync();
                            Console.WriteLine(responseData);

                        }
                    }
                }
            }
        }

        public static void CreatePrebelDataset()
        {
            string[] namestotales = { "Entregas", "Lineas", "Materiales", "Unidades" };
            string[] datatotales = { "String", "String", "String", "String" };
            string[] formatotales = { "@", "None", "None", "None" };

            string[] tablenames = { "Colas", "Lineas", "Materiales", "OTs", "Unidades" };
            string[] datatypes = { "String", "String", "String", "String", "String" };
            string[] formatstring = { "@", "None", "None", "None", "None" };

            string[] horaname = { "Hora de actualizacion" };
            string[] datahora = { "String" };
            string[] formathora = { "G" };
            JObject[] tablas = new JObject[] { TableJs("Totales", namestotales, datatotales, formatotales), TableJs("Colas", tablenames, datatypes, formatstring), TableJs("Detalle", tablenames, datatypes, formatstring), TableJs("Hora", horaname, datahora, formathora) };
            CreateDataset("PrebelDataset", "Push", tablas).Wait();

            //ListTables("PrebelDataset");
            //var id = GetID("PrebelDataset");
            //AddRows("PrebelDataset");
            //String PathToPush = ProgramConstants.PowerBiServiceRootUrl + "datasets/" + id;
        }

        public static async Task AddRowsPrebel(string TableName, String[] Values)
        {
            string id = GetID("PrebelDataset");
            var baseAddress = new Uri("https://api.powerbi.com/");
            using (var client = new HttpClient { BaseAddress = baseAddress })
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + AccessToken);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                string content;
                string[] keys;
                switch (TableName)
                {
                    case "Colas":
                        keys = new string[] { "Colas", "Lineas", "Materiales", "OTs", "Unidades" };
                        break;
                    case "Detalle":
                        keys = new string[] { "Colas", "Lineas", "Materiales", "OTs", "Unidades" };
                        break;
                    case "Hora":
                        keys = new string[] { "Hora de actualizacion" };
                        break;
                    case "Totales":
                        keys = new string[] { "Entregas", "Lineas", "Materiales", "Unidades" };
                        break;
                    default:
                        keys = new string[] { "" };
                        Console.Error.WriteLine("Usa 1 de las 4 tablas: Colas,Detalle,Hora,Totales");
                        break;
                }
                content = JsonConvert.SerializeObject(RowJs(keys, Values));
                using (var newcontent = new StringContent(content, System.Text.Encoding.Default, "application/json"))
                {
                    using (var response = await client.PostAsync("v1.0/myorg/datasets/" + id + "/tables/" + TableName + "/rows", newcontent))
                    {
                        string responseData = await response.Content.ReadAsStringAsync();
                        Console.WriteLine(responseData);
                    }
                }
            }
        }

        //public static async Task RefreshDataset(string DatasetId)
        //{
        //    var baseAddress = new Uri("https://api.powerbi.com/");
        //    using (var client = new HttpClient { BaseAddress = baseAddress })
        //    {
        //        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + AccessToken);
        //        client.DefaultRequestHeaders.Add("Accept", "application/json");

        //        string[] keys = new string[] { "id", "refreshType", "startTime", "endTime", "status" };
        //        string[] values = new string[] { "251845601", "ViaApi", "2017-07-09T12:06:46.087Z", "2017-07-09T12:07:46.087Z", "Completed" };

        //        JObject json = RowJs(keys, values, "value");
        //        string JsonData = JsonConvert.SerializeObject(json);

        //        using (var content = new StringContent(JsonData, System.Text.Encoding.Default, "application/json"))
        //        {
        //            using (var response = await client.PostAsync("v1.0/myorg/datasets/" + DatasetId + "/refreshes", content))
        //            {
        //                string responseData = await response.Content.ReadAsStringAsync();
        //                Console.WriteLine(responseData);

        //            }
        //        }
        //    }

        //}

        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static string RandomNumber(int length)
        {
            const string chars = "0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        static void Main()
        {

            AcquireAccessToken();
            //CreatePrebelDataset();
            //ListTables("PrebelDataset").Wait();
            
            for (int j = 0; j < 5; j++)
            {
                DeleteRows("PrebelDataset", "Detalle").Wait();
                DeleteRows("PrebelDataset", "Colas").Wait();
                DeleteRows("PrebelDataset", "Totales").Wait();
                DeleteRows("PrebelDataset", "Hora").Wait();
                AddRowsPrebel("Hora", new string[] { String.Format("{0:G}", DateTime.Now) }).Wait();
                for (int i = 0; i < 5; i++)
                {
                    AddRowsPrebel("Detalle", new string[] { RandomString(5), RandomNumber(3), RandomNumber(3), RandomNumber(3), RandomNumber(3) }).Wait();
                    AddRowsPrebel("Colas", new string[] { RandomString(5), RandomNumber(3), RandomNumber(3), RandomNumber(3), RandomNumber(3) }).Wait();
                    AddRowsPrebel("Totales", new string[] { RandomString(5), RandomNumber(3), RandomNumber(3), RandomNumber(3), RandomNumber(3) }).Wait();
                    

                    
                }
                Console.WriteLine("Sleep for 30 seconds.");
                Thread.Sleep(30000);
            }






            //CreatePrebelDataset();
            //ListTables("PrebelDataset");
            //DisplayWorkspaceContents();



        }



    }
}
