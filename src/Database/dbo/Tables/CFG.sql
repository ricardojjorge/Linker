CREATE TABLE [dbo].[CFG] (
    [id]   INT        IDENTITY (1, 1) NOT NULL,
    [name] NCHAR (10) NULL,
    [code] NCHAR (10) NOT NULL,
    [description] NCHAR(50) NULL, 
    CONSTRAINT [PK_CFG] PRIMARY KEY CLUSTERED ([id] ASC)
);

