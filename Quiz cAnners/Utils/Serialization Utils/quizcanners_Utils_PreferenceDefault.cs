using PlayerPrefs = UnityEngine.PlayerPrefs;

namespace QuizCanners.Utils
{
    public abstract class PreferenceDefaultGeneric<T>
    {
        protected string key;
        protected T defaultValue;
        protected T setValue;
        private bool _initialized;
        private bool _setByUser;

        protected abstract void SaveValue(T value);

        protected abstract T LoadValue();

        public void SetValue(T value)
        {
            _setByUser = true;
            setValue = value;
            SaveValue(value);
        }

        public T GetValue()
        {
            if (!_initialized)
            {
                _initialized = true;
                if (PlayerPrefs.HasKey(key))
                {
                    _setByUser = true;
                    setValue = LoadValue();
                }
            }
            return _setByUser ? setValue : defaultValue;
        }

        public PreferenceDefaultGeneric(string key, T defaultValue)
        {
            this.key = key;
            this.defaultValue = defaultValue;
        }
    }

    public class Preference_Int  : PreferenceDefaultGeneric<int>
    {
        protected override void SaveValue(int value) =>  PlayerPrefs.SetInt(key, value);
        protected override int LoadValue() => PlayerPrefs.GetInt(key, defaultValue: defaultValue);
        public Preference_Int(string key, int defaultValue) : base(key, defaultValue) { }
    }

    public class Preference_Float : PreferenceDefaultGeneric<float>
    {
        protected override void SaveValue(float value) => PlayerPrefs.SetFloat(key, value);
        protected override float LoadValue() => PlayerPrefs.GetFloat(key, defaultValue: defaultValue);
        public Preference_Float(string key, float defaultValue) : base(key, defaultValue) { }
    }

    public class Preference_String : PreferenceDefaultGeneric<string>
    {
        protected override void SaveValue(string value) => PlayerPrefs.SetString(key, value);
        protected override string LoadValue() => PlayerPrefs.GetString(key, defaultValue: defaultValue);
        public Preference_String(string key, string defaultValue) : base(key, defaultValue) { }
    }

    public class Preference_Bool : PreferenceDefaultGeneric<bool>
    {
        private bool From(int value) => value > 0;
        private int From(bool value) => value ? 1 : 0;

        protected override void SaveValue(bool value) => PlayerPrefs.SetInt(key, From(value));
        protected override bool LoadValue() => From(PlayerPrefs.GetInt(key, defaultValue: From(defaultValue)));
        public Preference_Bool(string key, bool defaultValue) : base(key, defaultValue) { }
    }
}
