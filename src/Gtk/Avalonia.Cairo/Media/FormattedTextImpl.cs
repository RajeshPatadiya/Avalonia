// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Cairo.Media
{
    public class FormattedTextImpl : IFormattedTextImpl
    {
        private Size _size;

        static double CorrectScale(double input)
        {
            return input * 0.75;
        }

        public FormattedTextImpl(
            Pango.Context context,
            string text,
            string fontFamily,
            double fontSize,
            FontStyle fontStyle,
            TextAlignment textAlignment,
            FontWeight fontWeight)
        {
            Contract.Requires<ArgumentNullException>(context != null);
            Contract.Requires<ArgumentNullException>(text != null);
            Layout = new Pango.Layout(context);
            Layout.SetText(text);
            Layout.FontDescription = new Pango.FontDescription
            {
                Family = fontFamily,
                Size = Pango.Units.FromDouble(CorrectScale(fontSize)),
                Style = (Pango.Style)fontStyle,
                Weight = fontWeight.ToCairo()
            };

            Layout.Alignment = textAlignment.ToCairo();
            Layout.Attributes = new Pango.AttrList();
        }

        public string Text => Layout.Text;

        public Size Constraint
        {
            get
            {
                return _size;
            }

            set
            {
                _size = value;
                Layout.Width = double.IsPositiveInfinity(value.Width) ?
                    -1 : Pango.Units.FromDouble(value.Width);
            }
        }

        public Pango.Layout Layout
        {
            get;
        }

        public void Dispose()
        {
            Layout.Dispose();
        }

        public IEnumerable<FormattedTextLine> GetLines()
        {
            return new FormattedTextLine[0];
        }

        public TextHitTestResult HitTestPoint(Point point)
        {
            int textPosition;
            int trailing;

            var isInside = Layout.XyToIndex(
                Pango.Units.FromDouble(point.X),
                Pango.Units.FromDouble(point.Y),
                out textPosition,
                out trailing);

            textPosition = PangoIndexToTextIndex(textPosition);

            return new TextHitTestResult
            {
                IsInside = isInside,
                TextPosition = textPosition,
                IsTrailing = trailing == 0,
            };
        }

        int PangoIndexToTextIndex(int pangoIndex)
        {
            return Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(Text), 0, Math.Min(pangoIndex, Text.Length)).Length;
        }

        public Rect HitTestTextPosition(int index)
        {
            return Layout.IndexToPos(TextIndexToPangoIndex(index)).ToAvalonia();
        }

        int TextIndexToPangoIndex(int textIndex)
        {
            return Encoding.UTF8.GetByteCount(textIndex < Text.Length ? Text.Remove(textIndex) : Text);
        }

        public IEnumerable<Rect> HitTestTextRange(int index, int length)
        {
            var ranges = new List<Rect>();

            for (var i = 0; i < length; i++)
            {
                ranges.Add(HitTestTextPosition(index + i));
            }

            return ranges;
        }

        public Size Measure()
        {
            int width;
            int height;
            Layout.GetPixelSize(out width, out height);

            return new Size(width, height);
        }

        public void SetForegroundBrush(IBrush brush, int startIndex, int count)
        {
            var scb = brush as SolidColorBrush;
            if (scb != null)
            {

                var color = new Pango.Color();
                color.Parse(string.Format("#{0}", scb.Color.ToString().Substring(3)));

                var brushAttr = new Pango.AttrForeground(color);
                brushAttr.StartIndex = (uint)TextIndexToPangoIndex(startIndex);
                brushAttr.EndIndex = (uint)TextIndexToPangoIndex(startIndex + count);

                Layout.Attributes.Insert(brushAttr);
            }
        }
    }
}
