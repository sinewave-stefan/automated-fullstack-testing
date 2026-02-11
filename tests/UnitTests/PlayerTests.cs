using Game.Core;

namespace Game.UnitTests;

public class PlayerTests
{
    [Fact]
    public void Player_InitializesWithCorrectValues()
    {
        var player = new Player("TestPlayer", 100);
        
        Assert.Equal("TestPlayer", player.Name);
        Assert.Equal(100, player.Health);
        Assert.Equal(100, player.MaxHealth);
        Assert.True(player.IsAlive);
        Assert.Equal(0, player.Position.X);
        Assert.Equal(0, player.Position.Y);
    }

    [Fact]
    public void Player_Move_UpdatesPosition()
    {
        var player = new Player("TestPlayer");
        player.Move(10, 5);
        
        Assert.Equal(10, player.Position.X);
        Assert.Equal(5, player.Position.Y);
    }

    [Fact]
    public void Player_TakeDamage_ReducesHealth()
    {
        var player = new Player("TestPlayer", 100);
        player.TakeDamage(30);
        
        Assert.Equal(70, player.Health);
        Assert.True(player.IsAlive);
    }

    [Fact]
    public void Player_TakeDamage_CanKillPlayer()
    {
        var player = new Player("TestPlayer", 50);
        player.TakeDamage(60);
        
        Assert.Equal(0, player.Health);
        Assert.False(player.IsAlive);
    }

    [Fact]
    public void Player_Heal_IncreasesHealth()
    {
        var player = new Player("TestPlayer", 100);
        player.TakeDamage(50);
        player.Heal(30);
        
        Assert.Equal(80, player.Health);
    }

    [Fact]
    public void Player_Heal_CannotExceedMaxHealth()
    {
        var player = new Player("TestPlayer", 100);
        player.TakeDamage(20);
        player.Heal(50);
        
        Assert.Equal(100, player.Health);
    }

    [Fact]
    public void Player_TakeDamage_ThrowsOnNegative()
    {
        var player = new Player("TestPlayer");
        Assert.Throws<ArgumentException>(() => player.TakeDamage(-10));
    }

    [Fact]
    public void Player_Constructor_ThrowsOnNullName()
    {
        Assert.Throws<ArgumentNullException>(() => new Player(null!));
    }
}
