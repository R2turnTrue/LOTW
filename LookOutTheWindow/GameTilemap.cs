using System.Drawing;
using System.Numerics;
using LookOutTheWindow.Map;
using LookOutTheWindow.Physics;
using Silk.NET.Vulkan;
using TiledSharp;

namespace LookOutTheWindow;

public class GameTilemap(MainScene scn) : IDisposable
{
    public struct ImportedTilemap
    {
        public Guid Id;
        public Vector2 Offset;
        public TmxMap Map;
        public Vector2 Spawn;
    }
    
    public List<MovingSpike> Spikes = new();
    public List<MovingPlatform> MovingPlatforms = new();
    public List<UpdownDoor> UpdownDoors = new();
    
    public List<ImportedTilemap> TiledMaps = new();
    public Sprite Tileset;
    public Sprite Spike;
    public Sprite SpikeFrozen;

    public Sprite MovingPlatform;
    public Sprite MovingPlatformFrozen;
    
    public Sprite UpdownDoor;
    public Sprite UpdownDoorFrozen;

    public void Load()
    {
        Tileset = new Sprite(Game.Instance, "assets/sprites/tileset.png");
        Spike = new Sprite(Game.Instance, "assets/sprites/spike.png");
        SpikeFrozen = new Sprite(Game.Instance, "assets/sprites/spike_frozen.png");
        
        MovingPlatform = new Sprite(Game.Instance, "assets/sprites/moving_platform.png");
        MovingPlatformFrozen = new Sprite(Game.Instance, "assets/sprites/moving_platform_frozen.png");
        
        UpdownDoor = new Sprite(Game.Instance, "assets/sprites/updown_door.png");
        UpdownDoorFrozen = new Sprite(Game.Instance, "assets/sprites/updown_door_frozen.png");
        
        LoadMap(new ImportedTilemap
        {
            Id = Guid.NewGuid(),
            Map = new TmxMap("tiled/level0.tmx"),
            Offset = new Vector2(0, 0)
        });
    }

    public void ReloadSpecificMap(ImportedTilemap tm)
    {
        var idx = TiledMaps.FindIndex(t => t.Id == tm.Id);
        Console.WriteLine("Reloading Map: " + idx);
        if (idx != -1)
        {
            Spikes.RemoveAll(s => s.ParentTilemap != null && s.ParentTilemap.Value.Id == tm.Id);
            MovingPlatforms.RemoveAll(p => p.ParentTilemap != null && p.ParentTilemap.Value.Id == tm.Id);
            UpdownDoors.RemoveAll(d => d.ParentTilemap != null && d.ParentTilemap.Value.Id == tm.Id);
            
            TiledMaps[idx] = tm;
            LoadMap(tm, false, true, idx);
        }
    }

    public void LoadMap(ImportedTilemap tm, bool addMap = true, bool noChild = false, int idx = -1)
    {
        var map = tm.Map;
        
        if (idx == -1)
            idx = TiledMaps.Count;
        
        if (addMap)
            TiledMaps.Add(tm);

        if (map.ObjectGroups.Count >= 1)
        { 
            foreach (var obj in map.ObjectGroups[0].Objects)
            {
                if (obj != null)
                {
                    if (obj.Name == "PlayerSpawn")
                    {
                        var m = TiledMaps[idx];
                        m.Spawn = tm.Offset + new Vector2((float)obj.X, (float)obj.Y);
                        TiledMaps[idx] = m;
                    }

                    if (!noChild && obj.Name.StartsWith("ep_"))
                    {
                        LoadMap(new ImportedTilemap
                        {
                            Id = Guid.NewGuid(),
                            Map = new TmxMap("tiled/" + obj.Name.Replace("ep_", "") + ".tmx"),
                            Offset = new Vector2((float)obj.X, (float)obj.Y) + tm.Offset
                        });
                    }

                    var mw = Game.Instance.MainWindow;
                    
                    if (obj.Name == "MovingSpike")
                    {
                        var spike = new MovingSpike(scn, Spike, SpikeFrozen);
                        spike.ParentTilemap = tm;
                        spike.Position = tm.Offset + mw.ViewportPosToWorld(new Vector2((float)obj.X, (float)obj.Y));
                        Spikes.Add(spike);
                    }
                    
                    if (obj.Name == "MovingPlatform")
                    {
                        var platform = new MovingPlatform(scn, MovingPlatform, MovingPlatformFrozen);
                        platform.ParentTilemap = tm;
                        platform.Position = tm.Offset + mw.ViewportPosToWorld(new Vector2((float)obj.X, (float)obj.Y));
                        MovingPlatforms.Add(platform);
                    }
                    
                    if (obj.Name == "UpdownDoor")
                    {
                        var door = new UpdownDoor(scn, UpdownDoor, UpdownDoorFrozen);
                        door.ParentTilemap = tm;
                        door.Position = tm.Offset + mw.ViewportPosToWorld(new Vector2((float)obj.X, (float)obj.Y));
                        UpdownDoors.Add(door);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Tries to find a tilemap that contains the given point in world space.
    /// !!! By Viewport Coordniates
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public ImportedTilemap? GetMapContainingPoint(Vector2 point)
    {
        foreach (var tm in TiledMaps)
        {
            var map = tm.Map;
            var mapRect = new RectangleF(
                tm.Offset.X,
                tm.Offset.Y,
                map.Width * map.TileWidth,
                map.Height * map.TileHeight);
            
            if (mapRect.Contains(point.X, point.Y))
            {
                return tm;
            }
        }

        return null;
    }

    public (bool, BoundingBox) IntersectsTile(GameWindow window, BoundingBox collider, ImportedTilemap map, int i)
    {
        var tiledMap = map.Map;
        
        var tileWidth = 16;
        var tileHeight = 16;
        
        float x = (i % tiledMap.Width) * tiledMap.TileWidth;
        float y = (float)Math.Floor(i / (double)tiledMap.Width) * tiledMap.TileHeight;
                
        var pos = window.ViewportPosToWorld(new Vector2(x, y));
                
        var tileCollider = BoundingBox.CreateFromTopLeft(
            new Vector2(pos.X + map.Offset.X, pos.Y - map.Offset.Y),
            new Vector2(tileWidth, tileHeight));

        return (collider.Intersects(tileCollider), tileCollider);
    }
    
    public (bool, BoundingBox?) Intersects(GameWindow window, BoundingBox collider, ImportedTilemap map)
    {
        var tiledMap = map.Map;
        var tileWidth = 16;
        var tileHeight = 16;

        var tilesetTilesWide = Tileset.TextureWidth / tileWidth;
        var tilesetTilesHigh = Tileset.TextureHeight / tileHeight;

        for (var i = 0; i < tiledMap.Layers[0].Tiles.Count; i++)
        {
            int gid = tiledMap.Layers[0].Tiles[i].Gid;

            // Empty tile, do nothing
            if (gid == 0)
            {
            }
            else
            {
                (bool isCollide, BoundingBox tileCollider) = IntersectsTile(window, collider, map, i);
                
                if (isCollide)
                {
                    return (true, tileCollider);
                }
            }
        }
        return (false, null);
    }
    
    public (bool, BoundingBox?) Intersects(GameWindow window, BoundingBox collider, bool checkMovingPlatforms = true, bool checkUpdownDoors = true)
    {
        if (checkMovingPlatforms)
        {
            foreach (var platform in MovingPlatforms)
            {
                if (platform.Collider.Intersects(collider))
                {
                    return (true, platform.Collider);
                }
            }
        }
        
        if (checkUpdownDoors)
        {
            foreach (var door in UpdownDoors)
            {
                if (door.Collider.Intersects(collider))
                {
                    return (true, door.Collider);
                }
            }
        }
        
        foreach (var tiledMap in TiledMaps)
        {
            var result = Intersects(window, collider, tiledMap);
            if (result.Item1)
            {
                return result;
            }
        }

        return (false, null);
    }
    
    public void Draw(GameWindow window, double deltaTime)
    {
        foreach (var tiledMap in TiledMaps)
        {
            Draw(window, deltaTime, tiledMap);
        }
        
        foreach (var spike in Spikes)
        {
            spike.Draw(window);
        }
        
        foreach (var platform in MovingPlatforms)
        {
            platform.Draw(window);
        }
        
        foreach (var door in UpdownDoors)
        {
            door.Draw(window);
        }
    }
    
    public void Update(GameWindow window, double deltaTime, ImportedTilemap? currentTilemap)
    {
        if (!currentTilemap.HasValue) return;
        
        foreach (var spike in Spikes)
        {
            if (spike.ParentTilemap != null && spike.ParentTilemap.Value.Id != currentTilemap.Value.Id)
                continue;
            
            spike.Update(window, (float)deltaTime);
        }
        
        foreach (var platform in MovingPlatforms)
        {
            if (platform.ParentTilemap != null && platform.ParentTilemap.Value.Id != currentTilemap.Value.Id)
                continue;
            
            platform.Update(window, (float)deltaTime);
        }
        
        foreach (var door in UpdownDoors)
        {
            if (door.ParentTilemap != null && door.ParentTilemap.Value.Id != currentTilemap.Value.Id)
                continue;
            
            door.Update(window, (float)deltaTime);
        }
    }

    public void Draw(GameWindow window, double deltaTime, ImportedTilemap map)
    {
        var tiledMap = map.Map;
        
        var tileWidth = 16;
        var tileHeight = 16;

        var tilesetTilesWide = Tileset.TextureWidth / tileWidth;
        var tilesetTilesHigh = Tileset.TextureHeight / tileHeight;

        for (var i = 0; i < tiledMap.Layers[0].Tiles.Count; i++)
        {
            int gid = tiledMap.Layers[0].Tiles[i].Gid;

            // Empty tile, do nothing
            if (gid == 0)
            {
            }
            else
            {
                int tileFrame = gid - 1;
                int column = tileFrame % tilesetTilesWide;
                int row = (int)Math.Floor((double)tileFrame / (double)tilesetTilesWide);

                float x = (i % tiledMap.Width) * tiledMap.TileWidth;
                float y = (float)Math.Floor(i / (double)tiledMap.Width) * tiledMap.TileHeight;

                Tileset.Width = tileWidth;
                Tileset.Height = tileHeight;
                Tileset.TexX = tileWidth * column;
                Tileset.TexY = tileHeight * row;
                
                var pos = window.ViewportPosToWorld(new Vector2(x, y));
                window.DrawSprite(Tileset, pos.X + tileWidth / 2.0f + map.Offset.X, pos.Y - tileHeight / 2.0f - map.Offset.Y, Color.White);

                //Console.WriteLine("Drawing tilemap tile at " + x + ", " + y);
            }
        }
    }

    public void Dispose()
    {
        TiledMaps.Clear();
        Tileset.Dispose();
        
        Spikes.Clear();
        MovingPlatform.Dispose();
        MovingPlatformFrozen.Dispose();
        
        UpdownDoors.Clear();
        UpdownDoor.Dispose();
        UpdownDoorFrozen.Dispose();
        
        MovingPlatforms.Clear();
        
        Spike.Dispose();
        SpikeFrozen.Dispose();
    }
}