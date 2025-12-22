using System.Numerics;
using Silk.NET.OpenGL;

namespace LookOutTheWindow;

public class ShaderProgram : IDisposable
{
    private GL _gl;

    public uint Program;

    public ShaderProgram(GL gl, string vshSrc, string fshSrc)
    { 
        uint vertexShader = gl.CreateShader(ShaderType.VertexShader);
        gl.ShaderSource(vertexShader, vshSrc);
        gl.CompileShader(vertexShader);
        
        string infoLog = gl.GetShaderInfoLog(vertexShader);
        if (!string.IsNullOrWhiteSpace(infoLog))
        {
            throw new Exception($"Error compiling vertex shader {infoLog}");
        }
        
        uint fragmentShader = gl.CreateShader(ShaderType.FragmentShader);
        gl.ShaderSource(fragmentShader, fshSrc);
        gl.CompileShader(fragmentShader);
        
        infoLog = gl.GetShaderInfoLog(fragmentShader);
        if (!string.IsNullOrWhiteSpace(infoLog))
        {
            throw new Exception($"Error compiling fragment shader {infoLog}");
        }

        Program = gl.CreateProgram();
        gl.AttachShader(Program, vertexShader);
        gl.AttachShader(Program, fragmentShader);
        gl.LinkProgram(Program);
        
        gl.GetProgram(Program, GLEnum.LinkStatus, out int status);
        if (status == 0)
        {
            infoLog = gl.GetProgramInfoLog(Program);
            throw new Exception($"Error linking shader program: {infoLog}");
        }
        
        gl.DeleteShader(vertexShader);
        gl.DeleteShader(fragmentShader);
        
        _gl = gl;
    }
    
    public void Use()
    {
        _gl.UseProgram(Program);
    }
    
    public void Dispose()
    {
        _gl.DeleteProgram(Program);
    }
    
    public void SetInteger(string name, int value)
    {
        int location = _gl.GetUniformLocation(Program, name);
        _gl.Uniform1(location, value);
    }
    
    public void SetFloat(string name, float value)
    {
        int location = _gl.GetUniformLocation(Program, name);
        _gl.Uniform1(location, value);
    }
    
    public unsafe void SetMatrix4(string name, Matrix4x4 matrix)
    {
        int location = _gl.GetUniformLocation(Program, name);
        _gl.UniformMatrix4(location, 1, false, (float*)&matrix);
    }
    
    public void SetVector3(string name, Vector3 vector)
    {
        int location = _gl.GetUniformLocation(Program, name);
        _gl.Uniform3(location, vector.X, vector.Y, vector.Z);
    }
    
    public void SetVector2(string name, Vector2 vector)
    {
        int location = _gl.GetUniformLocation(Program, name);
        _gl.Uniform2(location, vector.X, vector.Y);
    }
    
    public void SetVector4(string name, Vector4 vector)
    {
        int location = _gl.GetUniformLocation(Program, name);
        _gl.Uniform4(location, vector.X, vector.Y, vector.Z, vector.W);
    }
}