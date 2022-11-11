USE [master]
GO
/****** Object:  Database [Umix]    Script Date: 12.03.2020 11:03:10 ******/
CREATE DATABASE [Umix]
 CONTAINMENT = NONE
 ON  PRIMARY 
( NAME = N'Umix', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL14.SQLEXPRESS\MSSQL\DATA\Umix.mdf' , SIZE = 5120KB , MAXSIZE = UNLIMITED, FILEGROWTH = 1024KB )
 LOG ON 
( NAME = N'Umix_log', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL14.SQLEXPRESS\MSSQL\DATA\Umix_log.ldf' , SIZE = 1536KB , MAXSIZE = 2048GB , FILEGROWTH = 10%)
GO
ALTER DATABASE [Umix] SET COMPATIBILITY_LEVEL = 120
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [Umix].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [Umix] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [Umix] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [Umix] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [Umix] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [Umix] SET ARITHABORT OFF 
GO
ALTER DATABASE [Umix] SET AUTO_CLOSE ON 
GO
ALTER DATABASE [Umix] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [Umix] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [Umix] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [Umix] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [Umix] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [Umix] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [Umix] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [Umix] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [Umix] SET  DISABLE_BROKER 
GO
ALTER DATABASE [Umix] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [Umix] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [Umix] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [Umix] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [Umix] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [Umix] SET READ_COMMITTED_SNAPSHOT OFF 
GO
ALTER DATABASE [Umix] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [Umix] SET RECOVERY SIMPLE 
GO
ALTER DATABASE [Umix] SET  MULTI_USER 
GO
ALTER DATABASE [Umix] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [Umix] SET DB_CHAINING OFF 
GO
ALTER DATABASE [Umix] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [Umix] SET TARGET_RECOVERY_TIME = 0 SECONDS 
GO
ALTER DATABASE [Umix] SET DELAYED_DURABILITY = DISABLED 
GO
ALTER DATABASE [Umix] SET QUERY_STORE = OFF
GO
USE [Umix]
GO
/****** Object:  User [SIMATIC HMI VIEWER User]    Script Date: 12.03.2020 11:03:10 ******/
CREATE USER [SIMATIC HMI VIEWER User]
GO
/****** Object:  User [SIMATIC HMI User]    Script Date: 12.03.2020 11:03:10 ******/
CREATE USER [SIMATIC HMI User]
GO
/****** Object:  DatabaseRole [SIMATIC HMI VIEWER role]    Script Date: 12.03.2020 11:03:10 ******/
CREATE ROLE [SIMATIC HMI VIEWER role]
GO
/****** Object:  DatabaseRole [SIMATIC HMI role]    Script Date: 12.03.2020 11:03:10 ******/
CREATE ROLE [SIMATIC HMI role]
GO
ALTER ROLE [SIMATIC HMI VIEWER role] ADD MEMBER [SIMATIC HMI VIEWER User]
GO
ALTER ROLE [db_datareader] ADD MEMBER [SIMATIC HMI VIEWER User]
GO
ALTER ROLE [SIMATIC HMI role] ADD MEMBER [SIMATIC HMI User]
GO
ALTER ROLE [db_ddladmin] ADD MEMBER [SIMATIC HMI User]
GO
ALTER ROLE [db_datareader] ADD MEMBER [SIMATIC HMI User]
GO
ALTER ROLE [db_datawriter] ADD MEMBER [SIMATIC HMI User]
GO
ALTER ROLE [db_datareader] ADD MEMBER [SIMATIC HMI VIEWER role]
GO
ALTER ROLE [db_ddladmin] ADD MEMBER [SIMATIC HMI role]
GO
ALTER ROLE [db_datareader] ADD MEMBER [SIMATIC HMI role]
GO
ALTER ROLE [db_datawriter] ADD MEMBER [SIMATIC HMI role]
GO
/****** Object:  Table [dbo].[Bunker]    Script Date: 12.03.2020 11:03:10 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Bunker](
	[ID] [bigint] IDENTITY(1,1) NOT NULL,
	[Name_Bunker] [nvarchar](50) NULL,
	[Num_Bunker] [int] NULL,
 CONSTRAINT [PK_Bunker] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Cycle_Data]    Script Date: 12.03.2020 11:03:10 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Cycle_Data](
	[ID] [bigint] IDENTITY(1,1) NOT NULL,
	[Cycle_ID] [bigint] NULL,
	[Bunker_ID] [bigint] NULL,
	[Need_Weight] [real] NULL,
	[Act_Weight] [real] NULL,
	[Manual_Weight] [real] NULL,
	[Auto_Weight] [real] NULL,
	[Material_ID] [bigint] NULL,
 CONSTRAINT [PK_Cycle_Data] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Cycle_Info]    Script Date: 12.03.2020 11:03:10 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Cycle_Info](
	[ID] [bigint] IDENTITY(1,1) NOT NULL,
	[Recipe_ID] [bigint] NULL,
	[Total_Weight] [real] NULL,
	[Cycle_Time] [nvarchar](50) NULL,
	[Cycle_Date] [nvarchar](50) NULL,
	[Cycle_Num] [bigint] NULL,
 CONSTRAINT [PK_Cycle_Info] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Material]    Script Date: 12.03.2020 11:03:10 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Material](
	[ID] [bigint] IDENTITY(1,1) NOT NULL,
	[Name_Material] [nvarchar](50) NULL,
 CONSTRAINT [PK_Material] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Recipe_Fact_Data]    Script Date: 12.03.2020 11:03:10 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Recipe_Fact_Data](
	[ID] [bigint] IDENTITY(1,1) NOT NULL,
	[Recipe_ID] [bigint] NULL,
	[Bunker_Name] [nvarchar](50) NULL,
	[Bunker_Seq] [int] NULL,
	[Bunker_Percent] [int] NULL,
	[Material_Name] [nvarchar](50) NULL,
	[Inaccuracy] [real] NULL,
	[Bunker_ID] [bigint] NULL,
 CONSTRAINT [PK_Recipe_Data] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Recipe_Fact_Info]    Script Date: 12.03.2020 11:03:10 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Recipe_Fact_Info](
	[ID] [bigint] IDENTITY(1,1) NOT NULL,
	[Recipe_Templ_Name] [nvarchar](50) NULL,
	[Total_Weight_Fact] [real] NULL,
	[Total_Cycle_Fact] [real] NULL,
	[Mixing_Time_Fact] [int] NULL,
	[Auto_Pause_Fact] [bit] NULL,
	[Recipe_Time_Created] [nvarchar](50) NULL,
	[Recipe_Date_Created] [nvarchar](50) NULL,
	[Recipe_Time_Finished] [nvarchar](50) NULL,
	[Recipe_Date_Finished] [nvarchar](50) NULL,
	[Status] [nvarchar](50) NULL,
	[Itinerary] [nvarchar](20) NULL,
 CONSTRAINT [PK_Recipe] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Recipe_Template_Data]    Script Date: 12.03.2020 11:03:10 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Recipe_Template_Data](
	[ID] [bigint] IDENTITY(1,1) NOT NULL,
	[Templ_ID] [bigint] NULL,
	[Bunker_ID] [bigint] NULL,
	[Bunker_Sequence] [int] NULL,
	[Bunker_Percent] [int] NULL,
	[Material_ID] [bigint] NULL,
	[Inaccuracy] [int] NULL,
 CONSTRAINT [PK_Recipe_Template_Data] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Recipe_Template_Info]    Script Date: 12.03.2020 11:03:10 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Recipe_Template_Info](
	[ID] [bigint] IDENTITY(1,1) NOT NULL,
	[Recipe_Templ_Name] [nvarchar](50) NULL,
	[Total_Weight] [real] NULL,
	[Mixing_Time] [int] NULL,
	[Auto_Pause] [bit] NULL,
	[Itinerary] [nvarchar](20) NULL,
	[Max_Doses] [real] NULL,
 CONSTRAINT [PK_Recipe_Template_Info_1] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TagSaveData]    Script Date: 12.03.2020 11:03:10 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TagSaveData](
	[SaveDate] [nvarchar](50) NULL,
	[SaveTime] [nvarchar](50) NULL,
	[RecalcCurrentAkt] [nvarchar](50) NULL,
	[UseCurrentUnload] [nvarchar](50) NULL,
	[TimeAfterLoadDB] [int] NULL,
	[TimeAfterLoadDM] [int] NULL,
	[TimeAfterUnloadDB] [int] NULL,
	[TimeAfterUnloadDM] [int] NULL,
	[TimeNoLoad] [nvarchar](50) NULL,
	[TimeUnloadDB] [int] NULL,
	[TimeUnloadDM] [int] NULL,
	[TimeUnloadMixer] [nvarchar](50) NULL
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Cycle_Data]  WITH CHECK ADD  CONSTRAINT [FK_Cycle_Data_Bunker] FOREIGN KEY([Bunker_ID])
REFERENCES [dbo].[Bunker] ([ID])
GO
ALTER TABLE [dbo].[Cycle_Data] CHECK CONSTRAINT [FK_Cycle_Data_Bunker]
GO
ALTER TABLE [dbo].[Cycle_Data]  WITH CHECK ADD  CONSTRAINT [FK_Cycle_Data_Cycle_Info] FOREIGN KEY([Cycle_ID])
REFERENCES [dbo].[Cycle_Info] ([ID])
GO
ALTER TABLE [dbo].[Cycle_Data] CHECK CONSTRAINT [FK_Cycle_Data_Cycle_Info]
GO
ALTER TABLE [dbo].[Cycle_Data]  WITH CHECK ADD  CONSTRAINT [FK_Cycle_Data_Material] FOREIGN KEY([Material_ID])
REFERENCES [dbo].[Material] ([ID])
GO
ALTER TABLE [dbo].[Cycle_Data] CHECK CONSTRAINT [FK_Cycle_Data_Material]
GO
ALTER TABLE [dbo].[Cycle_Info]  WITH CHECK ADD  CONSTRAINT [FK_Cycle_Info_Recipe_Info2] FOREIGN KEY([Recipe_ID])
REFERENCES [dbo].[Recipe_Fact_Info] ([ID])
GO
ALTER TABLE [dbo].[Cycle_Info] CHECK CONSTRAINT [FK_Cycle_Info_Recipe_Info2]
GO
ALTER TABLE [dbo].[Recipe_Fact_Data]  WITH CHECK ADD  CONSTRAINT [FK_Recipe_Data_Bunker] FOREIGN KEY([Bunker_ID])
REFERENCES [dbo].[Bunker] ([ID])
GO
ALTER TABLE [dbo].[Recipe_Fact_Data] CHECK CONSTRAINT [FK_Recipe_Data_Bunker]
GO
ALTER TABLE [dbo].[Recipe_Fact_Data]  WITH CHECK ADD  CONSTRAINT [FK_Recipe_Fact_Data_Recipe_Fact_Info] FOREIGN KEY([Recipe_ID])
REFERENCES [dbo].[Recipe_Fact_Info] ([ID])
GO
ALTER TABLE [dbo].[Recipe_Fact_Data] CHECK CONSTRAINT [FK_Recipe_Fact_Data_Recipe_Fact_Info]
GO
ALTER TABLE [dbo].[Recipe_Template_Data]  WITH CHECK ADD  CONSTRAINT [FK_Recipe_Template_Data_Bunker] FOREIGN KEY([Bunker_ID])
REFERENCES [dbo].[Bunker] ([ID])
GO
ALTER TABLE [dbo].[Recipe_Template_Data] CHECK CONSTRAINT [FK_Recipe_Template_Data_Bunker]
GO
ALTER TABLE [dbo].[Recipe_Template_Data]  WITH CHECK ADD  CONSTRAINT [FK_Recipe_Template_Data_Material] FOREIGN KEY([Material_ID])
REFERENCES [dbo].[Material] ([ID])
GO
ALTER TABLE [dbo].[Recipe_Template_Data] CHECK CONSTRAINT [FK_Recipe_Template_Data_Material]
GO
ALTER TABLE [dbo].[Recipe_Template_Data]  WITH CHECK ADD  CONSTRAINT [FK_Recipe_Template_Data_Recipe_Template_Info] FOREIGN KEY([Templ_ID])
REFERENCES [dbo].[Recipe_Template_Info] ([ID])
GO
ALTER TABLE [dbo].[Recipe_Template_Data] CHECK CONSTRAINT [FK_Recipe_Template_Data_Recipe_Template_Info]
GO
/****** Object:  DdlTrigger [OnTriggerDboSchema]    Script Date: 12.03.2020 11:03:10 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE trigger [OnTriggerDboSchema] ON database FOR create_table, create_view AS BEGIN   DECLARE @xmlEventData xml   SELECT    @xmlEventData = eventdata()   DECLARE @schemaName nvarchar(max)   DECLARE @objectName nvarchar(max)   DECLARE @DynSql nvarchar(max)      SET @schemaName    = convert(nvarchar(max), @xmlEventData.query('/EVENT_INSTANCE/SchemaName/text()'))   SET @objectName    = convert(nvarchar(max), @xmlEventData.query('/EVENT_INSTANCE/ObjectName/text()'))   IF(@schemaName='')   BEGIN     SET @DynSql = N'alter schema [dbo] transfer [' + @schemaName + N'].[' + @objectName + N']'     EXEC sp_executesql @statement=@DynSql   END END SET QUOTED_IDENTIFIER ON SET ANSI_NULLS ON 
GO
ENABLE TRIGGER [OnTriggerDboSchema] ON DATABASE
GO
USE [master]
GO
ALTER DATABASE [Umix] SET  READ_WRITE 
GO
