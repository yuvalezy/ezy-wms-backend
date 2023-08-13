CREATE TABLE [ServiceManager](
                                 [dbName] [nvarchar](100) NOT NULL,
                                 [Active] [char](1) NOT NULL,
                                 CONSTRAINT [PK_ServiceManager] PRIMARY KEY CLUSTERED
                                     (
                                      [dbName] ASC
                                         )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]