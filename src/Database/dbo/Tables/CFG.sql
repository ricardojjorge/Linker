﻿CREATE TABLE [dbo].[CFG] (
    [id]   INT        IDENTITY (1, 1) NOT NULL,
    [name] NCHAR (20) NULL,
    [value] NCHAR (50) NOT NULL,
    [description] NCHAR(50) NOT NULL, 
    CONSTRAINT [PK_CFG] PRIMARY KEY CLUSTERED ([id] ASC)
);

