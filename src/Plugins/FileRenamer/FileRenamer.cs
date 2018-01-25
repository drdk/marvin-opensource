using System;
using System.IO;
using System.Text.RegularExpressions;
using DR.Marvin.Model;
using DR.Marvin.Plugins.Common;

namespace DR.Marvin.Plugins.FileRenamer
{
    public class FileRenamer : PluginBase
    {
        public static readonly string Type = nameof(FileRenamer).ToLower();
        public static string UrnPrefix => $"{UrnHelper.UrnBase}{PluginBaseUrn}{Type}:";
        public override string PluginType => Type;
        public override bool AsyncOperation => false;

        public FileRenamer(string urn, ITimeProvider timeProvider, ILogging logging) : base(urn, Type, timeProvider, logging)
        {
        }

        public override bool CheckAndEstimate(ExecutionTask task)
        {
            if (task.To.Files?.Count != 1)
                return false;

            var template = task.To.Files[0].Value;

            var match = parameterDetection.Match(template);
            if (!match.Success)
                return false;
            do
            {
                SupportedParameters par;
                var param = match.Groups[1].Value;
                var paramValue = match.Groups[2].Value;
                if (!Enum.TryParse(paramValue, out par))
                    return false;
                template = template.Replace(param, string.Empty);
                match = parameterDetection.Match(template);
            } while (match.Success);

            task.Estimation = TimeSpan.FromSeconds(5);
            
            return true;
        }

        private enum SupportedParameters
        {
            index,
            ext
        }
        private Regex parameterDetection = new Regex("(%([^%]+)%)", RegexOptions.Compiled);

        protected override void DoWork()
        {
            try
            {
                var template = CurrentTask.To.Files[0].Value;

                CurrentTask.To.Files.Clear();
                
                var index = 1;

                foreach (var file in CurrentTask.From.Files)
                {
                    var res = template;
                    var match = parameterDetection.Match(res);
                    while (match.Success)
                    {
                        var param = match.Groups[1].Value;
                        var paramValue = match.Groups[2].Value;
                        string replacementValue;
                        switch ((SupportedParameters) Enum.Parse(typeof(SupportedParameters),paramValue))
                        {
                            case SupportedParameters.index:
                                replacementValue = index++.ToString();
                                break;
                            case SupportedParameters.ext:
                                replacementValue = GetExtension(file.Value);
                                break;
                            default:
                                throw 
                                    new PluginException("Failed to read parameters",
                                    new ArgumentException("Unsupported parameter", paramValue));
                        }
                        res = res.Replace(param, replacementValue);
                        match = match.NextMatch();
                    }
                    RenameFile(CurrentTask.To.Path, file.Value, res);
                    CurrentTask.To.Files.Add(res);
                }
                CurrentTask.State = ExecutionState.Done;
            }
            catch (Exception e)
            {
                CurrentTask.State = ExecutionState.Failed;
                Release(CurrentTask);
                Logging.LogException(e, e.Message);
            }
        }

        private void RenameFile(string path, string oldName, string newName)
        {
            var from = Path.Combine(path, oldName);

            if (!File.Exists(from))
                throw new PluginException($"Source file not found : {from}");

            var to = Path.Combine(path, newName);

            if (File.Exists(to))
                throw new PluginException($"Destination file already exists : {to}");

            File.Move(from, to);
        }

        private static string GetExtension(string filename)
        {
            var regex = new Regex(@"(?<=\.)\w+$");
            return regex.Match(filename).Value;
        }
    }
}