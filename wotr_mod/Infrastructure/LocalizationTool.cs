using System.IO;
using System.Xml;
using Kingmaker.Localization;

namespace wotr_mod.Infrastructure
{
    internal sealed class LocalizationTool
    {
        public void Put(string key, string value)
        {
            if (LocalizationManager.CurrentPack == null)
            {
                return;
            }

            LocalizationManager.CurrentPack.PutString(key, value);
        }

        public void PutSoundEvent(string key, string akEvent)
        {
            if (LocalizationManager.SoundPack == null)
            {
                return;
            }

            LocalizationManager.SoundPack.PutString(key, akEvent);
        }

        public LocalizedString Text(string key)
        {
            return new LocalizedString { Key = key };
        }

        public int PutResx(string path)
        {
            if (LocalizationManager.CurrentPack == null || !File.Exists(path))
            {
                return 0;
            }

            var document = new XmlDocument();
            document.Load(path);

            var count = 0;
            foreach (XmlNode node in document.SelectNodes("/root/data"))
            {
                var key = node.Attributes?["name"]?.Value;
                var value = node.SelectSingleNode("value")?.InnerText;
                if (string.IsNullOrEmpty(key) || value == null)
                {
                    continue;
                }

                Put(key, value);
                count++;
            }

            return count;
        }
    }
}
