﻿using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using VrtNuDownloader.Models.Vrt.Api;
using VrtNuDownloader.Service.Interface;

namespace VrtNuDownloader.Service
{
    public class VrtNuService : IVrtNuService
    {
        private readonly ILogService _logService;
        private readonly IVrtTokenService _vrtTokenService;

        public VrtNuService
            (
                ILogService logService,
                IVrtTokenService vrtTokenService
            )
        {
            _logService = logService;
            _vrtTokenService = vrtTokenService;
        }

        public Uri[] GetShowSeasons(Uri showUri)
        {
            HtmlWeb web = new HtmlWeb();
            HtmlDocument html = web.Load(showUri);
            var seasonSelectOIptions = html.DocumentNode.SelectNodes("//*[@class=\"vrt-labelnav\"]")
                ?.FirstOrDefault()
                ?.SelectNodes(".//li//a");

            if ((seasonSelectOIptions?.Count ?? 0) <= 1)
                return new Uri[] { showUri };

            return seasonSelectOIptions
                .Select(x => new Uri("https://www.vrt.be" + x.GetAttributeValue("href", "")))
                .OrderBy(x => x.AbsoluteUri)
                .ToArray();
        }

        public Uri[] GetShowSeasonEpisodes(Uri seasonUri)
        {
            HtmlDocument html = new HtmlWeb().Load(seasonUri);
            return html.DocumentNode.SelectSingleNode("//ul[@aria-labelledby='episodelist-title']")
                ?.SelectNodes(".//li//a")
                ?.Select(x => new Uri("https://www.vrt.be" + x.GetAttributeValue("href", "")))
                .OrderBy(x => x.AbsoluteUri)
                .ToArray();
        }

        public VrtContent GetEpisodeInfo(Uri episodeUri)
        {
            var episodeURL = episodeUri.AbsoluteUri;
            var contentJsonURL = episodeURL.Remove(episodeURL.Length - 1) + ".content.json";
            var contentJson = new WebClient().DownloadString(contentJsonURL);
            return JsonConvert.DeserializeObject<VrtContent>(contentJson);
        }

        public VrtPbsPub GetPublishInfo(string publicationId, string videoId)
        {
            var pbsPubURL = $"https://mediazone.vrt.be/api/v1/vrtvideo/assets/{publicationId}${videoId}";
            var pbsPubJson = new WebClient().DownloadString(pbsPubURL);
            return JsonConvert.DeserializeObject<VrtPbsPub>(pbsPubJson);
        }

        public VrtContent GetEpisodeInfoV2(Uri episodeUri)
        {
            var epInfo = new VrtContent();
            try
            {
                epInfo = GetEpisodeInfo(episodeUri);
            }
            catch
            {
                _logService.WriteLog(MessageType.Error, "Old episode info failed");
            }
            finally
            {
                HtmlDocument html = new HtmlWeb().Load(episodeUri);
                var div = html.DocumentNode.SelectSingleNode("//div[@class='vrtvideo']");
                epInfo.publicationId = div.GetAttributeValue("data-publicationid", "");
                epInfo.videoId = div.GetAttributeValue("data-videoid", "");
            }
            return epInfo;
        }

        public VrtPbsPubv2 GetPublishInfoV2(string publicationId, string videoId)
        {
            var pbsPubURL = $"https://media-services-public.vrt.be/vualto-video-aggregator-web/rest/external/v1/videos/{publicationId}${videoId}?vrtPlayerToken={_vrtTokenService.PlayerToken}&client=vrtvideo";
            var pbsPubJson = new WebClient().DownloadString(pbsPubURL);
            return JsonConvert.DeserializeObject<VrtPbsPubv2>(pbsPubJson);
        }
    }
}
