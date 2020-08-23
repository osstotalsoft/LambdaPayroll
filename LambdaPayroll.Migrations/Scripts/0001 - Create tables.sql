/****** Object:  Table [dbo].[DbElemDefinition]    Script Date: 3/19/2020 11:11:13 AM ******/
SET ANSI_NULLS ON
GO 
SET QUOTED_IDENTIFIER ON  
GO 
CREATE TABLE [dbo].[DbElemDefinition](
	[DbElemDefinitionId] [int] IDENTITY(1,1) NOT NULL,
	[TableName] [nchar](10) NOT NULL,  
	[ColumnName] [nchar](10) NOT NULL,
	[ElemDefinitionId] [int] NOT NULL,
 CONSTRAINT [PK_DbElemDefinition] PRIMARY KEY CLUSTERED  
(
	[DbElemDefinitionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ElemDefinition]    Script Date: 3/19/2020 11:11:13 AM ******/
SET ANSI_NULLS ON
GO 
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ElemDefinition](
	[ElemDefinitionId] [int] IDENTITY(1,1) NOT NULL,
	[Code] [nvarchar](50) NOT NULL,
	[DataType] [nvarchar](20) NOT NULL,
 CONSTRAINT [PK_ElemDefinition] PRIMARY KEY CLUSTERED 
(
	[ElemDefinitionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ElemDependency]    Script Date: 3/19/2020 11:11:13 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ElemDependency](
	[ElemDependencyId] [int] IDENTITY(1,1) NOT NULL,
	[ElemDefinitionId] [int] NOT NULL,
	[DependencyElemDefinitionId] [int] NOT NULL,
 CONSTRAINT [PK_ElemDependency] PRIMARY KEY CLUSTERED 
(
	[ElemDependencyId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[FromulaElemDefinition]    Script Date: 3/19/2020 11:11:13 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FromulaElemDefinition](
	[FormulaId] [int] IDENTITY(1,1) NOT NULL,
	[Formula] [nvarchar](500) NOT NULL,
	[ElemDefinitionId] [int] NOT NULL,
 CONSTRAINT [PK_FromulaElemDefinition] PRIMARY KEY CLUSTERED 
(
	[FormulaId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  View [dbo].[VW_ElemDefinitions]    Script Date: 3/19/2020 11:11:13 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE view [dbo].[VW_ElemDefinitions] AS
	select 
		ed.Code, 
		ed.DataType, 
		(CASE 
			WHEN ded.DbElemDefinitionId IS NOT NULL THEN 'Db' 
			WHEN fed.FormulaId IS NOT NULL THEN 'Formula' END) as [Type],
		ded.[TableName], 
		ded.[ColumnName], 
		fed.Formula,
		STUFF((
			select ';'+ ed1.Code 
				from dbo.ElemDependency dep
				inner join dbo.ElemDefinition ed1 on dep.DependencyElemDefinitionId = ed1.ElemDefinitionId
				where dep.ElemDefinitionId = ed.ElemDefinitionId
			FOR XML PATH('')
			),1,1,'') as FormulaDeps
	from dbo.ElemDefinition ed
	left join dbo.DbElemDefinition ded on ed.ElemDefinitionId = ded.ElemDefinitionId
	left join dbo.FromulaElemDefinition fed on ed.ElemDefinitionId = fed.ElemDefinitionId
GO
ALTER TABLE [dbo].[DbElemDefinition]  WITH CHECK ADD  CONSTRAINT [FK_DbElemDefinition_ElemDefinition] FOREIGN KEY([ElemDefinitionId])
REFERENCES [dbo].[ElemDefinition] ([ElemDefinitionId])
GO
ALTER TABLE [dbo].[DbElemDefinition] CHECK CONSTRAINT [FK_DbElemDefinition_ElemDefinition]
GO
ALTER TABLE [dbo].[ElemDependency]  WITH CHECK ADD  CONSTRAINT [FK_ElemDependency_DependencyElemDefinition] FOREIGN KEY([DependencyElemDefinitionId])
REFERENCES [dbo].[ElemDefinition] ([ElemDefinitionId])
GO
ALTER TABLE [dbo].[ElemDependency] CHECK CONSTRAINT [FK_ElemDependency_DependencyElemDefinition]
GO
ALTER TABLE [dbo].[ElemDependency]  WITH CHECK ADD  CONSTRAINT [FK_ElemDependency_ElemDefinition] FOREIGN KEY([ElemDefinitionId])
REFERENCES [dbo].[ElemDefinition] ([ElemDefinitionId])
GO
ALTER TABLE [dbo].[ElemDependency] CHECK CONSTRAINT [FK_ElemDependency_ElemDefinition]
GO
ALTER TABLE [dbo].[FromulaElemDefinition]  WITH CHECK ADD  CONSTRAINT [FK_FromulaElemDefinition_ElemDefinition] FOREIGN KEY([ElemDefinitionId])
REFERENCES [dbo].[ElemDefinition] ([ElemDefinitionId])
GO
ALTER TABLE [dbo].[FromulaElemDefinition] CHECK CONSTRAINT [FK_FromulaElemDefinition_ElemDefinition]
GO
