IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.StoolballPlayerInMatchStatistics')
      AND name = 'OppositionTeamId'
      AND is_nullable = 0
)
BEGIN
    PRINT 'Altering StoolballPlayerInMatchStatistics opposition columns to be nullable...';

    ALTER TABLE dbo.StoolballPlayerInMatchStatistics
        ALTER COLUMN OppositionTeamId UNIQUEIDENTIFIER NULL;

    ALTER TABLE dbo.StoolballPlayerInMatchStatistics
        ALTER COLUMN OppositionTeamName NVARCHAR(255) NULL;

    ALTER TABLE dbo.StoolballPlayerInMatchStatistics
        ALTER COLUMN OppositionTeamRoute NVARCHAR(255) NULL;

    PRINT 'Columns updated.';
END
ELSE
BEGIN
    PRINT 'StoolballPlayerInMatchStatistics.OppositionTeamId is already nullable. No changes made.';
END;