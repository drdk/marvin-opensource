namespace DR.Marvin.Model
{
    /// <summary>
    /// Essence filename or template class
    /// </summary>
    public class EssenceFile 
    {
        /// <summary>
        /// string value of the class
        /// </summary>
        public string Value { get; }
        /// <summary>
        /// Type defenition , template or filename
        /// </summary>
        public EssenceFileKind Kind { get; }

        /// <summary>
        /// Contructor
        /// </summary>
        /// <param name="value">String value</param>
        /// <param name="kind">EssenceFile type, template or filename</param>
        public EssenceFile(string value, EssenceFileKind kind)
        {
            Value = value;
            Kind = kind;
        }
        
        /// <summary>
        /// Implicit string conversion to filename
        /// </summary>
        /// <param name="filename">string value of new essenceFile with kind as filename</param>
        public static implicit operator EssenceFile(string filename)
        {
            return new EssenceFile(filename, EssenceFileKind.Filename);
        }
        
        /// <summary>
        /// To string operate. returns .Value
        /// </summary>
        /// <param name="file">Target EssenceFile</param>
        public static implicit operator string(EssenceFile file)
        {
            return file.Value;
        }

        /// <summary>
        /// Factory method to create template without the contructor
        /// </summary>
        /// <param name="template">string value of new essenceFile with kind as template</param>
        /// <returns></returns>
        public static EssenceFile Template(string template)
        {
            return new EssenceFile(template, EssenceFileKind.Template);
        }

    }

    /// <summary>
    /// Enum to define wheterornot essence is a template or a filename
    /// </summary>
    public enum EssenceFileKind
    {
        #pragma warning disable 1591
        Filename = 1,
        Template = 2
        #pragma warning restore 1591
    }
}