using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using DR.Marvin.Model;

[assembly: InternalsVisibleTo("DR.Marvin.Plugins.Test")]
namespace DR.Marvin.Plugins.Common
{
    public abstract class DynamicPlugin : PluginBase
    {
        
        private static readonly IDictionary<string,IList<DynamicPlugin>> Siblings = new ConcurrentDictionary<string, IList<DynamicPlugin>>();
        internal bool HasTask => CurrentTask != null;

        protected DynamicPlugin(string urn, string pluginType, ITimeProvider timeProvider,ILogging logging) : base(urn, pluginType, timeProvider, logging)
        {
            if (!Siblings.ContainsKey(pluginType))
                Siblings[pluginType] = new List<DynamicPlugin> { this };
            else
                Siblings[pluginType].Add(this);
        }

        internal static void ClearSiblings(string pluginType)
        {
            if (Siblings.ContainsKey(pluginType))
                Siblings[pluginType].Clear();
        }

        protected abstract int GetWorkerNodeCount();

        public override bool Busy
        {
            get
            {
                if (HasTask)
                    return true;

                try
                {
                    var res = GetWorkerNodeCount();
                    if (Siblings[PluginType].Count <= res)
                        return false; //free

                    if (Siblings[PluginType].Count(p => p.HasTask) >= res)
                        return true;

                    var toBeMarkAsBusy =
                        Siblings[PluginType].OrderByDescending(p => p.HasTask).ThenBy(p => p.Urn).Select(p => p.Urn).Skip(res).ToArray();
                    return toBeMarkAsBusy.Contains(Urn);
                }
                catch (Exception e)
                {
                    Logging.LogException(e, "Caught exception trying to get GetWorkerNodeCount");

                    return true;
                }
            }
        }
    }
}
