using System.Globalization;

namespace JigsawPuzzleEffect
{
    internal static class L10nStrings
    {
        private static readonly string UICulture = CultureInfo.CurrentUICulture.Name;

        internal static string EffectName
        {
            get
            {
                switch (UICulture)
                {
                    case "ru":
                        return "Пазл";
                    default:
                        return "Jigsaw Puzzle";
                }
            }
        }

        internal static string EffectDescription
        {
            get
            {
                switch (UICulture)
                {
                    case "ru":
                        return "Создание узора пазла";
                    default:
                        return "Generates a jigsaw puzzle";
                }
            }
        }

        internal static string EffectKeywords
        {
            get
            {
                switch (UICulture)
                {
                    default:
                        return "jigsaw|puzzle";
                }
            }
        }

        internal static string Scale
        {
            get
            {
                switch (UICulture)
                {
                    case "ru":
                        return "Масштаб";
                    default:
                        return "Scale";
                }
            }
        }

        internal static string LineWidth
        {
            get
            {
                switch (UICulture)
                {
                    case "ru":
                        return "Ширина линии";
                    default:
                        return "Line Width";
                }
            }
        }

        internal static string Pattern
        {
            get
            {
                switch (UICulture)
                {
                    case "ru":
                        return "Узор";
                    default:
                        return "Pattern";
                }
            }
        }

        internal static string AltHorVer
        {
            get
            {
                switch (UICulture)
                {
                    case "ru":
                        return "Ключ-замок-ключ-замок";
                    default:
                        return "Alternate Horizontal & Vertical";
                }
            }
        }

        internal static string AltNone
        {
            get
            {
                switch (UICulture)
                {
                    case "ru":
                        return "Ключ-ключ-замок-замок";
                    default:
                        return "Alternate Neither";
                }
            }
        }

        internal static string AltHor
        {
            get
            {
                switch (UICulture)
                {
                    case "ru":
                        return "Ключ-ключ-ключ-замок";
                    default:
                        return "Alternate Horizontal";
                }
            }
        }

        internal static string AltVer
        {
            get
            {
                switch (UICulture)
                {
                    case "ru":
                        return "Ключ-замок-замок-замок";
                    default:
                        return "Alternate Vertical";
                }
            }
        }

        internal static string LineColor
        {
            get
            {
                switch (UICulture)
                {
                    case "ru":
                        return "Цвет линии";
                    default:
                        return "Line Color";
                }
            }
        }

        internal static string Transparent
        {
            get
            {
                switch (UICulture)
                {
                    case "ru":
                        return "Прозрачность";
                    default:
                        return "Transparent";
                }
            }
        }

        internal static string Position
        {
            get
            {
                switch (UICulture)
                {
                    case "ru":
                        return "Позиция";
                    default:
                        return "Position";
                }
            }
        }
    }
}
