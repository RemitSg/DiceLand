using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.SmartFormat;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>Resolves presentation text from the local String Tables. Game data keeps its source text and IDs.</summary>
public static class GameText
{
    public const string Chinese = "zh-Hans";
    public const string English = "en";
    private const string TableName = "DiceLand";

    private static AsyncOperationHandle<StringTable> table;
    private static int selection;
    public static string LanguageCode { get; private set; } = Chinese;
    public static bool IsChanging { get; private set; }
    public static event Action Changed;

    public static string Normalize(string code) => string.Equals(code, English, StringComparison.OrdinalIgnoreCase) ? English : Chinese;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        if (table.IsValid()) Addressables.Release(table);
        table = default;
        selection++;
        IsChanging = false;
        LanguageCode = Chinese;
        Changed = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RestoreSavedLanguage()
    {
        Select(MainMenuSettingsPersistence.Current.languageCode);
    }

    public static void Select(string code, Action<bool> completed = null)
    {
        code = Normalize(code);
        int request = ++selection;
        if (code == LanguageCode && table.IsValid())
        {
            IsChanging = false;
            completed?.Invoke(true);
            return;
        }

        IsChanging = code != LanguageCode;
        var initialization = LocalizationSettings.InitializationOperation;
        if (initialization.IsDone) LoadTable(initialization);
        else initialization.Completed += LoadTable;

        void LoadTable(AsyncOperationHandle<LocalizationSettings> operation)
        {
            if (request != selection) return;
            Locale locale = operation.Status == AsyncOperationStatus.Succeeded
                ? LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(code)) : null;
            if (locale == null) { Finish(null, default); return; }

            var loading = LocalizationSettings.StringDatabase.GetTableAsync(TableName, locale);
            if (loading.IsDone) ApplyTable(loading);
            else loading.Completed += ApplyTable;

            void ApplyTable(AsyncOperationHandle<StringTable> result)
            {
                Finish(locale, result);
            }
        }

        void Finish(Locale locale, AsyncOperationHandle<StringTable> loaded)
        {
            if (request != selection) return;
            IsChanging = false;
            bool success = loaded.IsValid() && loaded.Status == AsyncOperationStatus.Succeeded && loaded.Result != null;
            if (success)
            {
                if (table.IsValid()) Addressables.Release(table);
                // The database releases its cache when SelectedLocale changes.
                Addressables.ResourceManager.Acquire(loaded);
                table = loaded;
                LanguageCode = code;
                LocalizationSettings.SelectedLocale = locale;
            }
            else Debug.LogWarning($"[GameText] Could not load {TableName} for {code}.");
            Changed?.Invoke();
            completed?.Invoke(success);
        }
    }

    public static string Get(string key, string source)
    {
        string localized = !string.IsNullOrEmpty(key) && table.IsValid() ? table.Result.GetEntry(key)?.LocalizedValue : null;
        return string.IsNullOrEmpty(localized) ? source : localized;
    }

    public static string Content(string category, int id, string field, string source)
        => Content(category + "." + id.ToString(CultureInfo.InvariantCulture), field, source);

    public static string Content(string key, string field, string source)
    {
        // Chinese is the authored content, including runtime changes to its source fields.
        return LanguageCode == Chinese || string.IsNullOrEmpty(key) ? source : Get(key + "." + field, source);
    }

    public static string Format(string key, string source, params (string name, object value)[] arguments)
    {
        string template = Get(key, source);
        var values = new Dictionary<string, object>(arguments.Length, StringComparer.Ordinal);
        foreach (var argument in arguments)
        {
            object value = argument.value;
            if (value is string text && template.Contains("{" + argument.name + ":plural"))
                value = new DisplayCount(text);
            values.Add(argument.name, value);
        }
        return Smart.Default.Format(CultureInfo.GetCultureInfo(LanguageCode), template, values);
    }

    // Skill details pass colored quantities. Keep their markup for display while
    // giving SmartFormat a numeric value for the locale's plural rules.
    private sealed class DisplayCount : IConvertible, IFormattable
    {
        private readonly string text;
        private readonly decimal number;
        private IConvertible Numeric => number;

        public DisplayCount(string text)
        {
            this.text = text;
            string plain = Regex.Replace(text, "<[^>]+>", string.Empty);
            if (!decimal.TryParse(plain, NumberStyles.Number, CultureInfo.InvariantCulture, out number))
                number = 2; // Ranges and symbolic quantities use the non-singular form.
        }

        public override string ToString() => text;
        public string ToString(string format, IFormatProvider provider) => string.IsNullOrEmpty(format) ? text : number.ToString(format, provider);
        public TypeCode GetTypeCode() => TypeCode.Decimal;
        public bool ToBoolean(IFormatProvider provider) => Numeric.ToBoolean(provider);
        public byte ToByte(IFormatProvider provider) => Numeric.ToByte(provider);
        public char ToChar(IFormatProvider provider) => Numeric.ToChar(provider);
        public DateTime ToDateTime(IFormatProvider provider) => Numeric.ToDateTime(provider);
        public decimal ToDecimal(IFormatProvider provider) => number;
        public double ToDouble(IFormatProvider provider) => Numeric.ToDouble(provider);
        public short ToInt16(IFormatProvider provider) => Numeric.ToInt16(provider);
        public int ToInt32(IFormatProvider provider) => Numeric.ToInt32(provider);
        public long ToInt64(IFormatProvider provider) => Numeric.ToInt64(provider);
        public sbyte ToSByte(IFormatProvider provider) => Numeric.ToSByte(provider);
        public float ToSingle(IFormatProvider provider) => Numeric.ToSingle(provider);
        public string ToString(IFormatProvider provider) => text;
        public object ToType(Type type, IFormatProvider provider) => type == typeof(string) ? text : Numeric.ToType(type, provider);
        public ushort ToUInt16(IFormatProvider provider) => Numeric.ToUInt16(provider);
        public uint ToUInt32(IFormatProvider provider) => Numeric.ToUInt32(provider);
        public ulong ToUInt64(IFormatProvider provider) => Numeric.ToUInt64(provider);
    }
}
