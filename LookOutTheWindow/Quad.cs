using Silk.NET.OpenGL;

namespace LookOutTheWindow;

public class Quad : IDisposable
{
    private GL _gl;
    
    public static float[] Vertices =
    {
        // Position + TexCoords
        1.0f, 1.0f, 0.0f, 1.0f, 1.0f,
        1.0f, -1.0f, 0.0f, 1.0f, 0.0f,
        -1.0f, -1.0f, 0.0f, 0.0f, 0.0f,
        -1.0f, 1.0f, 0.0f, 0.0f, 1.0f
    };
    
    public static uint[] Indices =
    {
        0u, 1u, 3u,
        1u, 2u, 3u
    };

    public uint Vbo;
    public uint Ebo;
    public uint Vao;
    
    public Quad(GL gl)
    {
        _gl = gl;
        Vao = gl.GenVertexArray();
        gl.BindVertexArray(Vao);
        
        Vbo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, Vbo);
        unsafe
        {
            fixed (float* v = Vertices)
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(Vertices.Length * sizeof(float)), v, BufferUsageARB.StaticDraw);
            }
        }

        Ebo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, Ebo);
        unsafe
        {
            fixed (uint* i = Indices)
            {
                gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(Indices.Length * sizeof(uint)), i, BufferUsageARB.StaticDraw);
            }
        }
        
        gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
        gl.EnableVertexAttribArray(0);
        
        gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
        gl.EnableVertexAttribArray(1);
        
        gl.BindVertexArray(0);
        //gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        //gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
    }
    
    public unsafe void Draw(GL gl)
    {
        gl.BindVertexArray(Vao);
        gl.DrawElements(PrimitiveType.Triangles, 6u, DrawElementsType.UnsignedInt, (void*)0);
    }

    public void Dispose()
    {
        _gl.DeleteVertexArray(Vao);
        _gl.DeleteBuffer(Ebo);
        _gl.DeleteBuffer(Vbo);
    }
}