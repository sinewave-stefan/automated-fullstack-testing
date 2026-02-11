using Game.Core;

namespace Game.UnitTests;

public class Vector2DTests
{
    [Fact]
    public void Vector2D_Length_CalculatesCorrectly()
    {
        var vector = new Vector2D(3, 4);
        Assert.Equal(5, vector.Length, 2);
    }

    [Fact]
    public void Vector2D_Normalize_CreatesUnitVector()
    {
        var vector = new Vector2D(3, 4);
        var normalized = vector.Normalize();
        
        Assert.Equal(1, normalized.Length, 2);
    }

    [Fact]
    public void Vector2D_Addition_WorksCorrectly()
    {
        var v1 = new Vector2D(1, 2);
        var v2 = new Vector2D(3, 4);
        var result = v1 + v2;
        
        Assert.Equal(4, result.X);
        Assert.Equal(6, result.Y);
    }

    [Fact]
    public void Vector2D_Subtraction_WorksCorrectly()
    {
        var v1 = new Vector2D(5, 7);
        var v2 = new Vector2D(2, 3);
        var result = v1 - v2;
        
        Assert.Equal(3, result.X);
        Assert.Equal(4, result.Y);
    }

    [Fact]
    public void Vector2D_ScalarMultiplication_WorksCorrectly()
    {
        var vector = new Vector2D(2, 3);
        var result = vector * 2;
        
        Assert.Equal(4, result.X);
        Assert.Equal(6, result.Y);
    }

    [Fact]
    public void Vector2D_Distance_CalculatesCorrectly()
    {
        var v1 = new Vector2D(0, 0);
        var v2 = new Vector2D(3, 4);
        var distance = Vector2D.Distance(v1, v2);
        
        Assert.Equal(5, distance, 2);
    }
}
