SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		Rick Mason
-- Create date: 25 August 2022
-- Description:	Links a player to a member account, merging with the existing player if one is already linked
-- =============================================
CREATE OR ALTER PROCEDURE usp_Link_Player_To_Member
	@MemberKey uniqueidentifier, 
	@PlayerId uniqueidentifier
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    BEGIN TRAN

	-- Is the player already claimed, either by this member or someone else?
	SELECT MemberKey FROM StoolballPlayer WHERE PlayerId = @PlayerId AND MemberKey IS NOT NULL
	IF @@ROWCOUNT > 0
	BEGIN
		ROLLBACK TRAN
		RETURN 
	END

	-- Is this member already linked to a player?
	DECLARE @ExistingPlayerId uniqueidentifier, @ExistingPlayerRoute nvarchar(255)
	SELECT TOP 1 @ExistingPlayerId = PlayerId, @ExistingPlayerRoute = PlayerRoute FROM StoolballPlayer WHERE MemberKey = @MemberKey

	IF @ExistingPlayerId IS NULL
		-- Link member to player record
		UPDATE StoolballPlayer SET MemberKey = @MemberKey WHERE PlayerId = @PlayerId
	ELSE
		BEGIN
			-- Move the player identities from this player id to the member's player id
			UPDATE StoolballPlayerIdentity SET PlayerId = @ExistingPlayerId WHERE PlayerId = @PlayerId
			UPDATE StoolballPlayerInMatchStatistics SET PlayerId = @ExistingPlayerId, PlayerRoute = @ExistingPlayerRoute WHERE PlayerId = @PlayerId
			DELETE FROM StoolballPlayer WHERE PlayerId = @PlayerId
		END

	COMMIT TRAN
END
GO
