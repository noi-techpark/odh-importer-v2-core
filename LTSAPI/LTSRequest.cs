﻿using System.Net;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net.NetworkInformation;
using Helper;

namespace LTSAPI
{
    public class LtsApi
    {
        LTSCredentials credentials;
        string baseurl;
        Dictionary<string,string>? parameters;
        string endpoint;
        bool getallpages = false;

        public LtsApi(LTSCredentials _credentials) 
        {
            this.baseurl = "https://go.lts.it/api/v1";
            this.credentials = _credentials;                        
        }

        private async Task<HttpResponseMessage> LTSRESTRequest()
        {
            try
            {
                var querystring = parameters != null ? "?" + string.Join("&", parameters.Select(x => String.Join("=", x.Key, x.Value))) : "";
                var serviceurl = baseurl + "/" + endpoint + querystring;
             
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials.username + ":" + credentials.password)));
                    client.DefaultRequestHeaders.Add("X-LTS-ClientID", credentials.ltsclientid);

                    var myresponse = await client.GetAsync(serviceurl);

                    return myresponse;
                }
            }
            catch (Exception ex)
            {
                return new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest, Content = new StringContent(ex.Message) };
            }
        }

        private async Task<List<JObject>> RequestAndParseToJObject()
        {
            var response = await LTSRESTRequest();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var jobjectlist = new List<JObject>() { };

                var contentstr = await response.Content.ReadAsStringAsync();

                if (String.IsNullOrEmpty(contentstr))
                {
                    jobjectlist.Add(new JObject(new { error = true, message = "LTS Api error", exception = "No data from api" }));
                    return jobjectlist;
                }

                var contentjson = (JObject)JsonConvert.DeserializeObject(contentstr);

                if (contentjson == null)
                {
                    jobjectlist.Add(new JObject(new { error = true, message = "LTS Api error ", exception = "Deserialization failed" }));
                    return jobjectlist;
                }

                jobjectlist.Add(contentjson);

                //Get all Pages and add the response
                if (getallpages)
                {
                    var pagesquantity = (string)contentjson["paging"]["pagesQuantity"];
                    int.TryParse(pagesquantity, out int pagesquantityint);

                    var currentpage = (string)contentjson["paging"]["pageNumber"];
                    int.TryParse(currentpage, out int currentpageint);

                    if(currentpageint < pagesquantityint)
                    {
                        //Add page parameter
                        parameters.TryAddOrUpdate("page[number]", (currentpageint + 1).ToString());
                        //Request again
                        jobjectlist.AddRange(await RequestAndParseToJObject());
                    }                  
                }

                return jobjectlist;
            }
            else
            {
                return new List<JObject>() { new JObject(new { error = true, message = "LTS Api error ", exception = await response.Content.ReadAsStringAsync() }) };
            }
        }

        private async Task<List<JObject>> LTSRequestMethod(string _endpoint, Dictionary<string, string>? _parameters, bool _getallpages)
        {
            endpoint = _endpoint;
            parameters = _parameters == null ? new Dictionary<string, string>() : _parameters;
            getallpages = _getallpages;

            return await RequestAndParseToJObject();
        }


        #region Accommodation

        public async Task<List<JObject>> AccommodationAmenitiesRequest(Dictionary<string, string>? _parameters, bool _getallpages)
        {            
            return await LTSRequestMethod("amenities", _parameters, _getallpages);
        }

        public async Task<List<JObject>> AccommodationCategoriesRequest(Dictionary<string, string>? _parameters, bool _getallpages)
        {
            return await LTSRequestMethod("accommodations/categories", _parameters, _getallpages);
        }

        public async Task<List<JObject>> AccommodationTypesRequest(Dictionary<string, string>? _parameters, bool _getallpages)
        {            
            return await LTSRequestMethod("accommodations/types", _parameters, _getallpages);
        }

        public async Task<List<JObject>> AccommodationListRequest(Dictionary<string, string>? _parameters, bool _getallpages)
        {            
            return await LTSRequestMethod("accommodations", _parameters, _getallpages);
        }

        public async Task<List<JObject>> AccommodationDetailRequest(string id, Dictionary<string, string>? _parameters)
        {
            return await LTSRequestMethod("accommodations/" + id, _parameters, false);            
        }

        public async Task<List<JObject>> AccommodationDeleteRequest(Dictionary<string, string>? _parameters, bool _getallpages)
        {            
            return await LTSRequestMethod("accommodations/deleted", _parameters, _getallpages);
        }

        #endregion

        #region Poi

        public async Task<List<JObject>> PoiListRequest(Dictionary<string, string>? _parameters, bool _getallpages)
        {
            return await LTSRequestMethod("pointofinterests", _parameters, _getallpages);
        }

        public async Task<List<JObject>> PoiDetailRequest(string id, Dictionary<string, string>? _parameters, bool _getallpages)
        {
            return await LTSRequestMethod("pointofinterests/" + id, _parameters, false);
        }

        #endregion

        #region Activity

        #endregion

        #region Gastronomy

        #endregion

        #region Beacon

        #endregion

        #region Event

        #endregion

        #region Venue

        #endregion

        #region Webcam

        #endregion

        #region WeatherSnow

        #endregion

        #region Tag

        #endregion

        #region QueryString Helper

        public IDictionary<string, string> GetLTSQSDictionary(LTSQueryStrings qs)
        {
            IDictionary<string, string> qsdict = new Dictionary<string, string>();
            
            foreach (var prop in qs.GetType().GetProperties())
            {
                string propkey = "";
                var propkeysplitted = prop.Name.Split("_");
                int i = 0;
                foreach(var pk in propkeysplitted)
                {
                    if (i == 0)
                        propkey = propkey + pk;
                    else
                        propkey = propkey + "[" + pk + "]";

                    i++;
                }

                var propvalue = prop.GetValue(qs, null);

                if (propvalue != null && !String.IsNullOrEmpty(propvalue.ToString()))
                    qsdict.Add(propkey, propvalue.ToString());
            }

            return qsdict;
        }

        #endregion
    }

    public class LTSCredentials
    {
        public string ltsclientid { get; set; }
        public string username { get; set; }
        public string password { get; set; }
    }

    public class LTSQueryStrings
    {        
        public string? language { get; set; }

        public int? page_number { get; set; }

        public int? page_size { get; set; }

        public string? filter_language { get; set; }
    }
    
}
