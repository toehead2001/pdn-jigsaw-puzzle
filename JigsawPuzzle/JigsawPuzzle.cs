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
        public string Author => ((AssemblyCopyrightAttribute)base.GetType().Assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false)[0]).Copyright;
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
            Amount1,
            Amount2,
            Amount3,
            Amount4,
            Amount5,
            Amount6
        }

        private enum Amount3Options
        {
            Amount3Option1,
            Amount3Option2,
            Amount3Option3,
            Amount3Option4
        }


        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> props = new List<Property>
            {
                new DoubleProperty(PropertyNames.Amount1, 1, 0.2, 10),
                new Int32Property(PropertyNames.Amount2, 2, 1, 10),
                StaticListChoiceProperty.CreateForEnum<Amount3Options>(PropertyNames.Amount3, 0, false),
                new BooleanProperty(PropertyNames.Amount4, true),
                new Int32Property(PropertyNames.Amount5, ColorBgra.ToOpaqueInt32(ColorBgra.FromBgra(EnvironmentParameters.PrimaryColor.B, EnvironmentParameters.PrimaryColor.G, EnvironmentParameters.PrimaryColor.R, 255)), 0, 0xffffff),
                new DoubleVectorProperty(PropertyNames.Amount6, Pair.Create(0.0, 0.0), Pair.Create(-1.0, -1.0), Pair.Create(+1.0, +1.0))
            };

            List<PropertyCollectionRule> propRules = new List<PropertyCollectionRule>
            {
                new ReadOnlyBoundToBooleanRule(PropertyNames.Amount5, PropertyNames.Amount4, false)
            };

            return new PropertyCollection(props, propRules);
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            configUI.SetPropertyControlValue(PropertyNames.Amount1, ControlInfoPropertyNames.DisplayName, L10nStrings.Scale);
            configUI.SetPropertyControlValue(PropertyNames.Amount1, ControlInfoPropertyNames.SliderLargeChange, 0.25);
            configUI.SetPropertyControlValue(PropertyNames.Amount1, ControlInfoPropertyNames.SliderSmallChange, 0.05);
            configUI.SetPropertyControlValue(PropertyNames.Amount1, ControlInfoPropertyNames.UpDownIncrement, 0.001);
            configUI.SetPropertyControlValue(PropertyNames.Amount1, ControlInfoPropertyNames.DecimalPlaces, 3);
            configUI.SetPropertyControlValue(PropertyNames.Amount2, ControlInfoPropertyNames.DisplayName, L10nStrings.LineWidth);
            configUI.SetPropertyControlValue(PropertyNames.Amount3, ControlInfoPropertyNames.DisplayName, L10nStrings.Pattern);
            PropertyControlInfo Amount3Control = configUI.FindControlForPropertyName(PropertyNames.Amount3);
            Amount3Control.SetValueDisplayName(Amount3Options.Amount3Option1, L10nStrings.AltHorVer);
            Amount3Control.SetValueDisplayName(Amount3Options.Amount3Option2, L10nStrings.AltNone);
            Amount3Control.SetValueDisplayName(Amount3Options.Amount3Option3, L10nStrings.AltHor);
            Amount3Control.SetValueDisplayName(Amount3Options.Amount3Option4, L10nStrings.AltVer);
            configUI.SetPropertyControlValue(PropertyNames.Amount4, ControlInfoPropertyNames.DisplayName, L10nStrings.LineColor);
            configUI.SetPropertyControlValue(PropertyNames.Amount4, ControlInfoPropertyNames.Description, L10nStrings.Transparent);
            configUI.SetPropertyControlValue(PropertyNames.Amount5, ControlInfoPropertyNames.DisplayName, string.Empty);
            configUI.SetPropertyControlType(PropertyNames.Amount5, PropertyControlType.ColorWheel);
            configUI.SetPropertyControlValue(PropertyNames.Amount6, ControlInfoPropertyNames.DisplayName, L10nStrings.Position);
            configUI.SetPropertyControlValue(PropertyNames.Amount6, ControlInfoPropertyNames.SliderSmallChangeX, 0.05);
            configUI.SetPropertyControlValue(PropertyNames.Amount6, ControlInfoPropertyNames.SliderLargeChangeX, 0.25);
            configUI.SetPropertyControlValue(PropertyNames.Amount6, ControlInfoPropertyNames.UpDownIncrementX, 0.001);
            configUI.SetPropertyControlValue(PropertyNames.Amount6, ControlInfoPropertyNames.SliderSmallChangeY, 0.05);
            configUI.SetPropertyControlValue(PropertyNames.Amount6, ControlInfoPropertyNames.SliderLargeChangeY, 0.25);
            configUI.SetPropertyControlValue(PropertyNames.Amount6, ControlInfoPropertyNames.UpDownIncrementY, 0.001);
            configUI.SetPropertyControlValue(PropertyNames.Amount6, ControlInfoPropertyNames.DecimalPlaces, 3);
            Rectangle selection6 = EnvironmentParameters.GetSelection(EnvironmentParameters.SourceSurface.Bounds).GetBoundsInt();
            ImageResource imageResource6 = ImageResource.FromImage(EnvironmentParameters.SourceSurface.CreateAliasedBitmap(selection6));
            configUI.SetPropertyControlValue(PropertyNames.Amount6, ControlInfoPropertyNames.StaticImageUnderlay, imageResource6);

            return configUI;
        }

        double Amount1 = 1; // [0.2,5] Scale
        int Amount2 = 2; // [1,10] Line Width
        byte Amount3 = 0; // Pattern|Pattern A|Pattern B|Pattern C|Pattern D
        bool Amount4 = true; // [0,1] Transparent
        ColorBgra Amount5 = ColorBgra.FromBgr(0, 0, 0); // Line Color
        Pair<double, double> Amount6 = Pair.Create(0.0, 0.0); // Offset

        readonly BinaryPixelOp normalOp = LayerBlendModeUtil.CreateCompositionOp(LayerBlendMode.Normal);
        Surface puzzleSurface;
        int horLoops, verLoops;
        double gridScale;
        Rectangle puzzleRect;

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            Amount1 = newToken.GetProperty<DoubleProperty>(PropertyNames.Amount1).Value;
            Amount2 = newToken.GetProperty<Int32Property>(PropertyNames.Amount2).Value;
            Amount3 = (byte)((int)newToken.GetProperty<StaticListChoiceProperty>(PropertyNames.Amount3).Value);
            Amount4 = newToken.GetProperty<BooleanProperty>(PropertyNames.Amount4).Value;
            Amount5 = ColorBgra.FromOpaqueInt32(newToken.GetProperty<Int32Property>(PropertyNames.Amount5).Value);
            Amount6 = newToken.GetProperty<DoubleVectorProperty>(PropertyNames.Amount6).Value;


            Rectangle selection = EnvironmentParameters.GetSelection(srcArgs.Surface.Bounds).GetBoundsInt();

            gridScale = 100 * Amount1;

            puzzleRect = new Rectangle
            {
                X = selection.Left + (int)Math.Round((selection.Width % gridScale) / 2 + (Amount6.First * (selection.Width % gridScale) / 2)),
                Y = selection.Top + (int)Math.Round((selection.Height % gridScale) / 2 + (Amount6.Second * (selection.Height % gridScale) / 2)),
                Width = selection.Width - (int)(selection.Width % gridScale),
                Height = selection.Height - (int)(selection.Height % gridScale)
            };

            horLoops = puzzleRect.Height / (int)(gridScale);
            verLoops = puzzleRect.Width / (int)(gridScale);

            // Puzzle Pattern
            bool horAlt, verAlt;
            switch (Amount3)
            {
                case 0:
                    horAlt = true;
                    verAlt = true;
                    break;
                case 1:
                    horAlt = false;
                    verAlt = false;
                    break;
                case 2:
                    horAlt = true;
                    verAlt = false;
                    break;
                case 3:
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
            using (Pen puzzlePen = new Pen(Amount5, Amount2))
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
                    points[0] = new PointF((float)(100 * Amount1 + gridScale * i + puzzleRect.X), (float)(0 * Amount1 + gridScale * i2 + puzzleRect.Y));
                    points[1] = new PointF((float)(99 * Amount1 + gridScale * i + puzzleRect.X), (float)(40 * Amount1 + gridScale * i2 + puzzleRect.Y));
                    points[2] = new PointF((float)(121 * Amount1 + gridScale * i + puzzleRect.X), (float)(36 * Amount1 + gridScale * i2 + puzzleRect.Y));
                    points[3] = new PointF((float)(133 * Amount1 + gridScale * i + puzzleRect.X), (float)(50 * Amount1 + gridScale * i2 + puzzleRect.Y));
                    points[4] = new PointF((float)(121 * Amount1 + gridScale * i + puzzleRect.X), (float)(64 * Amount1 + gridScale * i2 + puzzleRect.Y));
                    points[5] = new PointF((float)(99 * Amount1 + gridScale * i + puzzleRect.X), (float)(60 * Amount1 + gridScale * i2 + puzzleRect.Y));
                    points[6] = new PointF((float)(100 * Amount1 + gridScale * i + puzzleRect.X), (float)(100 * Amount1 + gridScale * i2 + puzzleRect.Y));
                    break;
                case Apex.Down:
                    points[0] = new PointF((float)(0 * Amount1 + gridScale * i2 + puzzleRect.X), (float)(100 * Amount1 + gridScale * i + puzzleRect.Y));
                    points[1] = new PointF((float)(40 * Amount1 + gridScale * i2 + puzzleRect.X), (float)(99 * Amount1 + gridScale * i + puzzleRect.Y));
                    points[2] = new PointF((float)(36 * Amount1 + gridScale * i2 + puzzleRect.X), (float)(121 * Amount1 + gridScale * i + puzzleRect.Y));
                    points[3] = new PointF((float)(50 * Amount1 + gridScale * i2 + puzzleRect.X), (float)(133 * Amount1 + gridScale * i + puzzleRect.Y));
                    points[4] = new PointF((float)(64 * Amount1 + gridScale * i2 + puzzleRect.X), (float)(121 * Amount1 + gridScale * i + puzzleRect.Y));
                    points[5] = new PointF((float)(60 * Amount1 + gridScale * i2 + puzzleRect.X), (float)(99 * Amount1 + gridScale * i + puzzleRect.Y));
                    points[6] = new PointF((float)(100 * Amount1 + gridScale * i2 + puzzleRect.X), (float)(100 * Amount1 + gridScale * i + puzzleRect.Y));
                    break;
                case Apex.Left:
                    points[0] = new PointF((float)(100 * Amount1 + gridScale * i + puzzleRect.X), (float)(0 * Amount1 + gridScale * i2 + puzzleRect.Y));
                    points[1] = new PointF((float)(101 * Amount1 + gridScale * i + puzzleRect.X), (float)(40 * Amount1 + gridScale * i2 + puzzleRect.Y));
                    points[2] = new PointF((float)(79 * Amount1 + gridScale * i + puzzleRect.X), (float)(36 * Amount1 + gridScale * i2 + puzzleRect.Y));
                    points[3] = new PointF((float)(67 * Amount1 + gridScale * i + puzzleRect.X), (float)(50 * Amount1 + gridScale * i2 + puzzleRect.Y));
                    points[4] = new PointF((float)(79 * Amount1 + gridScale * i + puzzleRect.X), (float)(64 * Amount1 + gridScale * i2 + puzzleRect.Y));
                    points[5] = new PointF((float)(101 * Amount1 + gridScale * i + puzzleRect.X), (float)(60 * Amount1 + gridScale * i2 + puzzleRect.Y));
                    points[6] = new PointF((float)(100 * Amount1 + gridScale * i + puzzleRect.X), (float)(100 * Amount1 + gridScale * i2 + puzzleRect.Y));
                    break;
                case Apex.Up:
                default:
                    points[0] = new PointF((float)(0 * Amount1 + gridScale * i2 + puzzleRect.X), (float)(100 * Amount1 + gridScale * i + puzzleRect.Y));
                    points[1] = new PointF((float)(40 * Amount1 + gridScale * i2 + puzzleRect.X), (float)(101 * Amount1 + gridScale * i + puzzleRect.Y));
                    points[2] = new PointF((float)(36 * Amount1 + gridScale * i2 + puzzleRect.X), (float)(79 * Amount1 + gridScale * i + puzzleRect.Y));
                    points[3] = new PointF((float)(50 * Amount1 + gridScale * i2 + puzzleRect.X), (float)(67 * Amount1 + gridScale * i + puzzleRect.Y));
                    points[4] = new PointF((float)(64 * Amount1 + gridScale * i2 + puzzleRect.X), (float)(79 * Amount1 + gridScale * i + puzzleRect.Y));
                    points[5] = new PointF((float)(60 * Amount1 + gridScale * i2 + puzzleRect.X), (float)(101 * Amount1 + gridScale * i + puzzleRect.Y));
                    points[6] = new PointF((float)(100 * Amount1 + gridScale * i2 + puzzleRect.X), (float)(100 * Amount1 + gridScale * i + puzzleRect.Y));
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
                    if ((horLoops > 1 || verLoops > 1) && (x <= puzzleRect.Left || x >= puzzleRect.Right || y <= puzzleRect.Top || y >= puzzleRect.Bottom))
                    {
                        currentPixel = src[x, y];
                        currentPixel.A = 0; // Delete pixels outside the puzzle border
                    }
                    else if (Amount4)
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
    }
}
