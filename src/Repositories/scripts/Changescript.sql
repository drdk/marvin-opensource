USE [Marvin]
GO

/****** Object:  Table [dbo].[healthCounter]    Script Date: 30-03-2017 15:03:50 ******/
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

