using Silk.NET.OpenGL;

namespace LookOutTheWindow;

public unsafe class RenderTexture : IDisposable
{
    private GL _gl;

    public uint Framebuffer { get; }
    public uint Rbo { get; }
    public uint Texture { get; }
    public int Width { get; }
    public int Height { get; }
    
    public RenderTexture(GL gl, int width, int height)
    {
        this._gl = gl;

        Width = width;
        Height = height;
        
        Framebuffer = gl.GenFramebuffer();
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, Framebuffer);

        Texture = gl.GenTexture();
        gl.BindTexture(TextureTarget.Texture2D, Texture);

        byte[] n = [];
        fixed (byte* ptr = n)
        {
            gl.TexImage2D(TextureTarget.Texture2D,
                0,
                InternalFormat.Rgba,
                (uint)width,
                (uint)height,
                0,
                PixelFormat.Rgba,
                PixelType.UnsignedByte, 
                ptr);
        }

        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, Texture, 0);
        
        Rbo = gl.GenRenderbuffer();
        gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, Rbo);
        gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.Depth24Stencil8, (uint)width, (uint)height);
        gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, Rbo);
        if (gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete)
        {
            throw new Exception("Framebuffer is not complete!");
        }
        
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void Dispose()
    {
        _gl.DeleteFramebuffer(Framebuffer);
        _gl.DeleteRenderbuffer(Rbo);
        _gl.DeleteTexture(Texture);
    }
}