/*
 * OptionsController.cs - by ThunderWire Studio
 * Version 1.0
*/

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing;
using ThunderWire.CrossPlatform.Input;
using UnityEditor;

namespace ThunderWire.Game.Options
{
    /// <summary>
    /// Main Controller for Options
    /// </summary>
    public class OptionsController : Singleton<OptionsController>
    {
        public struct ScreenResolution : IEquatable<ScreenResolution>
        {
            public int width;
            public int height;
            public int refreshRate;

            public ScreenResolution(int w, int h, int rate)
            {
                width = w;
                height = h;
                refreshRate = rate;
            }

            public bool Equals(ScreenResolution other)
            {
                return width == other.width && height == other.height;
            }
        }

        public const string OPTIONS_PC_PREFIX = "settings_pc";
        public const string OPTIONS_CONSOLE_PREFIX = "settings_console";

        private CrossPlatformInput crossPlatformInput;
        private JsonHandler jsonHandler;
        private Device device = Device.Keyboard;

        public enum OptionType { Custom, Volume, PostBloom, PostGrain, PostMotionBlur, PostAmbient, Resolution, Fullscreen, Antialiasing, TextureQuality, ShadowResolution, ShadowDistance, VSync }

        [Header("Main")]
        public List<OptionSection> GameOptionsPC = new List<OptionSection>();
        public List<OptionSection> GameOptionsConsole = new List<OptionSection>();

        [Header("Other")]
        public PostProcessVolume postProcessing;
        public Dropdown ResolutionDrop;
        public DropdownLink GameQuality;
        public bool showRefreshRate;

        List<ScreenResolution> Resolutions = new List<ScreenResolution>();

        private int resolutionIndex = -1;
        private int fullScreenMode = -1;

        private int antialising;
        private int texq;
        private int shadowres;

        void Awake()
        {
            if (!postProcessing)
            {
                ScriptManager scriptManager;

                if ((scriptManager = ScriptManager.Instance) != null)
                {
                    postProcessing = scriptManager.MainPostProcess;
                }
            }

            crossPlatformInput = CrossPlatformInput.Instance;
        }

        void Start()
        {
            jsonHandler = GetComponent<JsonHandler>();
            crossPlatformInput.OnInputsInitialized += OnInputsInitialized;
        }

        async void OnInputsInitialized(Device device)
        {
            this.device = device;

            if (device == Device.Keyboard)
            {
                if (showRefreshRate)
                {
                    Resolutions = Screen.resolutions.Select(x => new ScreenResolution(x.width, x.height, x.refreshRate)).Reverse().ToList();
                }
                else
                {
                    Resolutions = Screen.resolutions.Select(x => new ScreenResolution(x.width, x.height, 0)).Distinct().Reverse().ToList();
                }

                if (ResolutionDrop)
                {
                    ResolutionDrop.ClearOptions();

                    if (showRefreshRate)
                    {
                        ResolutionDrop.options.AddRange(Resolutions.Select(x => new Dropdown.OptionData(x.width + "x" + x.height + "@" + x.refreshRate)).ToArray());
                    }
                    else
                    {
                        ResolutionDrop.options.AddRange(Resolutions.Select(x => new Dropdown.OptionData(x.width + "x" + x.height)).ToArray());
                    }

                    ResolutionDrop.RefreshShownValue();
                }
            }

            if (jsonHandler.FileExist())
            {
                await Task.Run(() => jsonHandler.DeserializeDataAsync());

                if (jsonHandler.Json().HasValues)
                {
                    string rootKey = jsonHandler.Json().ToObject<Dictionary<string, object>>().Keys.FirstOrDefault();

                    if (device == Device.Keyboard && rootKey.Equals(OPTIONS_PC_PREFIX) || device == Device.Gamepad && rootKey.Equals(OPTIONS_CONSOLE_PREFIX))
                    {
                        UpdateOptions(true, true);
                    }
                    else
                    {
                        jsonHandler.DeleteFile();
                        UpdateOptions(false, false);
                    }
                }
                else
                {
                    UpdateOptions(false, false);
                }
            }
            else
            {
                UpdateOptions(false, false);
            }
        }

        public void OnOptionChanged(OptionBehaviour option)
        {
            if (device == Device.Keyboard)
            {
                foreach (var section in GameOptionsPC)
                {
                    foreach (var entry in section.OptionObjects)
                    {
                        if (entry.Instance == option)
                        {
                            entry.IsChanged = true;
                            break;
                        }
                    }
                }
            }
            else
            {
                foreach (var section in GameOptionsConsole)
                {
                    foreach (var entry in section.OptionObjects)
                    {
                        if (entry.Instance == option)
                        {
                            entry.IsChanged = true;
                            break;
                        }
                    }
                }
            }
        }

        public void ApplyOptions(bool isPC)
        {
            Hashtable root = new Hashtable();

            if (isPC)
            {
                foreach (var option in GameOptionsPC)
                {
                    Hashtable sectionData = new Hashtable();

                    foreach (var entry in option.OptionObjects)
                    {
                        string value = entry.Instance.GetValue().ToString();

                        if (entry.IsChanged)
                        {
                            ApplyOptionRealtime(entry.Option, value);
                        }

                        entry.IsChanged = false;
                        sectionData.Add(entry.Prefix.ToLower(), value);
                    }

                    root.Add(option.Section.ToLower(), sectionData);
                }

                ApplyResolution(resolutionIndex, fullScreenMode);
                resolutionIndex = -1;
                fullScreenMode = -1;
            }
            else
            {
                foreach (var option in GameOptionsConsole)
                {
                    Hashtable sectionData = new Hashtable();

                    foreach (var entry in option.OptionObjects)
                    {
                        string value = entry.Instance.GetValue().ToString();

                        if (entry.IsChanged)
                        {
                            ApplyOptionRealtime(entry.Option, value);
                        }

                        entry.IsChanged = false;
                        sectionData.Add(entry.Prefix.ToLower(), value);
                    }

                    root.Add(option.Section.ToLower(), sectionData);
                }
            }

            if (root.Values.Count > 0)
            {
                jsonHandler.Add(isPC ? OPTIONS_PC_PREFIX : OPTIONS_CONSOLE_PREFIX, root);
                jsonHandler.SerializeJsonData();
                jsonHandler.ClearArray();
            }
        }

        void ApplyOptionRealtime(OptionType option, string value)
        {
            if (option == OptionType.Volume)
            {
                AudioListener.volume = float.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
            }
            else if (option == OptionType.PostBloom && postProcessing)
            {
                bool val = bool.Parse(value);
                postProcessing.sharedProfile.GetSetting<Bloom>().enabled.Override(val);
                postProcessing.profile.GetSetting<Bloom>().enabled.Override(val);
            }
            else if (option == OptionType.PostGrain && postProcessing)
            {
                bool val = bool.Parse(value);
                postProcessing.sharedProfile.GetSetting<Grain>().enabled.Override(val);
                postProcessing.profile.GetSetting<Grain>().enabled.Override(val);
            }
            else if (option == OptionType.PostMotionBlur && postProcessing)
            {
                bool val = bool.Parse(value);
                postProcessing.sharedProfile.GetSetting<MotionBlur>().enabled.Override(val);
                postProcessing.profile.GetSetting<MotionBlur>().enabled.Override(val);
            }
            else if (option == OptionType.PostAmbient && postProcessing)
            {
                bool val = bool.Parse(value);
                postProcessing.sharedProfile.GetSetting<AmbientOcclusion>().enabled.Override(val);
                postProcessing.profile.GetSetting<AmbientOcclusion>().enabled.Override(val);
            }
            else if (option == OptionType.Resolution)
            {
                resolutionIndex = int.Parse(value);
            }
            else if (option == OptionType.Fullscreen)
            {
                fullScreenMode = int.Parse(value);
            }
            else if (option == OptionType.Antialiasing)
            {
                int val = int.Parse(value);
                int antialiasing = val == 0 ? 0 : val == 1 ? 2 : val == 2 ? 4 : 8;
                QualitySettings.antiAliasing = antialiasing;
            }
            else if (option == OptionType.TextureQuality)
            {
                int val = int.Parse(value);
                int quality = val == 0 ? 3 : val == 1 ? 2 : val == 2 ? 1 : 0;
                QualitySettings.masterTextureLimit = quality;
            }
            else if (option == OptionType.ShadowResolution)
            {
                int val = int.Parse(value);
                QualitySettings.shadowResolution = (ShadowResolution)val;
            }
            else if (option == OptionType.ShadowDistance)
            {
                QualitySettings.shadowDistance = float.Parse(value);
            }
            else if (option == OptionType.VSync)
            {
                QualitySettings.vSyncCount = bool.Parse(value) == false ? 0 : 1;
            }
        }

        object GetOptionDefault(OptionType option)
        {
            if (option == OptionType.Volume)
            {
                return AudioListener.volume;
            }
            else if (option == OptionType.PostBloom)
            {
                return postProcessing ? postProcessing.profile.GetSetting<Bloom>().enabled : false;
            }
            else if (option == OptionType.PostGrain)
            {
                return postProcessing ? postProcessing.profile.GetSetting<Grain>().enabled : false;
            }
            else if (option == OptionType.PostMotionBlur)
            {
                return postProcessing ? postProcessing.profile.GetSetting<MotionBlur>().enabled : false;
            }
            else if (option == OptionType.PostAmbient)
            {
                return postProcessing ? postProcessing.profile.GetSetting<AmbientOcclusion>().enabled : false;
            }
            else if (option == OptionType.Resolution)
            {
                return GetResolution(Screen.currentResolution);
            }
            else if (option == OptionType.Fullscreen)
            {
                int val = (int)Screen.fullScreenMode;
                return val == 0 ? 0 : val == 1 ? 1 : 2;
            }
            else if (option == OptionType.Antialiasing)
            {
                antialising = QualitySettings.antiAliasing;
                return antialising == 0 ? 0 : antialising == 2 ? 1 : antialising == 4 ? 2 : 3;
            }
            else if (option == OptionType.TextureQuality)
            {
                texq = QualitySettings.masterTextureLimit;
                return texq == 3 ? 0 : texq == 2 ? 1 : texq == 1 ? 2 : 3;
            }
            else if (option == OptionType.ShadowResolution)
            {
                shadowres = (int)QualitySettings.shadowResolution;
                return shadowres;
            }
            else if (option == OptionType.ShadowDistance)
            {
                return QualitySettings.shadowDistance;
            }
            else if (option == OptionType.VSync)
            {
                return QualitySettings.vSyncCount == 0 ? false : true;
            }

            return string.Empty;
        }

        void UpdateOptions(bool apply, bool exist)
        {
            if (device == Device.Keyboard)
            {
                    foreach (var section in GameOptionsPC)
                    {
                        foreach (var entry in section.OptionObjects)
                        {
                            string value;

                            if (exist && jsonHandler.Json()[OPTIONS_PC_PREFIX] != null)
                            {
                                value = jsonHandler.Json()[OPTIONS_PC_PREFIX][section.Section.ToLower()][entry.Prefix].ToString();
                            }
                            else
                            {
                                if (entry.Option != OptionType.Custom)
                                {
                                    value = GetOptionDefault(entry.Option).ToString();
                                }
                                else
                                {
                                    value = entry.DefaultValue;
                                }
                            }

                            if (!string.IsNullOrEmpty(value)) entry.Instance.SetValue(value);

                            if (apply)
                            {
                                ApplyOptionRealtime(entry.Option, value);
                            }
                        }
                    }

                    if (GameQuality) GameQuality.Refresh();
            }
            else
            {
                foreach (var section in GameOptionsConsole)
                {
                    foreach (var entry in section.OptionObjects)
                    {
                        string value = string.Empty;

                        if (exist && jsonHandler.Json()[OPTIONS_CONSOLE_PREFIX] != null)
                        {
                            value = jsonHandler.Json()[OPTIONS_CONSOLE_PREFIX][section.Section.ToLower()][entry.Prefix].ToString();
                        }
                        else
                        {
                            if (entry.Option != OptionType.Custom)
                            {
                                value = GetOptionDefault(entry.Option).ToString();
                            }
                            else
                            {
                                value = entry.DefaultValue;
                            }
                        }

                        if (!string.IsNullOrEmpty(value)) entry.Instance.SetValue(value);

                        if (apply)
                        {
                            ApplyOptionRealtime(entry.Option, value);
                        }
                    }
                }
            }
        }

        public void ResetChangedStatus()
        {
            foreach (var section in GameOptionsPC)
            {
                for (int i = 0; i < section.OptionObjects.Count(); i++)
                {
                    OptionObject entry = section.OptionObjects[i];
                    entry.IsChanged = false;
                    section.OptionObjects[i] = entry;
                }
            }

            foreach (var section in GameOptionsConsole)
            {
                for (int i = 0; i < section.OptionObjects.Count(); i++)
                {
                    OptionObject entry = section.OptionObjects[i];
                    entry.IsChanged = false;
                    section.OptionObjects[i] = entry;
                }
            }
        }

        public void ApplyResolution(int index, int fullscreen)
        {
            if (index > -1)
            {
                ScreenResolution resolution = Resolutions[index];
                FullScreenMode fullScreenMode = fullscreen == 0 ? FullScreenMode.ExclusiveFullScreen : fullscreen == 1 ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;

                if (showRefreshRate && resolution.refreshRate > 0)
                {
                    Screen.SetResolution(resolution.width, resolution.height, fullScreenMode, resolution.refreshRate);
                }
                else
                {
                    Screen.SetResolution(resolution.width, resolution.height, fullScreenMode);
                }
            }

            if (fullscreen > -1)
            {
                FullScreenMode fullScreenMode = fullscreen == 0 ? FullScreenMode.ExclusiveFullScreen : fullscreen == 1 ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
                Screen.fullScreenMode = fullScreenMode;
            }
        }

        public int GetResolution(Resolution res)
        {
            for (int i = 0; i < Resolutions.Count; i++)
            {
                if (Resolutions[i].width == res.width && Resolutions[i].height == res.height)
                {
                    return i;
                }
            }

            return default;
        }

        [Serializable]
        public class OptionSection
        {
            public string Section;
            public List<OptionObject> OptionObjects = new List<OptionObject>();
        }

        [Serializable]
        public class OptionObject
        {
            public string Prefix;
            public OptionBehaviour Instance;
            public OptionType Option = OptionType.Custom;
            public string DefaultValue;
            public bool IsChanged;
        }
    }
}