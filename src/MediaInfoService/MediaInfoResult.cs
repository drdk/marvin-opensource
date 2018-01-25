
using System;

namespace DR.Marvin.MediaInfoService
{
    public class MediaInfoResult
    {
        public int Duration { get; set; }
        public Video Video { get; set; }
        public Audio Audio { get; set; }
    }

    public class Audio
    {
        public string Format { get; set; }
        public string Channel { get; set; }
    }

    public class Video
    {
        public string CodecId { get; set; }
        public float DisplayAspectRatioRawValue { get; set; }

        public int Height { get; set; }
        public int Width { get; set; }

        private const float ratio_16x9 = 16f/9f;
        private const float ratio_4x3 = 4f/3f;
        private const float maxDiff = 1f/1000f;
        public DisplayAspectRatio DisplayAspectRatio
        {
            get
            {
                if(Math.Abs(DisplayAspectRatioRawValue - ratio_16x9) < maxDiff)
                    return DisplayAspectRatio.ratio_16x9;
                if (Math.Abs(DisplayAspectRatioRawValue - ratio_4x3) < maxDiff)
                    return DisplayAspectRatio.ratio_4x3;
                return DisplayAspectRatio.unknown;
            }
        }
        
        //TODO: Add proper condition checks:
        public Resolution Resolution => Width > 1000 ? Resolution.hd : Resolution.sd;
    }
    public enum DisplayAspectRatio
    {
        unknown = 0,
        ratio_4x3 = 1,
        ratio_16x9 = 2
    }

    public enum Resolution
    {
        unknown = 0,
        sd = 1,
        hd = 2,
        full_hd = 3
    }
}

