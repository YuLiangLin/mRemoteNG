using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using mRemoteNG.Resources.Language;

namespace mRemoteNG.Themes
{
    /// <summary>
    /// Converts stable palette keys into readable labels for the current UI language.
    /// Unknown languages intentionally fall back to English instead of leaking labels
    /// from an unrelated translation.
    /// </summary>
    internal static class ThemeColorLabelProvider
    {
        private static readonly IReadOnlyDictionary<string, string> TraditionalChineseTokens =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Active"] = "使用中",
                ["Background"] = "背景",
                ["Backgorund"] = "背景",
                ["Border"] = "邊框",
                ["Button"] = "按鈕",
                ["CheckBox"] = "核取方塊",
                ["ComboBox"] = "下拉選單",
                ["CommandBarMenuDefault"] = "選單",
                ["Critical"] = "嚴重",
                ["Dialog"] = "對話框",
                ["Disabled"] = "停用",
                ["ErrorText"] = "錯誤訊息",
                ["Fill"] = "填滿",
                ["Focused"] = "聚焦",
                ["Foreground"] = "文字",
                ["Glyph"] = "勾選圖示",
                ["GroupBox"] = "群組框",
                ["Header"] = "標題列",
                ["Hover"] = "滑入",
                ["Inactive"] = "非使用中",
                ["Item"] = "項目",
                ["Line"] = "分隔線",
                ["List"] = "清單",
                ["MouseOver"] = "滑入",
                ["PopUp"] = "彈出選單",
                ["Pressed"] = "按下",
                ["ProgressBar"] = "進度列",
                ["Selected"] = "選取",
                ["SelectedItem"] = "選取項目",
                ["Tab"] = "頁籤",
                ["Text"] = "文字",
                ["TextBox"] = "輸入欄",
                ["TreeView"] = "連線樹",
                ["Treeview"] = "連線樹",
                ["Warning"] = "警告",
                ["WarningText"] = "警告訊息"
            };

        internal static string GetDisplayName(string key)
        {
            CultureInfo culture = Language.Culture ?? CultureInfo.CurrentUICulture;
            return GetDisplayName(key, culture);
        }

        internal static string GetDisplayName(string key, CultureInfo culture)
        {
            if (string.IsNullOrWhiteSpace(key))
                return string.Empty;

            bool useTraditionalChinese = culture != null &&
                                         (culture.Name.Equals("zh-TW", StringComparison.OrdinalIgnoreCase) ||
                                          culture.Name.Equals("zh-HK", StringComparison.OrdinalIgnoreCase) ||
                                          culture.Name.Equals("zh-MO", StringComparison.OrdinalIgnoreCase) ||
                                          culture.Name.StartsWith("zh-Hant", StringComparison.OrdinalIgnoreCase));

            return string.Join(" · ", key.Split('_').Select(token =>
                useTraditionalChinese && TraditionalChineseTokens.TryGetValue(token, out string translation)
                    ? translation
                    : HumanizeEnglishToken(token)));
        }

        private static string HumanizeEnglishToken(string token)
        {
            token = token.Equals("Backgorund", StringComparison.OrdinalIgnoreCase)
                ? "Background"
                : token;

            StringBuilder label = new();
            for (int index = 0; index < token.Length; index++)
            {
                char current = token[index];
                if (index > 0 && char.IsUpper(current) &&
                    (char.IsLower(token[index - 1]) ||
                     (index + 1 < token.Length && char.IsLower(token[index + 1]))))
                {
                    label.Append(' ');
                }

                label.Append(current);
            }

            return label.ToString();
        }
    }
}
