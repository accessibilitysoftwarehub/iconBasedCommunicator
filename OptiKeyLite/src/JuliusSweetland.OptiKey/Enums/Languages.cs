using System.Globalization;
using OptiKey.Properties;

namespace OptiKey.Enums
{
    public enum Languages
    {
	    DutchBelgium,
        DutchNetherlands,
        EnglishCanada,
        EnglishUK,
        EnglishUS,
	    FrenchFrance,
        GermanGermany,
        RussianRussia,
        SpanishSpain
    }

    public static partial class EnumExtensions
    {
        public static string ToDescription(this Languages languages)
        {
            switch (languages)
            {
                case Languages.DutchBelgium: return Resources.DUTCH_BELGIUM;
                case Languages.DutchNetherlands: return Resources.DUTCH_NETHERLANDS;
                case Languages.EnglishCanada: return Resources.ENGLISH_CANADA;
                case Languages.EnglishUK: return Resources.ENGLISH_UK;
                case Languages.EnglishUS: return Resources.ENGLISH_US;
                case Languages.FrenchFrance: return Resources.FRENCH_FRANCE;
                case Languages.GermanGermany: return Resources.GERMAN_GERMANY;
                case Languages.RussianRussia: return Resources.RUSSIAN_RUSSIA;
                case Languages.SpanishSpain: return Resources.SPANISH_SPAIN;
            }

            return languages.ToString();
        }

        public static CultureInfo ToCultureInfo(this Languages languages)
        {
            switch (languages)
            {
                case Languages.DutchBelgium: return CultureInfo.GetCultureInfo("nl-BE");
                case Languages.DutchNetherlands: return CultureInfo.GetCultureInfo("nl-NL");			
                case Languages.EnglishUS: return CultureInfo.GetCultureInfo("en-US");
                case Languages.EnglishUK: return CultureInfo.GetCultureInfo("en-GB");
                case Languages.EnglishCanada: return CultureInfo.GetCultureInfo("en-CA");
                case Languages.FrenchFrance: return CultureInfo.GetCultureInfo("fr-FR");
                case Languages.GermanGermany: return CultureInfo.GetCultureInfo("de-DE");
                case Languages.RussianRussia: return CultureInfo.GetCultureInfo("ru-RU");
                case Languages.SpanishSpain: return CultureInfo.GetCultureInfo("es-ES");
            }

            return CultureInfo.GetCultureInfo("en-GB");
        }
    }
}
