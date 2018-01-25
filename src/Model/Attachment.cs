using System.Collections.Generic;

namespace DR.Marvin.Model
{
    /// <summary>
    /// Sidewagon - e.g. alternative soundtrack
    /// </summary>
    public class Attachment
    {
        /// <summary>
        /// File path to attachement 
        /// </summary>
        public string Path { get; set; }


        private IDictionary<string, string> _arguments;

        /// <summary>
        /// Optional parameters, where should the attachment be applied 
        /// </summary>
        public IDictionary<string, string> Arguments
        {
            get { return _arguments ?? (_arguments = new Dictionary<string, string>()); }
            set { _arguments = value; }
        }

        /// <summary>
        /// Attachement type, eg. subtitle, audio or logo
        /// </summary>
        public AttachmentType Type { get; set; }
    }

    /// <summary>
    /// Types of attachements,  eg. subtitle, audio or logo
    /// </summary>
    public enum AttachmentType
    {
        #pragma warning disable 1591
        Subtitle,
        Audio,
        Logo,
        Intro,
        Outro
        #pragma warning restore 1591
    }
}
