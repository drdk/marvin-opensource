using System.ComponentModel.DataAnnotations;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using DR.Marvin.Model;
using DR.Marvin.MediaInfoService;
using DR.Marvin.Plugins.FFMpeg;
using DR.Marvin.Plugins.Wfs;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Resolution = DR.Marvin.Model.Resolution;

[assembly: InternalsVisibleTo("DR.Marvin.WindowsService.Test")]
namespace DR.Marvin.WindowsService.Model
{
    /// <summary>
    /// Marvin order model. Will be validated and converted to a job.
    /// </summary>
    public class Order
    {
        /// <summary>
        /// Full path to file to transcode
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        public string FilePath { get; set; }

        /// <summary>
        /// Full path to intro file 
        /// </summary>
        public string IntroFilePath { get; set; }

        internal int? IntroDuration { get; private set; }
        /// <summary>
        /// Full path to outro file 
        /// </summary>
        public string OutroFilePath { get; set; }
        internal int? OutroDuration { get; private set; }

        /// <summary>
        /// Optional foregin key from system. Should be a valid urn-format. 
        /// </summary>
        [UsedImplicitly]
        public string SourceUrn { get; set; }

        /// <summary>
        /// Optional human readable description/title string. Defaults to filename if not supplied. 
        /// </summary>
        [UsedImplicitly]
        public string Name { get; set; }

        /// <summary>
        /// Should destination files have logo burned in (true/false)
        /// </summary>
        public bool? BurnInLogo { get; set; }

        /// <summary>
        /// Full path to logo
        /// (Optional) Can be left empty if logo is not to be burned in or if default logo is used.
        /// </summary>
        public string LogoPath { get; set; }

        /// <summary>
        /// Full path to the alternate audio sound file
        /// (Optional) Can be left empty if videos original sound is used.
        /// </summary>
        public string AlternateAudioPath { get; set; }


        /// <summary>
        /// Should subtitles be burned in (true/false)
        /// </summary>
        public bool? BurnInSubtitles { get; set; }

        /// <summary>
        /// Full path to subtitles
        /// (Optional) Can be left empty if susbtitles is not to be burned in
        /// </summary>
        public string SubtitlesPath { get; set; }


        /// <summary>
        /// Forces the aspect ratio, use this if the information can not be read from the source file.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public AspectRatio? ForceAspectRatio { get; set; }

        internal AspectRatio AspectRatio
        {
            get { return ForceAspectRatio.GetValueOrDefault(AspectRatio.unknown); }
            set { ForceAspectRatio = value; }
        }

        /// <summary>
        /// Forces the resolution, use this if the information can not be read from the source file.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public Resolution? ForceResolution { get; set; }

        internal Resolution Resolution
        {
            get { return ForceResolution.GetValueOrDefault(Resolution.unknown); }
            set { ForceResolution = value; }
        }

        /// <summary>
        /// Which destination format should be used
        /// Currently supports h264_od_dropfolder / h264_od_single / h264_od_standard
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        [JsonConverter(typeof(StringEnumConverter))]
        public StateFormat DestinationFormat { get; set; }


        /// <summary>
        /// Destination path for transcoded files
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        public string DestinationPath { get; set; }

        /// <summary>
        /// Specified name for the destination files.
        /// Used as a template which supports the following:
        /// %index% replaced with numbers starting from 1.
        /// %ext% replaced with the file extention. 
        /// If filename is provided with without template default is {DestinationFilename}_%index%.%ext%
        /// </summary>
        public string DestinationFilename { get; set; }

        /// <summary>
        /// Priority can be low / medium / high. Defaults to low.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public Priority? Priority { get; set; }

        /// <summary>
        /// When is the order expected to be ready. Defaults to now.
        /// </summary>
        public DateTime? DueDate { get; set; }

        internal DateTime Issued { get; private set; }

        internal bool Validated { get; private set; }

        /// <summary>
        /// Callback URL is called when the transcoding job is done or failed
        /// (Optional)
        /// </summary>
        public string CallbackUrl { get; set; }

        internal StateFormat Format { get; private set; }
        internal string CustomFormat { get; set; }
        internal int Duration { get; private set; }

        private readonly Regex _urnValidation = new Regex(@"^urn:[a-z0-9][a-z0-9-]{0,31}:[a-z0-9()+,\-.:=@;$_!*'%/?#]+$");
        
        /// <summary>
        /// Validate the order instance, may throw OrderException if the order cannot be procesed.
        /// </summary>
        /// <exception cref="OrderException">If the order is unacceptable (:lemon:)</exception>
        public void Validate(IMediaInfoFacade mediaInfoFacade, ITimeProvider timeProvider)
        {
            if (!File.Exists(FilePath))
            {
                throw new OrderException("Filepath does not exist.");
            }

            //Call Media Info for source format and valid source file check
            var arForced = ForceAspectRatio.HasValue && ForceAspectRatio != AspectRatio.unknown;
            var rForced = ForceResolution.HasValue && ForceResolution != Resolution.unknown;
            var infoResult = MediaInfoValidation(mediaInfoFacade, FilePath, arForced, rForced);
            if (!arForced)
                AspectRatio = infoResult.AspectRatio;
            CustomFormat = infoResult.CustomFormat;
            Duration = infoResult.Duration;
            Format = infoResult.Format;
            if (!rForced)
                Resolution = infoResult.Resolution;


            if (BurnInLogo.GetValueOrDefault(false))
            {
                if (!string.IsNullOrEmpty(LogoPath))
                {
                    if (!File.Exists(LogoPath))
                        throw new OrderException("LogoPath does not exist.");

                    if (!(LogoPath.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase) || LogoPath.EndsWith(".jpg", StringComparison.InvariantCultureIgnoreCase) ||
                          LogoPath.EndsWith(".jpeg", StringComparison.InvariantCultureIgnoreCase)))
                        throw new OrderException("LogoPath is not a supported file type. Must be png/jpg/jpeg");
                }
            }

            if (!string.IsNullOrEmpty(AlternateAudioPath))
            {
                if (!File.Exists(AlternateAudioPath))
                    throw new OrderException("AlternateAudioPath does not exist.");

                if (!(AlternateAudioPath.EndsWith(".wav", StringComparison.InvariantCultureIgnoreCase)))
                    throw new OrderException("AlternateAudioPath is not a supported file type. Must be wav format");

                var audioInfo = MediaInfoValidation(mediaInfoFacade, AlternateAudioPath);
                if (audioInfo.Format == StateFormat.custom)
                    throw new OrderException("AlternateAudioPath is not a supported encoding.");

                ValidateAlternateAudioToVideo(mediaInfoFacade, AlternateAudioPath);
                if (!FFMpeg.SupportedAudioMuxingVideoSourceFormats.Contains(Format) || 
                    !FFMpeg.SupportedAudioSourceFormats.Contains(audioInfo.Format))
                {
                    throw new OrderException($"Alternative audio format {audioInfo.Format} mux to {Format} is invalid.");
                }
            }

            if (BurnInSubtitles.GetValueOrDefault(false))
            {
                if (!File.Exists(SubtitlesPath))
                    throw new OrderException("SubtitlesPath does not exist.");

                if (!FFMpeg.SupportedSubtitleExtensions.Any(e=> SubtitlesPath.EndsWith(e, StringComparison.InvariantCultureIgnoreCase)))
                    throw new OrderException($"SubtitlesPath is not a supported file type. Must be one of {FFMpeg.SupportedSubtitleExtensions.Aggregate((c,n)=>$"{c}, {n}")}");
            }
            if ((FFMpeg.SupportedAudioDestinationFormats.Contains(DestinationFormat) && !FFMpeg.SupportedAudioSourceFormats.Contains(Format)) ||
                (Wfs.SupportedDestinationFormats.Contains(DestinationFormat) && !Wfs.SupportedSourceFormats.Contains(Format)))
            {
                throw new OrderException($"DestinationFormat {DestinationFormat} is invalid for source format {Format}.");
            }
            if (DestinationFormat == StateFormat.unknown)
            {
                throw new OrderException("DestinationFormat is invalid.");
            }

            if (DestinationFormat.GetType().GetField(DestinationFormat.ToString()).CustomAttributes.ToList()
                    .Find(a => a.AttributeType == typeof(ObsoleteAttribute)) != null)
            {
                throw new OrderException($"DestinationFormat is obsolete. {DestinationFormat}");
            }

            if (!Directory.Exists(DestinationPath))
            {
                throw new OrderException("DestinationPath does not exist.");
            }

            IntroDuration = ValidateStichAttachments(IntroFilePath, nameof(IntroFilePath), mediaInfoFacade);
            OutroDuration = ValidateStichAttachments(OutroFilePath, nameof(OutroFilePath), mediaInfoFacade);

            //TODO: Disabled until further notice. This check fails as it doens't check "Everyone". We might need to rethink this entirely. :bomb:
            ////Check if we have write permission on destination
            //var acc = new NTAccount(Environment.UserName);            
            //var accessGranted = false;
            //string rights = $"Rights found on {acc.Value}: ";
            //var secId = acc.Translate(typeof(SecurityIdentifier)) as SecurityIdentifier;
            //if (secId == null)
            //    throw new OrderException($"Unable to read SecurityIdentifier from {Environment.UserName}");
            //var dInfo = new DirectoryInfo(DestinationPath);
            //var dSecurity = dInfo.GetAccessControl();
            //var rules = dSecurity.GetAccessRules(true, true, typeof(SecurityIdentifier));

            //foreach (FileSystemAccessRule ar in rules)
            //{
            //    if (secId.CompareTo(ar.IdentityReference as SecurityIdentifier) == 0)
            //    {
            //        Debug.WriteLine($"We found the rights for the user: {ar.FileSystemRights}");
            //        rights += ar.FileSystemRights + " ";
            //        if ((ar.FileSystemRights & FileSystemRights.Write) == FileSystemRights.Write)
            //        {
            //            Debug.WriteLine("ACCESS GRANTED!");
            //            accessGranted = true;
            //        }
            //    }
            //}
            //if (!accessGranted) throw new OrderException("Can't write to destination. Only found following: "+ rights);      

            if (!Priority.HasValue)
                Priority = Marvin.Model.Priority.low;

            if (!string.IsNullOrEmpty(CallbackUrl) && !Uri.IsWellFormedUriString(CallbackUrl, UriKind.Absolute))
            {
                throw new OrderException("Callback URL is not valid");
            }

            if (!string.IsNullOrEmpty(DestinationFilename))
            {
                var invalidChars = Path.GetInvalidFileNameChars();
                foreach (var c in DestinationFilename.Where(c => invalidChars.Contains(c)))
                {
                    throw new OrderException($"{DestinationFilename} contains illigal character: {c}");
                }
            }

            Issued = timeProvider.GetUtcNow();
            if (!DueDate.HasValue || DueDate.Value < Issued)
                DueDate = Issued;

            if (string.IsNullOrEmpty(Name))
                Name = new FileInfo(FilePath).Name;

            if (string.IsNullOrEmpty(SourceUrn))
                SourceUrn = null;
            else if (!_urnValidation.IsMatch(SourceUrn))
                throw new OrderException($"Invalid source urn {SourceUrn} supplied");

            Validated = true;
        }

        private static void ValidateAudioInVideo(MediaInfoResult mi)
        {
            if (mi?.Audio == null)
                return;

            var audioStream = mi.Audio;

            //TODO: Reject unsupported audio
            //if (!Enum.IsDefined(typeof(StateFormat),audioStream.Format))
            //    throw new OrderException("Audio is not in a valid format.");
            
            var allowedChannelCount = new[] { "1" };
            if (Array.Exists(allowedChannelCount, element => element.Equals(audioStream.Channel)))
                throw new OrderException("Only audio with more than 1 channel is supported.");
        }

        private static void ValidateAlternateAudioToVideo(IMediaInfoFacade mediaInfoFacade, string audioPath)
        {
            var result = new MediaInfoValidationResult();
            MediaInfoResult mi;
            try
            {
                mi = mediaInfoFacade.Read(audioPath);

            }
            catch (Exception ex)
            {
                throw new OrderException("Error while reading metadata from file. " + ex.Message);
            }
            ValidateAudioInVideo(mi);
        }


        private static MediaInfoValidationResult MediaInfoValidation(IMediaInfoFacade mediaInfoFacade, string filePath, bool skipAsceptRatioValidation = false, bool skipResolutionValidation = false)
        {
            var result = new MediaInfoValidationResult();
            MediaInfoResult mi;
            try
            {
                mi = mediaInfoFacade.Read(filePath);
                
            }
            catch (Exception ex)
            {
                throw new OrderException("Error while reading metadata from file. " + ex.Message);
            }

            if (mi != null)
                result.Duration = mi.Duration;

            if (mi?.Video != null)
            {
                var videoStream = mi.Video;
                StateFormat temp;
                if (Enum.TryParse(videoStream.CodecId, out temp))
                    result.Format = temp;
                else
                {
                    // TODO: Temp work around , allows unknow formats. Should rather throw when we know all allowed formats.
                    //throw new OrderException("File is not in a valid format. Source file must be dvpp, dv5p or xd5c format.");
                    result.Format = StateFormat.custom;
                    result.CustomFormat = videoStream.CodecId;
                }
                if (!skipAsceptRatioValidation)
                    switch (videoStream.DisplayAspectRatio)
                    {
                        case DisplayAspectRatio.ratio_4x3:
                            result.AspectRatio = AspectRatio.ratio_4x3;
                            break;
                        case DisplayAspectRatio.ratio_16x9:
                            result.AspectRatio = AspectRatio.ratio_16x9;
                            break;
                        case DisplayAspectRatio.unknown:
                        default:
                            throw new OrderException("Unsupport aspect ratio. Only 16x9 and 4x3 is supported.");
                    }

                if(!skipResolutionValidation)
                    switch (videoStream.Resolution)
                    {
                        case MediaInfoService.Resolution.sd:
                            result.Resolution = Resolution.sd;
                            break;
                        case MediaInfoService.Resolution.hd:
                            result.Resolution = Resolution.hd;
                            break;
                        case MediaInfoService.Resolution.full_hd:
                            result.Resolution = Resolution.fullhd;
                            break;
                        case MediaInfoService.Resolution.unknown:
                        default:
                            throw new OrderException("Unsupport resultion. Only sd, hd and fullhd is supported.");
                    }

                ValidateAudioInVideo(mi);
            }
            else if (mi?.Audio != null)
            {
                var audioStream = mi.Audio;
                StateFormat temp;

                if (Enum.TryParse(audioStream.Format, out temp))
                    result.Format = temp;
                else
                {
                    // TODO: Temp work around , allows unknow formats. Should rather throw when we know all allowed formats.
                    result.Format = StateFormat.custom;
                    result.CustomFormat = audioStream.Format;
                }
            }
            else
            {
                throw new OrderException("File is not a valid media file. Source file must be dv5p or xd5c for video or wma, mpeg or pcm for audio format or wav for alternate audio.");
            }
            
            return result;
        }

        private static int? ValidateStichAttachments(string value, string propertyName, IMediaInfoFacade mediaInfoFacade)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            if (!(value.EndsWith(".wav", StringComparison.InvariantCultureIgnoreCase) || value.EndsWith(".mp3", StringComparison.InvariantCultureIgnoreCase)))
                throw new OrderException($"{propertyName} must be wav or mp3 file.");

            if (!File.Exists(value))
                throw new OrderException($"{propertyName} does not exist.");

            try
            {
                var res = MediaInfoValidation(mediaInfoFacade, value, skipAsceptRatioValidation:true, skipResolutionValidation:true);
                #if DEBUG
                Console.WriteLine($"{res.Format} {res.CustomFormat??""}");
                #endif
                return res.Duration;
            }
            catch (Exception ex)
            {
                throw new OrderException($"{propertyName}: " + ex.Message);
            }
        }


        private class MediaInfoValidationResult
        {
            public AspectRatio AspectRatio { get; internal set; }
            public string CustomFormat { get; internal set; }
            public int Duration { get; internal set; }
            public StateFormat Format { get; internal set; }
            public Resolution Resolution { get; internal set; }
        }
    }
}