using System.Numerics;

namespace LookOutTheWindow.Physics;

/// <summary>
/// AABB Bounding Box for collision detection.
/// Use reversed Y axis (center is origin).
///
/// +
/// |
/// Y
/// X----+
/// </summary>
public class BoundingBox
{
    public Vector2 Position;
    public Vector2 Size;
    
    public Vector2 Center => Position + Size / 2;
    public float Right => Position.X + Size.X;
    public float Left => Position.X;
    public float Top => Position.Y;
    public float Bottom => Position.Y - Size.Y;
    
    BoundingBox(Vector2 position, Vector2 size)
    {
        Position = position;
        Size = size;
    }
    
    public static BoundingBox CreateFromCenter(Vector2 center, Vector2 size)
    {
        return new BoundingBox(
            new Vector2(center.X - size.X / 2, center.Y + size.Y / 2),
            size);
    }
    
    public static BoundingBox CreateFromTopLeft(Vector2 topLeft, Vector2 size)
    {
        return new BoundingBox(
            topLeft,
            size);
    }
    
    public bool Intersects(BoundingBox other)
    {
        return Right > other.Left &&
               Left < other.Right &&
               Top > other.Bottom &&
               Bottom < other.Top;
    }
}