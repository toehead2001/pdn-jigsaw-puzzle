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
        public string Author
        {
            get
            {
                return ((AssemblyCopyrightAttribute)base.GetType().Assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false)[0]).Copyright;
            }
        }
        public string Copyright
        {
            get
            {
                return ((AssemblyDescriptionAttribute)base.GetType().Assembly.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false)[0]).Description;
            }
        }

        public string DisplayName
        {
            get
            {
                return ((AssemblyProductAttribute)base.GetType().Assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false)[0]).Product;
            }
        }

        public Version Version
        {
            get
            {
                return base.GetType().Assembly.GetName().Version;
            }
        }

        public Uri WebsiteUri
        {
            get
            {
                return new Uri("http://www.getpaint.net/redirect/plugins.html");
            }
        }
    }

    [PluginSupportInfo(typeof(PluginSupportInfo), DisplayName = "Jigsaw Puzzle")]
    public class JigsawPuzzleEffectPlugin : PropertyBasedEffect
    {
        public static string StaticName
        {
            get
            {
                return "Jigsaw Puzzle";
            }
        }

        public static Image StaticIcon
        {
            get
            {
                return new Bitmap(typeof(JigsawPuzzleEffectPlugin), "JigsawPuzzle.png");
            }
        }

        public static string SubmenuName
        {
            get
            {
                return SubmenuNames.Render;
            }
        }

        public JigsawPuzzleEffectPlugin()
            : base(StaticName, StaticIcon, SubmenuName, EffectFlags.Configurable)
        {
        }

        public enum PropertyNames
        {
            Amount1,
            Amount2,
            Amount3,
            Amount4,
            Amount5,
            Amount6
        }

        public enum Amount3Options
        {
            Amount3Option1,
            Amount3Option2,
            Amount3Option3,
            Amount3Option4
        }


        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> props = new List<Property>();
            props.Add(new DoubleProperty(PropertyNames.Amount1, 1, 0.2, 5));
            props.Add(new Int32Property(PropertyNames.Amount2, 2, 1, 10));
            props.Add(StaticListChoiceProperty.CreateForEnum<Amount3Options>(PropertyNames.Amount3, 0, false));
            props.Add(new BooleanProperty(PropertyNames.Amount4, true));
            props.Add(new Int32Property(PropertyNames.Amount5, ColorBgra.ToOpaqueInt32(ColorBgra.FromBgra(EnvironmentParameters.PrimaryColor.B, EnvironmentParameters.PrimaryColor.G, EnvironmentParameters.PrimaryColor.R, 255)), 0, 0xffffff));
            props.Add(new DoubleVectorProperty(PropertyNames.Amount6, Pair.Create(0.0, 0.0), Pair.Create(-1.0, -1.0), Pair.Create(+1.0, +1.0)));

            List<PropertyCollectionRule> propRules = new List<PropertyCollectionRule>();
            propRules.Add(new ReadOnlyBoundToBooleanRule(PropertyNames.Amount5, PropertyNames.Amount4, false));

            return new PropertyCollection(props, propRules);
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            configUI.SetPropertyControlValue(PropertyNames.Amount1, ControlInfoPropertyNames.DisplayName, "Scale");
            configUI.SetPropertyControlValue(PropertyNames.Amount1, ControlInfoPropertyNames.SliderLargeChange, 0.25);
            configUI.SetPropertyControlValue(PropertyNames.Amount1, ControlInfoPropertyNames.SliderSmallChange, 0.05);
            configUI.SetPropertyControlValue(PropertyNames.Amount1, ControlInfoPropertyNames.UpDownIncrement, 0.001);
            configUI.SetPropertyControlValue(PropertyNames.Amount1, ControlInfoPropertyNames.DecimalPlaces, 3);
            configUI.SetPropertyControlValue(PropertyNames.Amount2, ControlInfoPropertyNames.DisplayName, "Line Width");
            configUI.SetPropertyControlValue(PropertyNames.Amount3, ControlInfoPropertyNames.DisplayName, "Pattern");
            PropertyControlInfo Amount3Control = configUI.FindControlForPropertyName(PropertyNames.Amount3);
            Amount3Control.SetValueDisplayName(Amount3Options.Amount3Option1, "Alternate Horizontal & Vertical");
            Amount3Control.SetValueDisplayName(Amount3Options.Amount3Option2, "Alternate Neither");
            Amount3Control.SetValueDisplayName(Amount3Options.Amount3Option3, "Alternate Horizontal");
            Amount3Control.SetValueDisplayName(Amount3Options.Amount3Option4, "Alternate Vertical");
            configUI.SetPropertyControlValue(PropertyNames.Amount4, ControlInfoPropertyNames.DisplayName, "Line Color");
            configUI.SetPropertyControlValue(PropertyNames.Amount4, ControlInfoPropertyNames.Description, "Transparent");
            configUI.SetPropertyControlValue(PropertyNames.Amount5, ControlInfoPropertyNames.DisplayName, string.Empty);
            configUI.SetPropertyControlType(PropertyNames.Amount5, PropertyControlType.ColorWheel);
            configUI.SetPropertyControlValue(PropertyNames.Amount6, ControlInfoPropertyNames.DisplayName, "Offset");
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

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            Amount1 = newToken.GetProperty<DoubleProperty>(PropertyNames.Amount1).Value;
            Amount2 = newToken.GetProperty<Int32Property>(PropertyNames.Amount2).Value;
            Amount3 = (byte)((int)newToken.GetProperty<StaticListChoiceProperty>(PropertyNames.Amount3).Value);
            Amount4 = newToken.GetProperty<BooleanProperty>(PropertyNames.Amount4).Value;
            Amount5 = ColorBgra.FromOpaqueInt32(newToken.GetProperty<Int32Property>(PropertyNames.Amount5).Value);
            Amount6 = newToken.GetProperty<DoubleVectorProperty>(PropertyNames.Amount6).Value;


            Rectangle selection = EnvironmentParameters.GetSelection(srcArgs.Surface.Bounds).GetBoundsInt();

            Bitmap puzzleBitmap = new Bitmap(selection.Width, selection.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Graphics puzzleGraphics = Graphics.FromImage(puzzleBitmap);
            puzzleGraphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            gridScale = 100 * Amount1;

            xOffset = (int)((selection.Width % gridScale) / 2 + (Amount6.First * (selection.Width % gridScale) / 2));
            yOffset = (int)((selection.Height % gridScale) / 2 + (Amount6.Second * (selection.Height % gridScale) / 2));

            int puzzleWidth = selection.Width - (int)(selection.Width % gridScale);
            int puzzleHeight = selection.Height - (int)(selection.Height % gridScale);

            horLoops = puzzleHeight / (int)(gridScale);
            verLoops = puzzleWidth / (int)(gridScale);

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

            Pen puzzlePen = new Pen(Amount5, Amount2);
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
                            curvePoints = getCurvePoints(0, i, i2); // upper apex
                        }
                        else
                        {
                            curvePoints = getCurvePoints(2, i, i2); // lower apex
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
                            curvePoints = getCurvePoints(0, i, i2); // upper apex
                        }
                        else
                        {
                            curvePoints = getCurvePoints(2, i, i2); // lower apex
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
                            curvePoints = getCurvePoints(1, i, i2); // right apex
                        }
                        else
                        {
                            curvePoints = getCurvePoints(3, i, i2); // left apex
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
                            curvePoints = getCurvePoints(1, i, i2); // right apex
                        }
                        else
                        {
                            curvePoints = getCurvePoints(3, i, i2); // left apex
                        }

                        puzzleGraphics.DrawCurve(puzzlePen, curvePoints);
                    }
                }
            }
            #endregion

            puzzlePen.Dispose();

            puzzleSurface = Surface.CopyFromBitmap(puzzleBitmap);
            puzzleBitmap.Dispose();


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
        PointF[] getCurvePoints(int apex, int i, int i2)
        {
            PointF point0, point1, point2, point3, point4, point5, point6;

            switch (apex)
            {
                case 0: // upper apex
                    point0 = new PointF((float)(0 * Amount1 + gridScale * i2 + xOffset), (float)(100 * Amount1 + gridScale * i + yOffset));
                    point1 = new PointF((float)(40 * Amount1 + gridScale * i2 + xOffset), (float)(101 * Amount1 + gridScale * i + yOffset));
                    point2 = new PointF((float)(36 * Amount1 + gridScale * i2 + xOffset), (float)(79 * Amount1 + gridScale * i + yOffset));
                    point3 = new PointF((float)(50 * Amount1 + gridScale * i2 + xOffset), (float)(67 * Amount1 + gridScale * i + yOffset));
                    point4 = new PointF((float)(64 * Amount1 + gridScale * i2 + xOffset), (float)(79 * Amount1 + gridScale * i + yOffset));
                    point5 = new PointF((float)(60 * Amount1 + gridScale * i2 + xOffset), (float)(101 * Amount1 + gridScale * i + yOffset));
                    point6 = new PointF((float)(100 * Amount1 + gridScale * i2 + xOffset), (float)(100 * Amount1 + gridScale * i + yOffset));
                    break;
                case 1: // right apex
                    point0 = new PointF((float)(100 * Amount1 + gridScale * i + xOffset), (float)(0 * Amount1 + gridScale * i2 + yOffset));
                    point1 = new PointF((float)(99 * Amount1 + gridScale * i + xOffset), (float)(40 * Amount1 + gridScale * i2 + yOffset));
                    point2 = new PointF((float)(121 * Amount1 + gridScale * i + xOffset), (float)(36 * Amount1 + gridScale * i2 + yOffset));
                    point3 = new PointF((float)(133 * Amount1 + gridScale * i + xOffset), (float)(50 * Amount1 + gridScale * i2 + yOffset));
                    point4 = new PointF((float)(121 * Amount1 + gridScale * i + xOffset), (float)(64 * Amount1 + gridScale * i2 + yOffset));
                    point5 = new PointF((float)(99 * Amount1 + gridScale * i + xOffset), (float)(60 * Amount1 + gridScale * i2 + yOffset));
                    point6 = new PointF((float)(100 * Amount1 + gridScale * i + xOffset), (float)(100 * Amount1 + gridScale * i2 + yOffset));
                    break;
                case 2: // lower apex
                    point0 = new PointF((float)(0 * Amount1 + gridScale * i2 + xOffset), (float)(100 * Amount1 + gridScale * i + yOffset));
                    point1 = new PointF((float)(40 * Amount1 + gridScale * i2 + xOffset), (float)(99 * Amount1 + gridScale * i + yOffset));
                    point2 = new PointF((float)(36 * Amount1 + gridScale * i2 + xOffset), (float)(121 * Amount1 + gridScale * i + yOffset));
                    point3 = new PointF((float)(50 * Amount1 + gridScale * i2 + xOffset), (float)(133 * Amount1 + gridScale * i + yOffset));
                    point4 = new PointF((float)(64 * Amount1 + gridScale * i2 + xOffset), (float)(121 * Amount1 + gridScale * i + yOffset));
                    point5 = new PointF((float)(60 * Amount1 + gridScale * i2 + xOffset), (float)(99 * Amount1 + gridScale * i + yOffset));
                    point6 = new PointF((float)(100 * Amount1 + gridScale * i2 + xOffset), (float)(100 * Amount1 + gridScale * i + yOffset));
                    break;
                case 3: // left apex
                    point0 = new PointF((float)(100 * Amount1 + gridScale * i + xOffset), (float)(0 * Amount1 + gridScale * i2 + yOffset));
                    point1 = new PointF((float)(101 * Amount1 + gridScale * i + xOffset), (float)(40 * Amount1 + gridScale * i2 + yOffset));
                    point2 = new PointF((float)(79 * Amount1 + gridScale * i + xOffset), (float)(36 * Amount1 + gridScale * i2 + yOffset));
                    point3 = new PointF((float)(67 * Amount1 + gridScale * i + xOffset), (float)(50 * Amount1 + gridScale * i2 + yOffset));
                    point4 = new PointF((float)(79 * Amount1 + gridScale * i + xOffset), (float)(64 * Amount1 + gridScale * i2 + yOffset));
                    point5 = new PointF((float)(101 * Amount1 + gridScale * i + xOffset), (float)(60 * Amount1 + gridScale * i2 + yOffset));
                    point6 = new PointF((float)(100 * Amount1 + gridScale * i + xOffset), (float)(100 * Amount1 + gridScale * i2 + yOffset));
                    break;
                default: // upper apex
                    point0 = new PointF((float)(0 * Amount1 + gridScale * i2 + xOffset), (float)(100 * Amount1 + gridScale * i + yOffset));
                    point1 = new PointF((float)(40 * Amount1 + gridScale * i2 + xOffset), (float)(101 * Amount1 + gridScale * i + yOffset));
                    point2 = new PointF((float)(36 * Amount1 + gridScale * i2 + xOffset), (float)(79 * Amount1 + gridScale * i + yOffset));
                    point3 = new PointF((float)(50 * Amount1 + gridScale * i2 + xOffset), (float)(67 * Amount1 + gridScale * i + yOffset));
                    point4 = new PointF((float)(64 * Amount1 + gridScale * i2 + xOffset), (float)(79 * Amount1 + gridScale * i + yOffset));
                    point5 = new PointF((float)(60 * Amount1 + gridScale * i2 + xOffset), (float)(101 * Amount1 + gridScale * i + yOffset));
                    point6 = new PointF((float)(100 * Amount1 + gridScale * i2 + xOffset), (float)(100 * Amount1 + gridScale * i + yOffset));
                    break;
            }

            PointF[] curvePoints = { point0, point1, point2, point3, point4, point5, point6 };
            return curvePoints;
        }

        #region CodeLab
        double Amount1 = 1; // [0.2,5] Scale
        int Amount2 = 2; // [1,10] Line Width
        byte Amount3 = 0; // Pattern|Pattern A|Pattern B|Pattern C|Pattern D
        bool Amount4 = true; // [0,1] Transparent
        ColorBgra Amount5 = ColorBgra.FromBgr(0, 0, 0); // Line Color
        Pair<double, double> Amount6 = Pair.Create(0.0, 0.0); // Offset
        #endregion

        readonly BinaryPixelOp normalOp = LayerBlendModeUtil.CreateCompositionOp(LayerBlendMode.Normal);
        Surface puzzleSurface;
        int horLoops, verLoops, xOffset, yOffset;
        double gridScale;

        void Render(Surface dst, Surface src, Rectangle rect)
        {
            Rectangle selection = EnvironmentParameters.GetSelection(src.Bounds).GetBoundsInt();

            ColorBgra sourcePixel, puzzlePixel, finalPixel;

            for (int y = rect.Top; y < rect.Bottom; y++)
            {
                if (IsCancelRequested) return;
                for (int x = rect.Left; x < rect.Right; x++)
                {
                    puzzlePixel = puzzleSurface.GetBilinearSample(x - selection.Left, y - selection.Top);
                    sourcePixel = src[x, y];

                    if (Amount4)
                    {
                        sourcePixel.A = Int32Util.ClampToByte(255 - puzzlePixel.A);
                        finalPixel = sourcePixel;
                    }
                    else
                    {
                        finalPixel = normalOp.Apply(sourcePixel, puzzlePixel);
                    }

                    // Delete pixels outside the puzzle border
                    if (horLoops > 1 || verLoops > 1)
                    {
                        if (xOffset != 0 && x - selection.Left < xOffset + 1 || x > selection.Right + xOffset - 1 - (selection.Width % gridScale))
                            finalPixel.A = 0;
                        if (yOffset != 0 && y - selection.Top < yOffset + 1 || y > selection.Bottom + yOffset - 1 - (selection.Height % gridScale))
                            finalPixel.A = 0;
                    }

                    dst[x, y] = finalPixel;
                }
            }
        }
    }
}
