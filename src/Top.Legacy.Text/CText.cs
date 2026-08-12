using System.Globalization;

namespace Top.Legacy.Text
{
    public class CText
    {
        public static int Atoi(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            var i = 0;
            while (i < text.Length && (text[i] == ' ' || text[i] == '\t'))
            {
                i++;
            }

            var sign = 1;
            if (i < text.Length && (text[i] == '+' || text[i] == '-'))
            {
                sign = text[i] == '-' ? -1 : 1;
                i++;
            }

            var value = 0;
            while (i < text.Length && text[i] >= '0' && text[i] <= '9')
            {
                value = value * 10 + (text[i] - '0');
                i++;
            }

            return sign * value;
        }

        public static long AtoiLong(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0L;
            }

            var i = 0;
            while (i < text.Length && (text[i] == ' ' || text[i] == '\t'))
            {
                i++;
            }

            var sign = 1L;
            if (i < text.Length && (text[i] == '+' || text[i] == '-'))
            {
                sign = text[i] == '-' ? -1L : 1L;
                i++;
            }

            var value = 0L;
            while (i < text.Length && text[i] >= '0' && text[i] <= '9')
            {
                value = value * 10L + (text[i] - '0');
                i++;
            }

            return sign * value;
        }

        public static float Atof(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0f;
            }

            var i = 0;
            while (i < text.Length && (text[i] == ' ' || text[i] == '\t'))
            {
                i++;
            }

            var start = i;
            if (i < text.Length && (text[i] == '+' || text[i] == '-'))
            {
                i++;
            }

            while (i < text.Length && text[i] >= '0' && text[i] <= '9')
            {
                i++;
            }

            if (i < text.Length && text[i] == '.')
            {
                i++;
                while (i < text.Length && text[i] >= '0' && text[i] <= '9')
                {
                    i++;
                }
            }

            if (i < text.Length && (text[i] == 'e' || text[i] == 'E'))
            {
                var j = i + 1;

                if (j < text.Length && (text[j] == '+' || text[j] == '-'))
                {
                    j++;
                }

                if (j < text.Length && text[j] >= '0' && text[j] <= '9')
                {
                    i = j;
                    while (i < text.Length && text[i] >= '0' && text[i] <= '9')
                    {
                        i++;
                    }
                }
            }

            var span = text.Substring(start, i - start);

            return float.TryParse(span, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? value
                : 0f;
        }
    }
}
