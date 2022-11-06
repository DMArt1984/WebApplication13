USE [master]
GO
/****** Object:  Database [Umix_Recipe]    Script Date: 12.03.2020 11:56:53 ******/
CREATE DATABASE [Umix_Recipe]
 CONTAINMENT = NONE
 ON  PRIMARY 
( NAME = N'Umix_Recipe', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL14.SQLEXPRESS\MSSQL\DATA\Umix_Recipe.mdf' , SIZE = 5120KB , MAXSIZE = UNLIMITED, FILEGROWTH = 1024KB )
 LOG ON 
( NAME = N'Umix_Recipe_log', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL14.SQLEXPRESS\MSSQL\DATA\Umix_Recipe_log.ldf' , SIZE = 1536KB , MAXSIZE = 2048GB , FILEGROWTH = 10%)
GO
ALTER DATABASE [Umix_Recipe] SET COMPATIBILITY_LEVEL = 120
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [Umix_Recipe].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [Umix_Recipe] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [Umix_Recipe] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [Umix_Recipe] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [Umix_Recipe] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [Umix_Recipe] SET ARITHABORT OFF 
GO
ALTER DATABASE [Umix_Recipe] SET AUTO_CLOSE ON 
GO
ALTER DATABASE [Umix_Recipe] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [Umix_Recipe] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [Umix_Recipe] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [Umix_Recipe] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [Umix_Recipe] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [Umix_Recipe] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [Umix_Recipe] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [Umix_Recipe] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [Umix_Recipe] SET  DISABLE_BROKER 
GO
ALTER DATABASE [Umix_Recipe] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [Umix_Recipe] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [Umix_Recipe] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [Umix_Recipe] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [Umix_Recipe] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [Umix_Recipe] SET READ_COMMITTED_SNAPSHOT OFF 
GO
ALTER DATABASE [Umix_Recipe] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [Umix_Recipe] SET RECOVERY SIMPLE 
GO
ALTER DATABASE [Umix_Recipe] SET  MULTI_USER 
GO
ALTER DATABASE [Umix_Recipe] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [Umix_Recipe] SET DB_CHAINING OFF 
GO
ALTER DATABASE [Umix_Recipe] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [Umix_Recipe] SET TARGET_RECOVERY_TIME = 0 SECONDS 
GO
ALTER DATABASE [Umix_Recipe] SET DELAYED_DURABILITY = DISABLED 
GO
ALTER DATABASE [Umix_Recipe] SET QUERY_STORE = OFF
GO
USE [Umix_Recipe]
GO

/****** Object:  Table [dbo].[Bunker]    Script Date: 12.03.2020 11:56:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Bunker](
	[Bunker_N] [bigint] NOT NULL,
	[Material_Bunk] [char](20) NULL,
 CONSTRAINT [PK_Bunker] PRIMARY KEY CLUSTERED 
(
	[Bunker_N] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Material]    Script Date: 12.03.2020 11:56:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Material](
	[Material_N] [bigint] NOT NULL,
	[Material] [char](20) NULL,
 CONSTRAINT [PK_Material] PRIMARY KEY CLUSTERED 
(
	[Material_N] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Recipe]    Script Date: 12.03.2020 11:56:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Recipe](
	[Recip_Number] [int] NULL,
	[Recipe_Name] [char](30) NULL,
	[Total_Weight] [real] NULL,
	[Sequence_Number] [int] NULL,
	[Mixing_Time] [int] NULL,
	[Bunker_1_Percent] [real] NULL,
	[Bunker_2_Percent] [real] NULL,
	[Bunker_3_Percent] [real] NULL,
	[Bunker_4_Percent] [real] NULL,
	[Bunker_5_Percent] [real] NULL,
	[Bunker_6_Percent] [real] NULL,
	[Bunker_7_Percent] [real] NULL,
	[Bunker_8_Percent] [real] NULL,
	[Bunker_9_Percent] [real] NULL,
	[Bunker_10_Percent] [real] NULL
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Bunker]  WITH CHECK ADD  CONSTRAINT [FK_Bunker_Bunker] FOREIGN KEY([Bunker_N])
REFERENCES [dbo].[Bunker] ([Bunker_N])
GO
ALTER TABLE [dbo].[Bunker] CHECK CONSTRAINT [FK_Bunker_Bunker]
GO
ALTER TABLE [dbo].[Material]  WITH CHECK ADD  CONSTRAINT [FK_Material_Bunker] FOREIGN KEY([Material_N])
REFERENCES [dbo].[Material] ([Material_N])
GO
ALTER TABLE [dbo].[Material] CHECK CONSTRAINT [FK_Material_Bunker]
GO
/****** Object:  DdlTrigger [OnTriggerDboSchema]    Script Date: 12.03.2020 11:56:54 ******/
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
ALTER DATABASE [Umix_Recipe] SET  READ_WRITE 
GO
