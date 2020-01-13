namespace Demo
{
    using System;
    using System.IO;

    using SharpDX.D3DCompiler;

    public class StreamInclude : Include
    {
        static string includeDirectory = "Shaders\\";

        public void Close( Stream stream ) { stream.Close(); }

        public void Dispose() { }

        public IDisposable Shadow { get; set; }

        public Stream Open( IncludeType type, string fileName, Stream parentStream )
        {
            return new FileStream( includeDirectory + fileName, FileMode.Open );
        }
    }
}
