using System.Text;

namespace SQLParser
{
    public static class StringBuilderExtantion
    {
        public static bool IsOnlyWhiteSpace(this StringBuilder builder)
        {
            for (var i = 0; i < builder.Length; i++)
            {
                if (!char.IsWhiteSpace(builder[i]))
                    return false;
            }
            return true;
        }
    }
}
