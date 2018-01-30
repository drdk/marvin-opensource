USE [MarvinLocal]

ALTER DATABASE [MarvinLocal] SET ALLOW_SNAPSHOT_ISOLATION ON 
GO
/****** Object:  FullTextCatalog [logMessage]    Script Date: 30-01-2018 11:07:30 ******/
CREATE FULLTEXT CATALOG [logMessage]WITH ACCENT_SENSITIVITY = ON

GO
/****** Object:  Table [dbo].[attachment]    Script Date: 30-01-2018 11:07:30 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[attachment](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[essence_Id] [int] NOT NULL,
	[path] [nvarchar](260) NOT NULL,
	[attachmentType] [int] NOT NULL,
	[arguments] [ntext] NULL,
 CONSTRAINT [PK_attachment] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[attachmentType]    Script Date: 30-01-2018 11:07:30 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[attachmentType](
	[id] [int] NOT NULL,
	[name] [nvarchar](32) NOT NULL,
 CONSTRAINT [PK_attachmentType] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[command]    Script Date: 30-01-2018 11:07:30 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[command](
	[type] [int] NOT NULL,
	[urn] [nvarchar](200) NOT NULL,
	[username] [nvarchar](32) NOT NULL,
 CONSTRAINT [PK_command] PRIMARY KEY CLUSTERED 
(
	[type] ASC,
	[urn] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[commandType]    Script Date: 30-01-2018 11:07:30 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[commandType](
	[id] [int] NOT NULL,
	[name] [nvarchar](32) NOT NULL,
 CONSTRAINT [PK_commandType] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[essence]    Script Date: 30-01-2018 11:07:30 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[essence](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[path] [nvarchar](260) NOT NULL,
	[stateFormat] [int] NOT NULL,
	[stateFlag] [int] NOT NULL,
	[duration] [int] NOT NULL,
	[aspectratio] [int] NOT NULL,
	[customFormat] [nvarchar](50) NULL,
	[resolution] [int] NOT NULL CONSTRAINT [DF_essence_resolution]  DEFAULT ((0)),
 CONSTRAINT [PK_essence_1] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[essenceFile]    Script Date: 30-01-2018 11:07:30 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[essenceFile](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[essence_Id] [int] NOT NULL,
	[sortOrder] [int] NOT NULL,
	[value] [nvarchar](255) NOT NULL,
	[kind] [int] NOT NULL,
 CONSTRAINT [PK_essenceFile] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[essenceFileKind]    Script Date: 30-01-2018 11:07:30 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[essenceFileKind](
	[id] [int] NOT NULL,
	[name] [nvarchar](32) NOT NULL,
 CONSTRAINT [PK_essenceFileKind] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[execution_essence]    Script Date: 30-01-2018 11:07:30 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[execution_essence](
	[essence_Id] [int] NOT NULL,
	[executiontask_Id] [uniqueidentifier] NOT NULL,
	[executionEssenceType] [int] NOT NULL,
 CONSTRAINT [PK_execution_essence] PRIMARY KEY CLUSTERED 
(
	[essence_Id] ASC,
	[executiontask_Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[executionEssenceType]    Script Date: 30-01-2018 11:07:30 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[executionEssenceType](
	[id] [int] NOT NULL,
	[name] [nvarchar](32) NOT NULL,
 CONSTRAINT [PK_executionEssenceType] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[executionPlan]    Script Date: 30-01-2018 11:07:30 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[executionPlan](
	[urn] [nvarchar](200) NOT NULL,
	[executionState] [int] NOT NULL,
	[currentEssence_Id] [int] NULL,
	[jobId] [uniqueidentifier] NOT NULL,
	[activeTaskIndex] [int] NULL,
 CONSTRAINT [PK_ExecutionPlan_jobid] PRIMARY KEY CLUSTERED 
(
	[jobId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[executionState]    Script Date: 30-01-2018 11:07:30 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[executionState](
	[id] [int] NOT NULL,
	[name] [nvarchar](32) NOT NULL,
 CONSTRAINT [PK_executionState] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[executiontask]    Script Date: 30-01-2018 11:07:30 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[executiontask](
	[Id] [uniqueidentifier] NOT NULL,
	[urn] [nvarchar](200) NOT NULL,
	[startTime] [datetime] NULL,
	[endTime] [datetime] NULL,
	[estimation] [bigint] NOT NULL,
	[executionState] [int] NOT NULL,
	[pluginUrn] [nvarchar](200) NOT NULL,
	[executionPlan_Id] [uniqueidentifier] NOT NULL,
	[sortOrder] [int] NOT NULL,
	[foreignKey] [nvarchar](100) NULL,
	[arguments] [ntext] NULL CONSTRAINT [DF_executiontask_arguments]  DEFAULT (NULL),
	[numberOfRetries] [int] NOT NULL DEFAULT ((0)),
 CONSTRAINT [PK_executiontask] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[healthCounter]    Script Date: 30-01-2018 11:07:30 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[healthCounter](
	[id] [varchar](50) NOT NULL,
	[timestamp] [datetime] NOT NULL,
	[count] [int] NOT NULL,
	[message] [nvarchar](260) NULL,
 CONSTRAINT [PK_healthCounter] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[job]    Script Date: 30-01-2018 11:07:30 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[job](
	[id] [uniqueidentifier] NOT NULL,
	[urn] [nvarchar](200) NOT NULL,
	[issued] [datetime] NOT NULL,
	[dueDate] [datetime] NOT NULL,
	[priority] [int] NOT NULL,
	[endTime] [datetime] NULL,
	[created] [datetime] NOT NULL CONSTRAINT [DF_job_Created]  DEFAULT (getutcdate()),
	[modified] [datetime] NOT NULL CONSTRAINT [DF_job_Modified]  DEFAULT (getutcdate()),
	[callbackUrl] [nvarchar](100) NULL,
	[name] [nvarchar](200) NULL CONSTRAINT [DF_job_name]  DEFAULT (NULL),
	[sourceUrn] [nvarchar](200) NULL CONSTRAINT [DF_job_sourceUrn]  DEFAULT (NULL),
 CONSTRAINT [PK_Jobs] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[job_essence]    Script Date: 30-01-2018 11:07:30 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[job_essence](
	[job_Id] [uniqueidentifier] NOT NULL,
	[jobEssenceType] [int] NOT NULL,
	[essence_Id] [int] NOT NULL,
 CONSTRAINT [PK_job_essence] PRIMARY KEY CLUSTERED 
(
	[job_Id] ASC,
	[essence_Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[jobEssenceType]    Script Date: 30-01-2018 11:07:30 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[jobEssenceType](
	[id] [int] NOT NULL,
	[name] [nvarchar](32) NULL,
 CONSTRAINT [PK_jobEssenceType] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[log]    Script Date: 30-01-2018 11:07:30 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[log](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Date] [datetime] NOT NULL,
	[Hostname] [varchar](255) NOT NULL,
	[Thread] [varchar](255) NOT NULL,
	[Level] [varchar](50) NOT NULL,
	[Logger] [varchar](255) NOT NULL,
	[Message] [varchar](4000) NOT NULL,
	[Exception] [varchar](4000) NULL,
 CONSTRAINT [PK_log] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[semaphore]    Script Date: 30-01-2018 11:07:30 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[semaphore](
	[semaphoreId] [varchar](50) NOT NULL,
	[currentOwnerId] [varchar](50) NOT NULL,
	[heartBeat] [datetime] NOT NULL,
	[rowversion] [timestamp] NOT NULL,
 CONSTRAINT [PK_semaphore] PRIMARY KEY CLUSTERED 
(
	[semaphoreId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[stateFormat]    Script Date: 30-01-2018 11:07:30 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[stateFormat](
	[id] [int] NOT NULL,
	[name] [nvarchar](32) NULL,
 CONSTRAINT [PK_stateFormat] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Index [IX_attachment]    Script Date: 30-01-2018 11:07:30 ******/
CREATE NONCLUSTERED INDEX [IX_attachment] ON [dbo].[attachment]
(
	[essence_Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_essence_stateFormat]    Script Date: 30-01-2018 11:07:30 ******/
CREATE NONCLUSTERED INDEX [IX_essence_stateFormat] ON [dbo].[essence]
(
	[stateFormat] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_essenceFile]    Script Date: 30-01-2018 11:07:30 ******/
CREATE NONCLUSTERED INDEX [IX_essenceFile] ON [dbo].[essenceFile]
(
	[essence_Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_exeTask]    Script Date: 30-01-2018 11:07:30 ******/
CREATE NONCLUSTERED INDEX [IX_exeTask] ON [dbo].[execution_essence]
(
	[executiontask_Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_executionPlan_currentEssence_Id]    Script Date: 30-01-2018 11:07:30 ******/
CREATE NONCLUSTERED INDEX [IX_executionPlan_currentEssence_Id] ON [dbo].[executionPlan]
(
	[currentEssence_Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON

GO
/****** Object:  Index [IX_executionPlan_urn]    Script Date: 30-01-2018 11:07:30 ******/
CREATE NONCLUSTERED INDEX [IX_executionPlan_urn] ON [dbo].[executionPlan]
(
	[urn] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON

GO
/****** Object:  Index [IX_planState]    Script Date: 30-01-2018 11:07:30 ******/
CREATE NONCLUSTERED INDEX [IX_planState] ON [dbo].[executionPlan]
(
	[executionState] ASC
)
INCLUDE ( 	[urn],
	[currentEssence_Id],
	[jobId],
	[activeTaskIndex]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON

GO
/****** Object:  Index [_dta_index_executiontask_14_1365579903__K8_K1_2_3_4_5_6_7_9_10_12]    Script Date: 30-01-2018 11:07:30 ******/
CREATE NONCLUSTERED INDEX [IX_executiontask_Plan_include] ON [dbo].[executiontask]
(
	[executionPlan_Id] ASC,
	[Id] ASC
)
INCLUDE ( 	[urn],
	[startTime],
	[endTime],
	[estimation],
	[executionState],
	[pluginUrn],
	[sortOrder],
	[foreignKey],
	[numberOfRetries]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_executiontask_executionPlan_Id]    Script Date: 30-01-2018 11:07:30 ******/
CREATE NONCLUSTERED INDEX [IX_executiontask_executionPlan_Id] ON [dbo].[executiontask]
(
	[executionPlan_Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_issued]    Script Date: 30-01-2018 11:07:30 ******/
CREATE NONCLUSTERED INDEX [IX_issued] ON [dbo].[job]
(
	[issued] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_modified]    Script Date: 30-01-2018 11:07:30 ******/
CREATE NONCLUSTERED INDEX [IX_modified] ON [dbo].[job]
(
	[modified] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_log_date]    Script Date: 30-01-2018 11:07:30 ******/
CREATE NONCLUSTERED INDEX [IX_log_date] ON [dbo].[log]
(
	[Date] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON

GO
/****** Object:  Index [IX_log_date_level]    Script Date: 30-01-2018 11:07:30 ******/
CREATE NONCLUSTERED INDEX [IX_log_date_level] ON [dbo].[log]
(
	[Date] DESC,
	[Level] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  FullTextIndex     Script Date: 30-01-2018 11:07:30 ******/
CREATE FULLTEXT INDEX ON [dbo].[log](
[Message] LANGUAGE 'English')
KEY INDEX [PK_log]ON ([logMessage], FILEGROUP [PRIMARY])
WITH (CHANGE_TRACKING = AUTO, STOPLIST = SYSTEM)


GO
ALTER TABLE [dbo].[attachment]  WITH CHECK ADD  CONSTRAINT [FK_attachment_attachmentType] FOREIGN KEY([attachmentType])
REFERENCES [dbo].[attachmentType] ([id])
GO
ALTER TABLE [dbo].[attachment] CHECK CONSTRAINT [FK_attachment_attachmentType]
GO
ALTER TABLE [dbo].[attachment]  WITH CHECK ADD  CONSTRAINT [FK_attachment_essence] FOREIGN KEY([essence_Id])
REFERENCES [dbo].[essence] ([id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[attachment] CHECK CONSTRAINT [FK_attachment_essence]
GO
ALTER TABLE [dbo].[command]  WITH CHECK ADD  CONSTRAINT [FK_command_commandType] FOREIGN KEY([type])
REFERENCES [dbo].[commandType] ([id])
GO
ALTER TABLE [dbo].[command] CHECK CONSTRAINT [FK_command_commandType]
GO
ALTER TABLE [dbo].[essence]  WITH CHECK ADD  CONSTRAINT [FK_essence_stateFormat] FOREIGN KEY([stateFormat])
REFERENCES [dbo].[stateFormat] ([id])
GO
ALTER TABLE [dbo].[essence] CHECK CONSTRAINT [FK_essence_stateFormat]
GO
ALTER TABLE [dbo].[essenceFile]  WITH CHECK ADD  CONSTRAINT [FK_essenceFile_essenceFileKind] FOREIGN KEY([kind])
REFERENCES [dbo].[essenceFileKind] ([id])
GO
ALTER TABLE [dbo].[essenceFile] CHECK CONSTRAINT [FK_essenceFile_essenceFileKind]
GO
ALTER TABLE [dbo].[essenceFile]  WITH CHECK ADD  CONSTRAINT [FK_fileName_essence1] FOREIGN KEY([essence_Id])
REFERENCES [dbo].[essence] ([id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[essenceFile] CHECK CONSTRAINT [FK_fileName_essence1]
GO
ALTER TABLE [dbo].[execution_essence]  WITH CHECK ADD  CONSTRAINT [FK_execution_essence_essence1] FOREIGN KEY([essence_Id])
REFERENCES [dbo].[essence] ([id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[execution_essence] CHECK CONSTRAINT [FK_execution_essence_essence1]
GO
ALTER TABLE [dbo].[execution_essence]  WITH CHECK ADD  CONSTRAINT [FK_execution_essence_executionEssenceType] FOREIGN KEY([executionEssenceType])
REFERENCES [dbo].[executionEssenceType] ([id])
GO
ALTER TABLE [dbo].[execution_essence] CHECK CONSTRAINT [FK_execution_essence_executionEssenceType]
GO
ALTER TABLE [dbo].[execution_essence]  WITH CHECK ADD  CONSTRAINT [FK_execution_essence_executiontask1] FOREIGN KEY([executiontask_Id])
REFERENCES [dbo].[executiontask] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[execution_essence] CHECK CONSTRAINT [FK_execution_essence_executiontask1]
GO
ALTER TABLE [dbo].[executionPlan]  WITH CHECK ADD  CONSTRAINT [FK_executionPlan_job] FOREIGN KEY([jobId])
REFERENCES [dbo].[job] ([id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[executionPlan] CHECK CONSTRAINT [FK_executionPlan_job]
GO
ALTER TABLE [dbo].[executiontask]  WITH CHECK ADD  CONSTRAINT [FK_executiontask_executionPlan] FOREIGN KEY([executionPlan_Id])
REFERENCES [dbo].[executionPlan] ([jobId])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[executiontask] CHECK CONSTRAINT [FK_executiontask_executionPlan]
GO
ALTER TABLE [dbo].[executiontask]  WITH CHECK ADD  CONSTRAINT [FK_executiontask_executionState] FOREIGN KEY([executionState])
REFERENCES [dbo].[executionState] ([id])
GO
ALTER TABLE [dbo].[executiontask] CHECK CONSTRAINT [FK_executiontask_executionState]
GO
ALTER TABLE [dbo].[job_essence]  WITH CHECK ADD  CONSTRAINT [FK_job_essence_essence1] FOREIGN KEY([essence_Id])
REFERENCES [dbo].[essence] ([id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[job_essence] CHECK CONSTRAINT [FK_job_essence_essence1]
GO
ALTER TABLE [dbo].[job_essence]  WITH CHECK ADD  CONSTRAINT [FK_job_essence_job] FOREIGN KEY([job_Id])
REFERENCES [dbo].[job] ([id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[job_essence] CHECK CONSTRAINT [FK_job_essence_job]
GO
ALTER TABLE [dbo].[job_essence]  WITH CHECK ADD  CONSTRAINT [FK_job_essence_jobEssenceType] FOREIGN KEY([jobEssenceType])
REFERENCES [dbo].[jobEssenceType] ([id])
GO
ALTER TABLE [dbo].[job_essence] CHECK CONSTRAINT [FK_job_essence_jobEssenceType]
GO
/****** Object:  Login [nunit]    Script Date: 20-09-2016 14:05:29 ******/
USE [master]
GO
/****** Object:  Login [nunit]    Script Date: 14-07-2016 14:25:18 ******/
CREATE LOGIN [nunit] WITH PASSWORD='test', DEFAULT_DATABASE=[MarvinLocal], DEFAULT_LANGUAGE=[us_english], CHECK_EXPIRATION=OFF, CHECK_POLICY=OFF
GO

USE [MarvinLocal]
GO

/****** Object:  User [nunit]    Script Date: 14-07-2016 14:25:05 ******/
CREATE USER [nunit] FOR LOGIN [nunit] WITH DEFAULT_SCHEMA=[dbo]
GO

ALTER ROLE [db_owner] ADD MEMBER [nunit]
GO
