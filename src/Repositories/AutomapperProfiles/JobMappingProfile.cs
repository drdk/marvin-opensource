using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using DR.Marvin.Model;
using Newtonsoft.Json;

// ReSharper disable ClassNeverInstantiated.Global
namespace DR.Marvin.Repositories.AutomapperProfiles
{
    /// <summary>
    /// Converts date times with Unspecified kind to UTC kind
    /// </summary>
    public class UtcTimeConverter : ITypeConverter<DateTime, DateTime>
    {
        public DateTime Convert(DateTime source, DateTime destination, ResolutionContext context)
        {
            var res =
                source.Kind == DateTimeKind.Unspecified ?
                DateTime.SpecifyKind(source, DateTimeKind.Utc) :
                (source.Kind != DateTimeKind.Utc ?
                source.ToUniversalTime() :
                source);
            return res;
        }
    }
    /// <summary>
    /// Converts nullable date times with Unspecified kind to UTC kind
    /// </summary>
    public class NullableUtcTimeConverter : ITypeConverter<DateTime?, DateTime?>
    {
        public DateTime? Convert(DateTime? source, DateTime? destination, ResolutionContext context)
        {

            var res =
                source?.Kind == DateTimeKind.Unspecified ?
                DateTime.SpecifyKind(source.Value, DateTimeKind.Utc) :
                (source != null && source.Value.Kind != DateTimeKind.Utc ?
                source.Value.ToUniversalTime() :
                source);
            return res;
        }
    }
    /// <summary>
    /// Automapper profile configuration for sql repo.
    /// </summary>
    public class JobMappingProfile : Profile
    {
        internal static readonly object Lock = new object();
        internal static MarvinEntities Db { private get; set; }

        private class EssenseFileWrapper
        {
            public EssenceFile File { get; set; }
            public int Id { get; set; }
            public int SortOrder { get; set; }
        }

        private class ExecutionTaskWrapper
        {
            public ExecutionTask Task { get; set; }
            public int SortOrder { get; set; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public JobMappingProfile()
        {
            CreateMap<DateTime, DateTime>().ConvertUsing<UtcTimeConverter>();
            CreateMap<DateTime?, DateTime?>().ConvertUsing<NullableUtcTimeConverter>();

            #region Model to Repository mappings

            CreateMap<Job, job>()
                //Members
                .ForMember(dest => dest.job_essence, opt => opt.ResolveUsing((src, dest) => JobEssenceToEssencelist(src, dest.job_essence)))
                .ForMember(dest => dest.modified, opt => opt.MapFrom(src => src.LastModified))
                .ForMember(dest => dest.id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.callbackUrl, opt => opt.MapFrom(src => src.CallbackUrl))
                .ForMember(dest => dest.dueDate, opt => opt.MapFrom(src => src.DueDate))
                .ForMember(dest => dest.endTime, opt => opt.MapFrom(src => src.EndTime))
                .ForMember(dest => dest.issued, opt => opt.MapFrom(src => src.Issued))
                .ForMember(dest => dest.priority, opt => opt.MapFrom(src => src.Priority))
                .ForMember(dest => dest.urn, opt => opt.MapFrom(src => src.Urn))
                .ForMember(dest => dest.executionPlan, opt => opt.UseDestinationValue())
                .ForMember(dest => dest.executionPlan, opt => opt.MapFrom(src => src.Plan))
                .ForMember(dest => dest.name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.sourceUrn, opt => opt.MapFrom(src => src.SourceUrn))
                //Ignored
                .ForMember(dest => dest.created, opt => opt.Ignore());

            CreateMap<Essence, essence>()
                .BeforeMap((src, dest, context) =>
                {
                    foreach (var f in dest.essenceFile.Where(f => f.sortOrder >= src.Files.Count).ToList())
                    {
                        Db.essenceFile.Remove(f);
                    }

                    foreach (var a in dest.attachment.Where(a => src.Attachments.All(sa => (int)sa.Type != a.attachmentType)).ToList())
                    {
                        Db.attachment.Remove(a);
                    }
                })
                //Members
                .ForMember(dest => dest.essenceFile, opt => opt.ResolveUsing((src, dest) => FilesToList(src.Files, dest.id, dest.essenceFile)))
                .ForMember(dest => dest.attachment, opt => opt.ResolveUsing((src, dest) => AttachmentToList(src.Attachments, dest.attachment)))
                .ForMember(dest => dest.stateFlag, opt => opt.MapFrom(src => src.Flags))
                .ForMember(dest => dest.stateFormat, opt => opt.MapFrom(src => src.Format))
                .ForMember(dest => dest.duration, opt => opt.MapFrom(src => src.Duration))
                .ForMember(dest => dest.aspectratio, opt => opt.MapFrom(src => src.AspectRatio))
                .ForMember(dest => dest.resolution, opt => opt.MapFrom(src => src.Resolution))
                .ForMember(dest => dest.customFormat, opt => opt.MapFrom(src => src.CustomFormat))
                //Ignored
                .ForMember(dest => dest.id, opt => opt.Ignore())
                ;

            CreateMap<EssenseFileWrapper, essenceFile>()
                .ForMember(dest => dest.sortOrder, opt => opt.MapFrom(src => src.SortOrder))
                .ForMember(dest => dest.kind, opt => opt.MapFrom(src => src.File.Kind))
                .ForMember(dest => dest.value, opt => opt.MapFrom(src => src.File.Value))
                .ForMember(dest => dest.essence_Id, opt => opt.MapFrom(src => src.Id))
                //Ignored
                .ForMember(dest => dest.id, opt => opt.Ignore())
                ;

            CreateMap<Attachment, attachment>()
                 //Members
                 .ForMember(dest => dest.attachmentType, opt => opt.MapFrom(src => src.Type))
                 .ForMember(dest => dest.path, opt => opt.MapFrom(src => src.Path))
                 .ForMember(dest => dest.arguments, opt => opt.ResolveUsing(
                    source => source.Arguments?.Count > 0 ?
                    JsonConvert.SerializeObject(source.Arguments) :
                    null))

                //Ignored
                .ForMember(dest => dest.id, opt => opt.Ignore())
                .ForMember(dest => dest.essence_Id, opt => opt.Ignore())
                ;
            CreateMap<ExecutionPlan, executionPlan>()
                .BeforeMap((src, dest) =>
                {
                    foreach (var t in dest.executiontask.Where(t => src.Tasks.All(st => st.Urn != t.urn)).ToList())
                    {
                        Db.executiontask.Remove(t);
                    }
                })
                //members
                .ForMember(dest => dest.executiontask, opt => opt.ResolveUsing((src, dest) =>
                TasksToList(src.Tasks, dest.executiontask)))
                .ForMember(dest => dest.executionState, opt => opt.ResolveUsing(src => src.GetState()))
                .ForMember(dest => dest.urn, opt => opt.MapFrom(src => src.Urn))
                .ForMember(dest => dest.activeTaskIndex, opt => opt.MapFrom(src => src.ActiveTaskIndex))

                //Ignored
                .ForMember(dest => dest.jobId, opt => opt.Ignore())
                .ForMember(dest => dest.currentEssence_Id, opt => opt.Ignore())
                ;

            CreateMap<ExecutionTaskWrapper, executiontask>()
                //members
                .ForMember(dest => dest.urn, b => b.MapFrom(c => c.Task.Urn))
                .ForMember(dest => dest.Id, opt => opt.Condition((src, dest) => dest.Id == Guid.Empty))
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Task.Id))
                .ForMember(dest => dest.estimation, opt => opt.MapFrom(src => src.Task.Estimation.Ticks))
                .ForMember(dest => dest.endTime, opt => opt.MapFrom(src => src.Task.EndTime))
                .ForMember(dest => dest.executionState, opt => opt.MapFrom(src => src.Task.State))
                .ForMember(dest => dest.pluginUrn, opt => opt.MapFrom(src => src.Task.PluginUrn))
                .ForMember(dest => dest.execution_essence, opt => opt.ResolveUsing((src, dest) => ExecutionEssenceToEsscenlist(src.Task, dest.execution_essence)))
                .ForMember(dest => dest.startTime, opt => opt.MapFrom(src => src.Task.StartTime))
                .ForMember(dest => dest.foreignKey, opt => opt.MapFrom(src => src.Task.ForeignKey))
                .ForMember(dest => dest.sortOrder, opt => opt.MapFrom(c => c.SortOrder))
                .ForMember(dest => dest.numberOfRetries, opt => opt.MapFrom(c => c.Task.NumberOfRetries))
                .ForMember(dest => dest.arguments, opt => opt.ResolveUsing(
                    source => source.Task.Arguments?.Count > 0 ?
                    JsonConvert.SerializeObject(source.Task.Arguments) :
                    null))

                //Ignored
                .ForMember(dest => dest.executionPlan_Id, opt => opt.Ignore())
                ;

            #endregion

            #region Repository to Model mappings

            CreateMap<job, Job>()
                //Members
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
                .ForMember(dest => dest.CallbackUrl, opt => opt.MapFrom(src => src.callbackUrl))
                .ForMember(dest => dest.DueDate, opt => opt.MapFrom(src => src.dueDate))
                .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.endTime))
                .ForMember(dest => dest.Issued, opt => opt.MapFrom(src => src.issued))
                .ForMember(dest => dest.LastModified, opt => opt.MapFrom(src => src.modified))
                .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.priority))
                .ForMember(dest => dest.Urn, opt => opt.MapFrom(src => src.urn))
                .ForMember(dest => dest.Plan, opt => opt.MapFrom(src => src.executionPlan))
                .ForMember(dest => dest.Source, opt => opt.MapFrom(src => src.job_essence.SingleOrDefault(d => d.jobEssenceType == (int)JobEssenceType.Source).essence))
                .ForMember(dest => dest.Destination, opt => opt.MapFrom(src => src.job_essence.SingleOrDefault(d => d.jobEssenceType == (int)JobEssenceType.Destination).essence))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.name))
                .ForMember(dest => dest.SourceUrn, opt => opt.MapFrom(src => src.sourceUrn))
                ;

            CreateMap<executionPlan, ExecutionPlan>()
                //members
                .ForMember(dest => dest.Tasks, opt => opt.MapFrom(src => src.executiontask.OrderBy(d => d.sortOrder)))
                .ForMember(dest => dest.Urn, opt => opt.MapFrom(src => src.urn))
                .ForMember(dest => dest.ActiveTaskIndex, opt => opt.MapFrom(src => src.activeTaskIndex))
                //Ignored
                ;

            CreateMap<executiontask, ExecutionTask>()
                //members
                .ForMember(dest => dest.Estimation, opt => opt.MapFrom(c => new TimeSpan(c.estimation)))
                .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.endTime))
                .ForMember(dest => dest.State, opt => opt.MapFrom(src => src.executionState))
                .ForMember(dest => dest.PluginUrn, opt => opt.MapFrom(src => src.pluginUrn))
                .ForMember(dest => dest.From, opt => opt.MapFrom(src => src.execution_essence.SingleOrDefault(dest => dest.executionEssenceType == (int)ExecutionEssenceType.From).essence))
                .ForMember(dest => dest.To, opt => opt.MapFrom(src => src.execution_essence.SingleOrDefault(dest => dest.executionEssenceType == (int)ExecutionEssenceType.To).essence))
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.startTime))
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ForeignKey, opt => opt.MapFrom(src => src.foreignKey))
                .ForMember(dest => dest.NumberOfRetries, opt => opt.MapFrom(c => c.numberOfRetries))
                .ForMember(dest => dest.Arguments, opt => opt.ResolveUsing(
                    source => source.arguments == null ?
                    null :
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(source.arguments)))

                //Ignored

                ;
            CreateMap<essence, Essence>()
                //Members
                .ForMember(dest => dest.Files, opt => opt.MapFrom(c =>
                c.essenceFile != null ?
                c.essenceFile.OrderBy(d => d.sortOrder) :
                null))
                .ForMember(dest => dest.Attachments, opt => opt.MapFrom(src => src.attachment))
                .ForMember(dest => dest.Flags, opt => opt.MapFrom(src => src.stateFlag))
                .ForMember(dest => dest.Format, opt => opt.MapFrom(src => src.stateFormat))
                .ForMember(dest => dest.Duration, opt => opt.MapFrom(src => src.duration))
                .ForMember(dest => dest.AspectRatio, opt => opt.MapFrom(src => src.aspectratio))
                .ForMember(dest => dest.Resolution, opt => opt.MapFrom(src => src.resolution))
                .ForMember(dest => dest.CustomFormat, opt => opt.MapFrom(src => src.customFormat))

                //Ignored
                ;
            CreateMap<essenceFile, EssenceFile>()
                .ConstructUsing(src => new EssenceFile(src.value, (EssenceFileKind)src.kind))
                ;

            CreateMap<attachment, Attachment>()
                //Members
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.attachmentType))
                .ForMember(dest => dest.Path, opt => opt.MapFrom(src => src.path))
                .ForMember(dest => dest.Arguments, opt => opt.ResolveUsing(
                    source => source.arguments == null ?
                    null :
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(source.arguments)))
                ;


            #endregion
        }

        private static ICollection<attachment> AttachmentToList(IList<Attachment> attachments,
           ICollection<attachment> destAttachments)
        {
            if (attachments == null)
                return destAttachments;
            if (destAttachments.Any())
            {
                foreach (var attachment in destAttachments.Where(t => attachments.Any(a => (int)a.Type == t.attachmentType)))
                {
                    Mapper.Map(attachments.First(a => (int)a.Type == attachment.attachmentType), attachment);
                }
            }
            foreach (var attachment in attachments.Where(t => destAttachments.All(r => r.attachmentType != (int)t.Type)))
            {
                destAttachments.Add(Mapper.Map<attachment>(attachment));
            }
            return destAttachments;
        }

        private static ICollection<essenceFile> FilesToList(IList<EssenceFile> files, int essenceId,
            ICollection<essenceFile> destFiles)
        {
            if (files == null)
                return destFiles;
            var index = 0;
            if (destFiles.Any())
            {
                foreach (var df in destFiles.OrderBy(df => df.sortOrder))
                {
                    if (index != df.sortOrder)
                        throw new Exception("Invalid data in essence file.");
                    Mapper.Map(new EssenseFileWrapper { File = files[index], SortOrder = index, Id = essenceId }, df);
                    index++;

                }
            }
            for (; index < files.Count; index++)
            {
                destFiles.Add(Mapper.Map<essenceFile>(new EssenseFileWrapper { File = files[index], SortOrder = index, Id = essenceId }));
            }
            return destFiles;
        }

        private static ICollection<executiontask> TasksToList(IList<ExecutionTask> tasks,
            ICollection<executiontask> destTasks)
        {
            if (tasks == null)
                return destTasks;
            var wrappedTasks = tasks.Select((t, i) => new ExecutionTaskWrapper { Task = t, SortOrder = i }).ToList();
            if (destTasks.Any())
            {
                foreach (var executiontask in destTasks.Where(t => wrappedTasks.Any(r => r.Task.Urn == t.urn)))
                {
                    Mapper.Map(wrappedTasks.First(t => executiontask.urn == t.Task.Urn), executiontask);
                }
            }
            foreach (var t in wrappedTasks.Where(t => destTasks.All(dt => dt.urn != t.Task.Urn)))
            {
                destTasks.Add(Mapper.Map<executiontask>(t));
            }

            return destTasks;
        }

        /// <summary>
        /// Takes executiontasks to and form essence and converts it to executionessence with the corret type 
        /// </summary>
        private static ICollection<execution_essence> ExecutionEssenceToEsscenlist(ExecutionTask executionTask, ICollection<execution_essence> destExecutionEssences)
        {
            if (destExecutionEssences.Any())
            {
                var fromEssence =
                    destExecutionEssences.First(e => e.executionEssenceType == (int)ExecutionEssenceType.From).essence;
                Mapper.Map(executionTask.From, fromEssence);
                var toEssence =
                    destExecutionEssences.First(e => e.executionEssenceType == (int)ExecutionEssenceType.To).essence;
                Mapper.Map(executionTask.To, toEssence);
            }
            else
            {
                destExecutionEssences.Add(
                    new execution_essence
                    {
                        essence = Mapper.Map<essence>(executionTask.From),
                        executionEssenceType = (int)ExecutionEssenceType.From
                    });
                destExecutionEssences.Add(
                    new execution_essence
                    {
                        essence = Mapper.Map<essence>(executionTask.To),
                        executionEssenceType = (int)ExecutionEssenceType.To
                    });
            }
            return destExecutionEssences;
        }

        /// <summary>
        /// Takes a job and parses its source and destination essence to essencelist with flag
        /// </summary>
        private static ICollection<job_essence> JobEssenceToEssencelist(Job job, ICollection<job_essence> destJobEssence)
        {
            if (destJobEssence.Any())
            {
                var sourceEssence = destJobEssence.First(e => e.jobEssenceType == (int)JobEssenceType.Source).essence;
                Mapper.Map(job.Source, sourceEssence);
                var destinationEssence =
                    destJobEssence.First(e => e.jobEssenceType == (int)JobEssenceType.Destination).essence;
                Mapper.Map(job.Destination, destinationEssence);
            }
            else
            {
                destJobEssence.Add(
                    new job_essence
                    {
                        essence = Mapper.Map<essence>(job.Source),
                        jobEssenceType = (int)JobEssenceType.Source
                    });
                destJobEssence.Add(
                    new job_essence
                    {
                        essence = Mapper.Map<essence>(job.Destination),
                        jobEssenceType = (int)JobEssenceType.Destination
                    });
            }
            return destJobEssence;
        }
    }
}
// ReSharper restore ClassNeverInstantiated.Global