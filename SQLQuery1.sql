 CREATE DATABASE InvestigacionIADB;
 GO

USE InvestigacionIADB;
 GO

CREATE TABLE Investigaciones (

    InvestigacionID INT IDENTITY(1,1) PRIMARY KEY,


    Prompt NVARCHAR(MAX) NOT NULL,

    Resultado NVARCHAR(MAX) NOT NULL,

    FechaConsulta DATETIME2 DEFAULT GETDATE() NOT NULL
);
GO
