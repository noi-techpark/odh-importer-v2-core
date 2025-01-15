﻿using DataModel;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Xml.Linq;
using GenericHelper;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Newtonsoft.Json;

namespace LTSAPI.Parser
{
    public class AccommodationRateplanParser
    {
        public static TagLinked ParseLTSAccommodationMealplan(
            JObject ltsresult, bool reduced
            )
        {
            string dataid = "";
            try
            {
                var ltsdata = ltsresult.ToObject<LTSData<LTSAccommodationRateplanData>>();
                dataid = ltsdata.data.rid;

                return ParseLTSAccommodationRateplan(ltsdata.data, reduced);
            }
            catch(Exception ex)
            {
                //Generate Log Response on Error
                Console.WriteLine(JsonConvert.SerializeObject(new { operation = "accommodation.rateplan.parse", id = dataid, source = "lts", success = false, error = true, exception = ex.Message }));

                return null;
            }          
        }

        public static TagLinked ParseLTSAccommodationRateplan(
            LTSAccommodationRateplanData data, 
            bool reduced)
        {
            List<string> typelistlts = new List<string>();

            TagLinked objecttosave = new TagLinked();

            objecttosave.Id = data.rid;
            objecttosave.Active = true;
            objecttosave.DisplayAsCategory = false;
            objecttosave.FirstImport =
                objecttosave.FirstImport == null ? DateTime.Now : objecttosave.FirstImport;
            objecttosave.LastChange = data.lastUpdate;

            objecttosave.Source = "lts";
            objecttosave.TagName = data.name;
            objecttosave.Description = data.description;

            objecttosave.MainEntity = "accommodation";
            objecttosave.ValidForEntity = new List<string>() { "accommodation" };
            objecttosave.Shortname = objecttosave.TagName.ContainsKey("en")
                ? objecttosave.TagName["en"]
                : objecttosave.TagName.FirstOrDefault().Value;
            objecttosave.Types = new List<string>() { "accommodationrateplans" };

            if (!typelistlts.Contains("accommodationrateplans"))
                typelistlts.Add("accommodationrateplans");

            //objecttosave.IDMCategoryMapping = null;
            objecttosave.PublishDataWithTagOn = null;
            objecttosave.Mapping = new Dictionary<string, IDictionary<string, string>>()
                    {
                        {
                            "lts",
                            new Dictionary<string, string>()
                            {
                                { "rid", data.rid },
                                { "code", data.code },
                                { "alpineBitsRatePlanId", data.alpineBitsRatePlanId },
                                { "chargeType", data.chargeType },
                                { "areChildrenAllowed", data.areChildrenAllowed.ToString() },
                            }
                        },
                    };
            objecttosave.LTSTaggingInfo = null;
            objecttosave.PublishedOn = null;

            return objecttosave;
        }
    }
}
