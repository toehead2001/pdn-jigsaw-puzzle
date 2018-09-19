using System;
using System.Drawing;
using System.Reflection;
using System.Collections.Generic;
using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;

namespace JigsawPuzzleEffect
{
    public class PluginSupportInfo : IPluginSupportInfo
    {
        public string Author => base.GetType().Assembly.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright;
        public string Copyright => L10nStrings.EffectDescription;
        public string DisplayName => L10nStrings.EffectName;
        public Version Version => base.GetType().Assembly.GetName().Version;
        public Uri WebsiteUri => new Uri("https://forums.getpaint.net/index.php?showtopic=32391");

        public string plugin_browser_Keywords => L10nStrings.EffectKeywords;
        public string plugin_browser_Description => L10nStrings.EffectDescription;
    }

    [PluginSupportInfo(typeof(PluginSupportInfo))]
    public class JigsawPuzzleEffectPlugin : PropertyBasedEffect
    {
        private static readonly Image StaticIcon = new Bitmap(typeof(JigsawPuzzleEffectPlugin), "JigsawPuzzle.png");

        public JigsawPuzzleEffectPlugin()
            : base(L10nStrings.EffectName, StaticIcon, SubmenuNames.Render, EffectFlags.Configurable)
        {
        }

        private enum Apex
        {
            Up,
            Right,
            Down,
            Left
        }

        private enum PropertyNames
        {
            Scale,
            LineWidth,
            Pattern,
            Transparent,
            LineColor,
            Offset
        }

        private enum Pattern
        {
            AltHorVer,
            AltNone,
            AltHor,
            AltVer
        }


        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> props = new List<Property>
            {
                new DoubleProperty(PropertyNames.Scale, 1, 0.2, 10),
                new Int32Property(PropertyNames.LineWidth, 2, 1, 10),
                StaticListChoiceProperty.CreateForEnum<Pattern>(PropertyNames.Pattern, 0, false),
                new BooleanProperty(PropertyNames.Transparent, true),
                new Int32Property(PropertyNames.LineColor, ColorBgra.ToOpaqueInt32(ColorBgra.FromBgra(EnvironmentParameters.PrimaryColor.B, EnvironmentParameters.PrimaryColor.G, EnvironmentParameters.PrimaryColor.R, 255)), 0, 0xffffff),
                new DoubleVectorProperty(PropertyNames.Offset, Pair.Create(0.0, 0.0), Pair.Create(-1.0, -1.0), Pair.Create(+1.0, +1.0))
            };

            List<PropertyCollectionRule> propRules = new List<PropertyCollectionRule>
            {
                new ReadOnlyBoundToBooleanRule(PropertyNames.LineColor, PropertyNames.Transparent, false)
            };

            return new PropertyCollection(props, propRules);
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            configUI.SetPropertyControlValue(PropertyNames.Scale, ControlInfoPropertyNames.DisplayName, L10nStrings.Scale);
            configUI.SetPropertyControlValue(PropertyNames.Scale, ControlInfoPropertyNames.SliderLargeChange, 0.25);
            configUI.SetPropertyControlValue(PropertyNames.Scale, ControlInfoPropertyNames.SliderSmallChange, 0.05);
            configUI.SetPropertyControlValue(PropertyNames.Scale, ControlInfoPropertyNames.UpDownIncrement, 0.001);
            configUI.SetPropertyControlValue(PropertyNames.Scale, ControlInfoPropertyNames.DecimalPlaces, 3);
            configUI.SetPropertyControlValue(PropertyNames.LineWidth, ControlInfoPropertyNames.DisplayName, L10nStrings.LineWidth);
            configUI.SetPropertyControlValue(PropertyNames.Pattern, ControlInfoPropertyNames.DisplayName, L10nStrings.Pattern);
            PropertyControlInfo patternControl = configUI.FindControlForPropertyName(PropertyNames.Pattern);
            patternControl.SetValueDisplayName(Pattern.AltHorVer, L10nStrings.AltHorVer);
            patternControl.SetValueDisplayName(Pattern.AltNone, L10nStrings.AltNone);
            patternControl.SetValueDisplayName(Pattern.AltHor, L10nStrings.AltHor);
            patternControl.SetValueDisplayName(Pattern.AltVer, L10nStrings.AltVer);
            configUI.SetPropertyControlValue(PropertyNames.Transparent, ControlInfoPropertyNames.DisplayName, L10nStrings.LineColor);
            configUI.SetPropertyControlValue(PropertyNames.Transparent, ControlInfoPropertyNames.Description, L10nStrings.Transparent);
            configUI.SetPropertyControlValue(PropertyNames.LineColor, ControlInfoPropertyNames.DisplayName, string.Empty);
            configUI.SetPropertyControlType(PropertyNames.LineColor, PropertyControlType.ColorWheel);
            configUI.SetPropertyControlValue(PropertyNames.Offset, ControlInfoPropertyNames.DisplayName, L10nStrings.Position);
            configUI.SetPropertyControlValue(PropertyNames.Offset, ControlInfoPropertyNames.SliderSmallChangeX, 0.05);
            configUI.SetPropertyControlValue(PropertyNames.Offset, ControlInfoPropertyNames.SliderLargeChangeX, 0.25);
            configUI.SetPropertyControlValue(PropertyNames.Offset, ControlInfoPropertyNames.UpDownIncrementX, 0.001);
            configUI.SetPropertyControlValue(PropertyNames.Offset, ControlInfoPropertyNames.SliderSmallChangeY, 0.05);
            configUI.SetPropertyControlValue(PropertyNames.Offset, ControlInfoPropertyNames.SliderLargeChangeY, 0.25);
            configUI.SetPropertyControlValue(PropertyNames.Offset, ControlInfoPropertyNames.UpDownIncrementY, 0.001);
            configUI.SetPropertyControlValue(PropertyNames.Offset, ControlInfoPropertyNames.DecimalPlaces, 3);
            Rectangle selBounds = EnvironmentParameters.GetSelection(EnvironmentParameters.SourceSurface.Bounds).GetBoundsInt();
            ImageResource selImage = ImageResource.FromImage(EnvironmentParameters.SourceSurface.CreateAliasedBitmap(selBounds));
            configUI.SetPropertyControlValue(PropertyNames.Offset, ControlInfoPropertyNames.StaticImageUnderlay, selImage);

            return configUI;
        }

        double scale = 1; // [0.2,5] Scale
        int lineWidth = 2; // [1,10] Line Width
        Pattern pattern = 0; // Pattern|Pattern A|Pattern B|Pattern C|Pattern D
        bool transparent = true; // [0,1] Transparent
        ColorBgra lineColor = ColorBgra.FromBgr(0, 0, 0); // Line Color
        Pair<double, double> offset = Pair.Create(0.0, 0.0); // Offset

        readonly BinaryPixelOp normalOp = LayerBlendModeUtil.CreateCompositionOp(LayerBlendMode.Normal);
        Surface puzzleSurface;
        int horLoops, verLoops;
        double gridScale;
        Rectangle puzzleRect;

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            scale = newToken.GetProperty<DoubleProperty>(PropertyNames.Scale).Value;
            lineWidth = newToken.GetProperty<Int32Property>(PropertyNames.LineWidth).Value;
            pattern = (Pattern)newToken.GetProperty<StaticListChoiceProperty>(PropertyNames.Pattern).Value;
            transparent = newToken.GetProperty<BooleanProperty>(PropertyNames.Transparent).Value;
            lineColor = ColorBgra.FromOpaqueInt32(newToken.GetProperty<Int32Property>(PropertyNames.LineColor).Value);
            offset = newToken.GetProperty<DoubleVectorProperty>(PropertyNames.Offset).Value;


            Rectangle selection = EnvironmentParameters.GetSelection(srcArgs.Surface.Bounds).GetBoundsInt();

            gridScale = 100 * scale;

            puzzleRect = new Rectangle
            {
                X = selection.Left + (int)Math.Round((selection.Width % gridScale) / 2 + (offset.First * (selection.Width % gridScale) / 2)),
                Y = selection.Top + (int)Math.Round((selection.Height % gridScale) / 2 + (offset.Second * (selection.Height % gridScale) / 2)),
                Width = selection.Width - (int)(selection.Width % gridScale),
                Height = selection.Height - (int)(selection.Height % gridScale)
            };

            horLoops = puzzleRect.Height / (int)(gridScale);
            verLoops = puzzleRect.Width / (int)(gridScale);

            // Puzzle Pattern
            bool horAlt, verAlt;
            switch (pattern)
            {
                case Pattern.AltHorVer:
                    horAlt = true;
                    verAlt = true;
                    break;
                case Pattern.AltNone:
                    horAlt = false;
                    verAlt = false;
                    break;
                case Pattern.AltHor:
                    horAlt = true;
                    verAlt = false;
                    break;
                case Pattern.AltVer:
                    horAlt = false;
                    verAlt = true;
                    break;
                default:
                    horAlt = true;
                    verAlt = true;
                    break;
            }

            if (puzzleSurface == null)
                puzzleSurface = new Surface(srcArgs.Surface.Size);
            else
                puzzleSurface.Clear(ColorBgra.Transparent);

            using (Graphics puzzleGraphics = new RenderArgs(puzzleSurface).Graphics)
            using (Pen puzzlePen = new Pen(lineColor, lineWidth))
            {
                puzzleGraphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                PointF[] curvePoints;

                //Horizontal Lines
                #region Horizontal Lines
                for (int i = 0; i < horLoops - 1; i++)
                {
                    if (horAlt && i % 2 == 0)
                    {
                        for (int i2 = 0; i2 < verLoops; i2++)
                        {
                            if (i2 % 2 != 0) // upper apex on odds
                            {
                                curvePoints = getCurvePoints(Apex.Up, i, i2); // upper apex
                            }
                            else
                            {
                                curvePoints = getCurvePoints(Apex.Down, i, i2); // lower apex
                            }

                            puzzleGraphics.DrawCurve(puzzlePen, curvePoints);
                        }
                    }
                    else
                    {
                        for (int i2 = 0; i2 < verLoops; i2++)
                        {
                            if (i2 % 2 == 0) // upper apex on evens
                            {
                                curvePoints = getCurvePoints(Apex.Up, i, i2); // upper apex
                            }
                            else
                            {
                                curvePoints = getCurvePoints(Apex.Down, i, i2); // lower apex
                            }

                            puzzleGraphics.DrawCurve(puzzlePen, curvePoints);
                        }
                    }
                }
                #endregion

                //Vertical Lines
                #region Vertical Lines
                for (int i = 0; i < verLoops - 1; i++)
                {
                    if (verAlt && i % 2 == 0)
                    {
                        for (int i2 = 0; i2 < horLoops; i2++)
                        {
                            if (i2 % 2 != 0) // right apex on odds
                            {
                                curvePoints = getCurvePoints(Apex.Right, i, i2); // right apex
                            }
                            else
                            {
                                curvePoints = getCurvePoints(Apex.Left, i, i2); // left apex
                            }

                            puzzleGraphics.DrawCurve(puzzlePen, curvePoints);
                        }
                    }
                    else
                    {
                        for (int i2 = 0; i2 < horLoops; i2++)
                        {
                            if (i2 % 2 == 0) // right apex on evens
                            {
                                curvePoints = getCurvePoints(Apex.Right, i, i2); // right apex
                            }
                            else
                            {
                                curvePoints = getCurvePoints(Apex.Left, i, i2); // left apex
                            }

                            puzzleGraphics.DrawCurve(puzzlePen, curvePoints);
                        }
                    }
                }
                #endregion
            }


            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);
        }

        protected override void OnRender(Rectangle[] renderRects, int startIndex, int length)
        {
            if (length == 0) return;
            for (int i = startIndex; i < startIndex + length; ++i)
            {
                Render(DstArgs.Surface, SrcArgs.Surface, renderRects[i]);
            }
        }

        // Define Curve points
        PointF[] getCurvePoints(Apex apexLocation, int i, int i2)
        {
            PointF[] points = new PointF[7];

            switch (apexLocation)
            {
                case Apex.Right:
                    points[0] = new PointF((float)(100 * scale + gridScale * i + puzzleRect.X), (float)(0 * scale + gridScale * i2 + puzzleRect.Y));
                    points[1] = new PointF((float)(99 * scale + gridScale * i + puzzleRect.X), (float)(40 * scale + gridScale * i2 + puzzleRect.Y));
                    points[2] = new PointF((float)(121 * scale + gridScale * i + puzzleRect.X), (float)(36 * scale + gridScale * i2 + puzzleRect.Y));
                    points[3] = new PointF((float)(133 * scale + gridScale * i + puzzleRect.X), (float)(50 * scale + gridScale * i2 + puzzleRect.Y));
                    points[4] = new PointF((float)(121 * scale + gridScale * i + puzzleRect.X), (float)(64 * scale + gridScale * i2 + puzzleRect.Y));
                    points[5] = new PointF((float)(99 * scale + gridScale * i + puzzleRect.X), (float)(60 * scale + gridScale * i2 + puzzleRect.Y));
                    points[6] = new PointF((float)(100 * scale + gridScale * i + puzzleRect.X), (float)(100 * scale + gridScale * i2 + puzzleRect.Y));
                    break;
                case Apex.Down:
                    points[0] = new PointF((float)(0 * scale + gridScale * i2 + puzzleRect.X), (float)(100 * scale + gridScale * i + puzzleRect.Y));
                    points[1] = new PointF((float)(40 * scale + gridScale * i2 + puzzleRect.X), (float)(99 * scale + gridScale * i + puzzleRect.Y));
                    points[2] = new PointF((float)(36 * scale + gridScale * i2 + puzzleRect.X), (float)(121 * scale + gridScale * i + puzzleRect.Y));
                    points[3] = new PointF((float)(50 * scale + gridScale * i2 + puzzleRect.X), (float)(133 * scale + gridScale * i + puzzleRect.Y));
                    points[4] = new PointF((float)(64 * scale + gridScale * i2 + puzzleRect.X), (float)(121 * scale + gridScale * i + puzzleRect.Y));
                    points[5] = new PointF((float)(60 * scale + gridScale * i2 + puzzleRect.X), (float)(99 * scale + gridScale * i + puzzleRect.Y));
                    points[6] = new PointF((float)(100 * scale + gridScale * i2 + puzzleRect.X), (float)(100 * scale + gridScale * i + puzzleRect.Y));
                    break;
                case Apex.Left:
                    points[0] = new PointF((float)(100 * scale + gridScale * i + puzzleRect.X), (float)(0 * scale + gridScale * i2 + puzzleRect.Y));
                    points[1] = new PointF((float)(101 * scale + gridScale * i + puzzleRect.X), (float)(40 * scale + gridScale * i2 + puzzleRect.Y));
                    points[2] = new PointF((float)(79 * scale + gridScale * i + puzzleRect.X), (float)(36 * scale + gridScale * i2 + puzzleRect.Y));
                    points[3] = new PointF((float)(67 * scale + gridScale * i + puzzleRect.X), (float)(50 * scale + gridScale * i2 + puzzleRect.Y));
                    points[4] = new PointF((float)(79 * scale + gridScale * i + puzzleRect.X), (float)(64 * scale + gridScale * i2 + puzzleRect.Y));
                    points[5] = new PointF((float)(101 * scale + gridScale * i + puzzleRect.X), (float)(60 * scale + gridScale * i2 + puzzleRect.Y));
                    points[6] = new PointF((float)(100 * scale + gridScale * i + puzzleRect.X), (float)(100 * scale + gridScale * i2 + puzzleRect.Y));
                    break;
                case Apex.Up:
                default:
                    points[0] = new PointF((float)(0 * scale + gridScale * i2 + puzzleRect.X), (float)(100 * scale + gridScale * i + puzzleRect.Y));
                    points[1] = new PointF((float)(40 * scale + gridScale * i2 + puzzleRect.X), (float)(101 * scale + gridScale * i + puzzleRect.Y));
                    points[2] = new PointF((float)(36 * scale + gridScale * i2 + puzzleRect.X), (float)(79 * scale + gridScale * i + puzzleRect.Y));
                    points[3] = new PointF((float)(50 * scale + gridScale * i2 + puzzleRect.X), (float)(67 * scale + gridScale * i + puzzleRect.Y));
                    points[4] = new PointF((float)(64 * scale + gridScale * i2 + puzzleRect.X), (float)(79 * scale + gridScale * i + puzzleRect.Y));
                    points[5] = new PointF((float)(60 * scale + gridScale * i2 + puzzleRect.X), (float)(101 * scale + gridScale * i + puzzleRect.Y));
                    points[6] = new PointF((float)(100 * scale + gridScale * i2 + puzzleRect.X), (float)(100 * scale + gridScale * i + puzzleRect.Y));
                    break;
            }

            return points;
        }

        void Render(Surface dst, Surface src, Rectangle rect)
        {
            ColorBgra currentPixel;

            for (int y = rect.Top; y < rect.Bottom; y++)
            {
                if (IsCancelRequested) return;
                for (int x = rect.Left; x < rect.Right; x++)
                {
                    if ((horLoops > 1 || verLoops > 1) && !puzzleRect.Contains(x, y))
                    {
                        currentPixel = src[x, y];
                        currentPixel.A = 0; // Delete pixels outside the puzzle border
                    }
                    else if (transparent)
                    {
                        currentPixel = src[x, y];
                        currentPixel.A = Int32Util.ClampToByte(byte.MaxValue - puzzleSurface[x, y].A);
                    }
                    else
                    {
                        currentPixel = normalOp.Apply(src[x, y], puzzleSurface[x, y]);
                    }

                    dst[x, y] = currentPixel;
                }
            }
        }

        protected override void OnDispose(bool disposing)
        {
            puzzleSurface?.Dispose();

            base.OnDispose(disposing);
        }
    }
}
