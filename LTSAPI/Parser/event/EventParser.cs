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
using LTSAPI.Utils;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LTSAPI.Parser
{
    public class EventParser
    {
        public static (List<EventV2>, VenueV2) ParseLTSEvent(
            JObject activitylts, bool reduced
            )
        {
            try
            {
                LTSEvent eventltsdetail = activitylts.ToObject<LTSEvent>();

                return ParseLTSEvent(eventltsdetail.data, reduced);
            }
            catch(Exception ex)
            {           
                return (null, null);
            }          
        }

        public static (List<EventV2>, VenueV2) ParseLTSEvent(
            LTSEventData activity, 
            bool reduced)
        {
            EventV2 eventv2 = new EventV2();

            eventv2.Id = activity.rid;
            eventv2._Meta = new Metadata() { Id = eventv2.Id, LastUpdate = DateTime.Now, Reduced = reduced, Source = "lts", Type = "event", UpdateInfo = new UpdateInfo() { UpdatedBy = "importer.v2", UpdateSource = "lts.interface.v2" } };
            eventv2.Source = "lts";

            eventv2.LastChange = activity.lastUpdate;
            
            //Detail Information

            //Contact Information

            //Opening Schedules

            //Tags

            //Images

            //Videos

            //Custom Fields


            return (new List<EventV2>() { eventv2 }, new VenueV2());
        }

        public static EventLinked ParseLTSEventV1(
           JObject activitylts, bool reduced
           )
        {
            try
            {
                LTSEvent eventltsdetail = activitylts.ToObject<LTSEvent>();

                return ParseLTSEventV1(eventltsdetail.data, reduced);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static EventLinked ParseLTSEventV1(
            LTSEventData ltsevent,
            bool reduced)
        {
            EventLinked eventv1 = new EventLinked();

            eventv1.Id = ltsevent.rid;
            eventv1._Meta = new Metadata() { Id = eventv1.Id, LastUpdate = DateTime.Now, Reduced = reduced, Source = "lts", Type = "event", UpdateInfo = new UpdateInfo() { UpdatedBy = "importer.v2", UpdateSource = "lts.interface.v2" } };
            eventv1.Source = "lts";

            eventv1.LastChange = ltsevent.lastUpdate;
            eventv1.FirstImport = ltsevent.createdAt;

            eventv1.Active = ltsevent.isActive;

            if (eventv1.TagIds == null)
                eventv1.TagIds = new List<string>();

            //Let's find out for which languages there is a name
            foreach (var name in ltsevent.name)
            {
                if (!String.IsNullOrEmpty(name.Value))
                    eventv1.HasLanguage.Add(name.Key);
            }

            //Classification
            eventv1.ClassificationRID = ltsevent.classification.rid;
            
            //Topics
            foreach (var category in ltsevent.categories)
            {
                if(eventv1.TopicRIDs == null)
                    eventv1.TopicRIDs = new List<string>();

                eventv1.TopicRIDs.Add(category.rid);
                eventv1.TagIds.Add(category.rid);
            }

            //Tags
            foreach (var tag in ltsevent.tags)
            {
                eventv1.TagIds.Add(tag.rid);
            }

            //Districts
            foreach(var dist in ltsevent.districts)
            {
                if(eventv1.DistrictIds == null)
                    eventv1.DistrictIds = new List<string>();

                eventv1.DistrictIds.Add(dist.rid);
            }

            //Detail Information
            foreach (var language in eventv1.HasLanguage)
            {
                Detail detail = new Detail();

                detail.Language = language;
                detail.Title = ltsevent.name[language];
                detail.BaseText = ltsevent.descriptions.Where(x => x.type == "longDescription").FirstOrDefault()?.description.GetValue(language);
                detail.IntroText = ltsevent.descriptions.Where(x => x.type == "shortDescription").FirstOrDefault()?.description.GetValue(language);
                detail.ParkingInfo = ltsevent.descriptions.Where(x => x.type == "howToPark").FirstOrDefault()?.description.GetValue(language);
                detail.GetThereText = ltsevent.descriptions.Where(x => x.type == "howToArrive").FirstOrDefault()?.description.GetValue(language);
                
                detail.AdditionalText = ltsevent.descriptions.Where(x => x.type == "routeDescription").FirstOrDefault()?.description.GetValue(language);
                detail.PublicTransportationInfo = ltsevent.descriptions.Where(x => x.type == "publicTransport").FirstOrDefault()?.description.GetValue(language);
                detail.AuthorTip = ltsevent.descriptions.Where(x => x.type == "authorTip").FirstOrDefault()?.description.GetValue(language);
                detail.SafetyInfo = ltsevent.descriptions.Where(x => x.type == "safetyInstructions").FirstOrDefault()?.description.GetValue(language);
                detail.EquipmentInfo = ltsevent.descriptions.Where(x => x.type == "equipment").FirstOrDefault()?.description.GetValue(language);

                eventv1.Detail.TryAddOrUpdate(language, detail);
            }

            //Contact Information
            foreach (var language in eventv1.HasLanguage)
            {
                ContactInfos contactinfo = new ContactInfos();

                contactinfo.Language = language;
                contactinfo.CompanyName = ltsevent.contact.address.name.GetValue(language);
                contactinfo.Address = ltsevent.contact.address.street.GetValue(language);
                contactinfo.City = ltsevent.contact.address.city.GetValue(language);
                contactinfo.CountryCode = ltsevent.contact.address.country;
                contactinfo.ZipCode = ltsevent.contact.address.postalCode;
                contactinfo.Email = ltsevent.contact.email.GetValue(language);
                contactinfo.Phonenumber = ltsevent.contact.phone;
                contactinfo.Url = ltsevent.contact.website.GetValue(language);

                eventv1.ContactInfos.TryAddOrUpdate(language, contactinfo);
            }

            //Opening Schedules

            //Position
            if (ltsevent.position != null && ltsevent.position.coordinates.Length == 2)
            {
                if (eventv1.GpsInfo == null)
                    eventv1.GpsInfo = new List<GpsInfo>();

                GpsInfo gpsinfo = new GpsInfo();
                gpsinfo.Gpstype = "position";
                gpsinfo.Latitude = ltsevent.position.coordinates[1];
                gpsinfo.Longitude = ltsevent.position.coordinates[0];
                gpsinfo.Altitude = ltsevent.position.altitude;
                gpsinfo.AltitudeUnitofMeasure = "m";

                eventv1.GpsInfo.Add(gpsinfo);
            }

            //Images
            //Images (Main Images with ValidFrom)
            List<ImageGallery> imagegallerylist = new List<ImageGallery>();

            if (ltsevent.images != null)
            {
                foreach (var image in ltsevent.images)
                {
                    ImageGallery imagepoi = new ImageGallery();

                    imagepoi.ImageName = image.rid;
                    imagepoi.ImageTitle = image.name;
                    imagepoi.CopyRight = image.copyright;
                    imagepoi.License = image.license;

                    imagepoi.ImageUrl = image.url;
                    imagepoi.IsInGallery = true;

                    imagepoi.Height = image.heightPixel;
                    imagepoi.Width = image.widthPixel;
                    imagepoi.ValidFrom = image.applicableStartDate;
                    imagepoi.ValidTo = image.applicableEndDate;
                    imagepoi.ListPosition = image.order;

                    imagegallerylist.Add(imagepoi);
                }
            }

            eventv1.ImageGallery = imagegallerylist;

            //TO ADD
            //publisherSettings
            //periods
            //meetingPoint
            //location
            //variants
            //registration
            //shopConfiguration
            //urlAlias
            //urls


            //Custom Fields
            //Mapping
            var ltsmapping = new Dictionary<string, string>();
            ltsmapping.Add("rid", ltsevent.rid);
            ltsmapping.Add("organizer", ltsevent.organizer.rid);
            ltsmapping.Add("isRegistrationRequired", ltsevent.isRegistrationRequired.ToString());
            ltsmapping.Add("isTicketRequired", ltsevent.isTicketRequired.ToString());
            ltsmapping.Add("organizer", ltsevent.organizer.rid);
            ltsmapping.Add("organizer", ltsevent.organizer.rid);
            ltsmapping.Add("organizer", ltsevent.organizer.rid);


            eventv1.Mapping.TryAddOrUpdate("lts", ltsmapping);

            return eventv1;
        }
    }
}
