using System;
using System.Collections.Generic;
using AutoMapper;

namespace DR.Marvin.Model
{
    /// <summary>
    /// Essence class, represent either a concrete instance of files or a desire destination state.
    /// A To and From pair of essences represent a desired transformation between the two. 
    /// </summary>
    public class Essence
    {
        /// <summary>
        /// Root path of the of file(s)
        /// </summary>
        public string Path { get; set; }
        /// <summary>
        /// Collection of file(s) or templates
        /// </summary>
        public IList<EssenceFile> Files { get; set; }
        /// <summary>
        /// The preset/format type.
        /// </summary>
        public StateFormat Format { get; set; }
        /// <summary>
        /// Optional format string, only sat if Format is custom
        /// </summary>
        public string CustomFormat { get; set; }
        /// <summary>
        /// List of attachement, optional "side wagons". May include addtional subtitles or audio tracks.
        /// </summary>
        public IList<Attachment> Attachments { get; set; }
        /// <summary>
        /// Collection of added attributes, flag enums. 
        /// </summary>
        public StateFlags Flags { get; set; }
        /// <summary>
        /// Aspect ratio, 4x3 or 16x9
        /// </summary>
        public AspectRatio AspectRatio { get; set; }
        /// <summary>
        /// Resolution, sd, hd ect.
        /// </summary>
        public Resolution Resolution { get; set; }
        /// <summary>
        /// Duration in miliseconds.
        /// </summary>
        public int Duration { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public Essence() { }

        /// <summary>
        /// Cloning constructor
        /// </summary>
        public Essence(Essence source)
        {
            Mapper.Map(source, this);
        }
    }

    // Enums - :warning: DO NOT change values after they have been used in production. 
    // Otherwise data integrity will be compromised! :skull: 
    #region Enums

    #pragma warning disable 1591
    // ReSharper disable InconsistentNaming
    
    /// <summary>
    /// State flags enums. 
    /// Values must be increced in powers of twos.
    /// </summary>
    [Flags]
    public enum StateFlags
    {
        None = 0,
        Logo = 1,
        HardSubtitles = 2,
        AlternativeAudio = 4,
        Intro = 8,
        Outro = 16
    }

    /// <summary>
    /// Format enums.
    /// </summary>
    public enum StateFormat
    {
        unknown = 0,

        // internal formats < 100
        // video
        xd5c = 1,
        dvpp = 2,
        dv5p = 3,
        dvh5 = 4,
        dv = 5,
        dvhq = 6,
        avc1 = 7,
        
        // audio
        wma = 50,
        mpeg_audio = 51,
        pcm = 52,

        custom = 99,

        // end user formats > 100
        // video
        [Obsolete]
        h264_od_q1 = 101,
        [Obsolete]
        h264_od_q3 = 102,
        [Obsolete]
        h264_od_q5 = 103,

        h264_od_single = 104,
        h264_od_standard = 105,
        h264_od_dropfolder = 106,
        h264_od_podcast = 107,

        // audio
        //audio_od_single = 150, //This is not needed by anyone
        audio_od_standard = 151,
    }
    /// <summary>
    /// Resolution enums.
    /// </summary>
    public enum Resolution
    {
        unknown = 0,
        sd = 1,
        hd = 2,
        fullhd = 3
    }

    /// <summary>
    /// Aspect ratio enums.
    /// </summary>
    public enum AspectRatio
    {
        unknown = 0,
        ratio_16x9 = 1,
        ratio_4x3 = 2
    }

    // ReSharper enable InconsistentNaming
    #pragma warning restore 1591
    
    #endregion
}
