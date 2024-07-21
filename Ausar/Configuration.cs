using Ausar.Enums;
using Ausar.Helpers;
using Newtonsoft.Json;
using System.ComponentModel;
using System.IO;
using System.Windows;

namespace Ausar
{
    public class Configuration : INotifyPropertyChanged
    {
        private static readonly string _configPath = $"{AssemblyHelper.GetAssemblyName()}.json";

        #region Property Initialisers

        private int _performancePreset = (int)EPerformancePreset.Low;

        #endregion

        #region Game Config

        public int FPS { get; set; } = 60;

        public float FOV { get; set; } = 78.0f;

        public bool IsApplyCustomFOVToVehicles { get; set; } = false;

        public byte CrosshairScaleMode { get; set; } = (byte)ECrosshairScaleMode.Off;

        public bool IsDynamicAspectRatio { get; set; } = false;

        public int ResolutionScale { get; set; } = 100;

        public int PerformancePreset
        {
            get => _performancePreset;

            set
            {
                _performancePreset = value;

                switch ((EPerformancePreset)_performancePreset)
                {
                    case EPerformancePreset.High:
                        DrawDistanceScalar = 300;
                        ObjectDetailScalar = 300;
                        BSPGeometryDrawDistanceScalar = 300;
                        EffectDrawDistanceScalar = 300;
                        ParticleDrawDistanceScalar = 300;
                        DecoratorDrawDistanceScalar = 300;
                        IsToggleFog = true;
                        IsToggleWeather = true;
                        break;

                    case EPerformancePreset.Medium:
                        DrawDistanceScalar = 200;
                        ObjectDetailScalar = 200;
                        BSPGeometryDrawDistanceScalar = 200;
                        EffectDrawDistanceScalar = 200;
                        ParticleDrawDistanceScalar = 200;
                        DecoratorDrawDistanceScalar = 200;
                        IsToggleFog = true;
                        IsToggleWeather = true;
                        break;

                    case EPerformancePreset.Low:
                        DrawDistanceScalar = 100;
                        ObjectDetailScalar = 100;
                        BSPGeometryDrawDistanceScalar = 100;
                        EffectDrawDistanceScalar = 100;
                        ParticleDrawDistanceScalar = 100;
                        DecoratorDrawDistanceScalar = 100;
                        IsToggleFog = true;
                        IsToggleWeather = true;
                        break;

                    case EPerformancePreset.VeryLow:
                        DrawDistanceScalar = 75;
                        ObjectDetailScalar = 75;
                        BSPGeometryDrawDistanceScalar = 75;
                        EffectDrawDistanceScalar = 0;
                        ParticleDrawDistanceScalar = 0;
                        DecoratorDrawDistanceScalar = 0;
                        IsToggleFog = false;
                        IsToggleWeather = false;
                        break;
                }
            }
        }

        public int DrawDistanceScalar { get; set; } = 100;

        public int ObjectDetailScalar { get; set; } = 100;

        public int BSPGeometryDrawDistanceScalar { get; set; } = 100;

        public int EffectDrawDistanceScalar { get; set; } = 100;

        public int ParticleDrawDistanceScalar { get; set; } = 100;

        public int DecoratorDrawDistanceScalar { get; set; } = 100;

        public bool IsToggleFog { get; set; } = true;

        public bool IsToggleWeather { get; set; } = true;

        public bool IsToggleFrontend { get; set; } = true;

        public bool IsToggleNavigationPoints { get; set; } = true;

        public bool IsToggleRagdoll { get; set; } = true;

        public bool IsToggleSmallerCrosshairScale { get; set; } = false;

        public bool IsToggleSmartLink { get; set; } = true;

        public bool IsToggleThirdPersonCamera { get; set; } = false;

        public bool IsToggleWorldSpaceViewModel { get; set; } = false;

        #endregion

        #region Frontend Config

        [JsonIgnore]
        public bool IsCrosshairScaleModeAvailable { get; set; } = true;

        [JsonIgnore]
        public bool IsDynamicAspectRatioAvailable { get; set; } = true;

        [JsonIgnore]
        public string ResolutionString { get; set; }

        public int PatchFrequency { get; set; } = 16;

        public bool IsUninstallPatchesOnExit { get; set; } = true;

        public bool IsAllowDynamicAspectRatioInGame { get; set; } = false;

        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged(PropertyChangedEventArgs in_args)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(in_args.PropertyName));

            Export();
        }

        public Configuration Import(bool in_isDefault = false)
        {
            if (in_isDefault || !File.Exists(_configPath))
                return Export(in_isDefault);
#if !DEBUG
            try
            {
#endif
                return JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(_configPath));
#if !DEBUG
            }
            catch (JsonReaderException out_ex)
            {
                MessageBox.Show($"Failed to import configuration!\n\n{out_ex.Message}\n\nThe configuration will now be reset.", "Ausar", MessageBoxButton.OK, MessageBoxImage.Error);
            }
#endif
            return Export(in_isDefault);
        }

        public Configuration Export(bool in_isDefault = false)
        {
            var instance = this;

            if (in_isDefault)
                instance = Activator.CreateInstance<Configuration>();

            File.WriteAllText(_configPath, JsonConvert.SerializeObject(instance, Formatting.Indented));

            return this;
        }

        public void Reset()
        {
            Import(true);

            App.Restart();
        }
    }
}
