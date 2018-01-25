#pragma warning disable 1591

namespace DR.Marvin.WindowsService.Model
{
    /// <summary>
    /// Common error view model
    /// </summary>
    public class Error
    {
        public string Message { get; set; }
        public string ExceptionMessage { get; set; }
        public string ExceptionType { get; set; }
        public string StackTrace { get; set; }
    }
}
