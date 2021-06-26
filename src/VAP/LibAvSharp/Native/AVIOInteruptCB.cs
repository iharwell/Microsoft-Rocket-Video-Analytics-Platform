namespace LibAvSharp.Native
{
    unsafe public struct AVIOInteruptCB
    {
        public delegate* unmanaged[Cdecl]<void*, int> callback;
        public void* opaque;
    }
}