﻿using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

#if AVALONIA_CAIRO
namespace Avalonia.Cairo.RenderTests
#elif AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests
#else
namespace Avalonia.Direct2D1.RenderTests
#endif
{
    public class GeometryClippingTests : TestBase
    {
        public GeometryClippingTests()
            :base("GeometryClipping")
        {
        }

        [Fact]
        public void Geometry_Clip_Clips_Path()
        {
            var target = new Canvas
            {
                Clip = StreamGeometry.Parse("F1 M 0,0  H 76 V 76 Z"),
                Width = 76,
                Height = 76,
                Children = new Avalonia.Controls.Controls
                {
                    new Path
                    {
                        Width = 32,
                        Height = 40,
                        [Canvas.LeftProperty] = 23,
                        [Canvas.TopProperty] = 18,
                        Stretch = Stretch.Fill,
                        Fill = Brushes.Black,
                        Data = StreamGeometry.Parse("F1 M 27,18L 23,26L 33,30L 24,38L 33,46L 23,50L 27,58L 45,58L 55,38L 45,18L 27,18 Z")
                    }
                }
            };

            RenderToFile(target);
            CompareImages();
        }
    }
}
