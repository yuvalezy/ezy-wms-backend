CREATE COLUMN TABLE "ServiceManager"(
	"dbName" nvarchar(100) NOT NULL,
	"Active" char(1) NOT NULL,
	PRIMARY KEY("dbName")
) UNLOAD PRIORITY 5 AUTO MERGE