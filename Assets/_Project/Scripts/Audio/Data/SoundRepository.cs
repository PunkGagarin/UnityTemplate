using System;
using System.Collections.Generic;
using System.Text;
#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
#endif
using NaughtyAttributes;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _Project.Scripts.Audio.Data
{
    [CreateAssetMenu(fileName = "SoundRepository", menuName = "Game Resources/Audio/SoundRepository")]
    public class SoundRepository : ScriptableObject
    {
#if UNITY_EDITOR
        private static readonly Type SoundsEnumType = typeof(_Project.Scripts.Audio.Domain.Sounds);
#endif

        [SerializeField] private SoundsHolder _soundsHolder;
        [SerializeField] private SoundsHolder _musicHolder;
        [field: SerializeField] public List<SoundData> Sounds { get; private set; }
        [field: SerializeField] public List<SoundData> Music { get; private set; }
        
        public SoundElement GetClip(string clipName, SoundType soundType)
        {
            SoundElement clipToReturn = null;
            List<SoundData> soundsData = soundType == SoundType.Effect ? Sounds : Music;
            foreach (SoundData clipData in soundsData)
            {
                if (IsClipNameMatch(clipData.Name, clipName))
                {
                    clipToReturn = clipData.Clips[Random.Range(0, clipData.Clips.Count)];
                    break;
                }
            }

            if (clipToReturn == null)
            {
                Debug.LogError("Clip not found: " + clipName);  
                return null;
            }

            return clipToReturn;
        }

        [Button]
        private void GenerateSounds() =>
            UpdateSoundData(_soundsHolder, SoundType.Effect);

        [Button]
        private void GenerateMusic() =>
            UpdateSoundData(_musicHolder, SoundType.Music);

#if UNITY_EDITOR
        [Button]
        private void UpdateSoundsEnum()
        {
            List<string> soundNames = GetEnumSourceNames()
                .Select(CreateEnumMemberName)
                .ToList();

            if (soundNames.Count == 0)
            {
                Debug.LogError("Сначала надо сгенерировать или заполнить Sounds/Music в SoundRepository или holder-ах!");
                return;
            }

            List<string> duplicates = soundNames
                .GroupBy(name => name)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();

            if (duplicates.Count > 0)
            {
                Debug.LogError($"Нельзя обновить {SoundsEnumType.Name}: после нормализации есть дубли: {string.Join(", ", duplicates)}");
                return;
            }

            string enumPath = GetSoundsEnumPath();
            if (string.IsNullOrEmpty(enumPath))
                return;

            File.WriteAllText(enumPath, BuildSoundsEnumContent(soundNames), Encoding.UTF8);
            AssetDatabase.ImportAsset(enumPath);
            AssetDatabase.Refresh();
            Debug.Log($"{SoundsEnumType.Name} обновлен: {soundNames.Count} значений.");
        }
#endif
        
        private void UpdateSoundData(SoundsHolder holder, SoundType soundType)
        {
            if (holder ==null || holder.Sounds.Count == 0)
            {
                Debug.LogError("Сначала надо закинуть SoundsHolder и заполнить его треками!");
                return;
            }

            if (soundType == SoundType.Effect)
                Sounds = new List<SoundData>();
            else 
                Music = new List<SoundData>();
            
            foreach (AudioClip soundClip in holder.Sounds)
            {
                List<SoundElement> clip = new List<SoundElement>{ new SoundElement(soundClip) };
                SoundData clipData = new SoundData(soundClip.name, clip);
                
                if (soundType == SoundType.Effect)
                    Sounds.Add(clipData);
                else 
                    Music.Add(clipData);
            }
        }

        private static bool IsClipNameMatch(string repositoryName, string requestedName) =>
            repositoryName.Equals(requestedName, StringComparison.InvariantCultureIgnoreCase) ||
            CreateEnumMemberName(repositoryName).Equals(requestedName, StringComparison.InvariantCultureIgnoreCase);

        private static string CreateEnumMemberName(string soundName)
        {
            StringBuilder enumName = new StringBuilder();
            bool useUpperCase = false;

            foreach (char symbol in soundName.Trim())
            {
                if (char.IsLetterOrDigit(symbol) == false)
                {
                    useUpperCase = enumName.Length > 0;
                    continue;
                }

                if (enumName.Length == 0 && char.IsDigit(symbol))
                    enumName.Append('_');

                enumName.Append(useUpperCase ? char.ToUpperInvariant(symbol) : symbol);
                useUpperCase = false;
            }

            return enumName.Length == 0 ? "SoundValue" : enumName.ToString();
        }

#if UNITY_EDITOR
        private IEnumerable<string> GetEnumSourceNames()
        {
            List<string> holderNames = GetHolderSoundNames(_soundsHolder)
                .Concat(GetHolderSoundNames(_musicHolder))
                .ToList();

            if (holderNames.Count > 0)
                return holderNames;

            return GetSoundDataNames(Sounds).Concat(GetSoundDataNames(Music));
        }

        private static IEnumerable<string> GetHolderSoundNames(SoundsHolder holder) =>
            holder == null || holder.Sounds == null
                ? Enumerable.Empty<string>()
                : holder.Sounds
                    .Where(sound => sound != null && string.IsNullOrWhiteSpace(sound.name) == false)
                    .Select(sound => sound.name);

        private static IEnumerable<string> GetSoundDataNames(IEnumerable<SoundData> sounds) =>
            sounds == null
                ? Enumerable.Empty<string>()
                : sounds
                    .Where(sound => sound != null && string.IsNullOrWhiteSpace(sound.Name) == false)
                    .Select(sound => sound.Name);

        private static string BuildSoundsEnumContent(IReadOnlyList<string> soundNames)
        {
            Dictionary<string, int> existingValues = Enum.GetValues(SoundsEnumType)
                .Cast<Enum>()
                .ToDictionary(value => value.ToString(), value => Convert.ToInt32(value));
            int nextValue = existingValues.Count == 0 ? 1 : existingValues.Values.Max() + 1;
            StringBuilder content = new StringBuilder();
            content.AppendLine($"namespace {SoundsEnumType.Namespace}");
            content.AppendLine("{");
            content.AppendLine($"    public enum {SoundsEnumType.Name}");
            content.AppendLine("    {");

            foreach (string soundName in soundNames)
            {
                if (existingValues.TryGetValue(soundName, out int existingValue) == false)
                {
                    existingValue = nextValue;
                    nextValue++;
                }

                content.AppendLine($"        {soundName} = {existingValue},");
            }

            content.AppendLine("    }");
            content.AppendLine("}");

            return content.ToString();
        }

        private static string GetSoundsEnumPath()
        {
            List<string> enumPaths = AssetDatabase
                .FindAssets($"{SoundsEnumType.Name} t:MonoScript")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => Path.GetFileNameWithoutExtension(path) == SoundsEnumType.Name)
                .ToList();

            if (enumPaths.Count == 1)
                return enumPaths[0];

            Debug.LogError($"Нельзя найти единственный файл {SoundsEnumType.Name}.cs для обновления enum.");
            return null;
        }
#endif
    }
}
