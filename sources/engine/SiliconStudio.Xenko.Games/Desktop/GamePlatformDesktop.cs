// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
//
// Copyright (c) 2010-2013 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
#if SILICONSTUDIO_PLATFORM_WINDOWS_DESKTOP
using System;
using System.IO;
using System.Reflection;

namespace SiliconStudio.Xenko.Games
{
    internal class GamePlatformDesktop : GamePlatform
    {
        public GamePlatformDesktop(GameBase game) : base(game)
        {
            IsBlockingRun = true;
#if SILICONSTUDIO_XENKO_GRAPHICS_API_DIRECT3D
                // This is required by the Audio subsystem of SharpDX.
            Win32Native.CoInitialize(IntPtr.Zero);
#endif
        }

        public override string DefaultAppDirectory
        {
            get
            {
                var assemblyUri = new Uri(Assembly.GetEntryAssembly().CodeBase);
                return Path.GetDirectoryName(assemblyUri.LocalPath);
            }
        }

        internal override GameWindow[] GetSupportedGameWindows()
        {
            return new GameWindow[]
                {
#if !SILICONSTUDIO_UI_SDL2
#if SILICONSTUDIO_XENKO_GRAPHICS_API_OPENGL
                    new GameWindowOpenTK(),
#endif
                    new GameWindowDesktop(),
#endif
                    // SDL is always available on Windows
                    new GameWindowDesktopSDL(),
                };
        }
    }
}
#endif
