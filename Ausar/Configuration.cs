using Ausar.Enums;
using Ausar.Helpers;
using Newtonsoft.Json;
using System.ComponentModel;
using System.IO;

namespace Ausar
{
    public class Configuration : INotifyPropertyChanged
    {
        private static readonly string _configPath = $"{AssemblyHelper.GetAssemblyName()}.json";

        #region Default Initialisers

        private int _performancePreset = (int)EPerformancePreset.Low;

        #endregion

        #region Game Config

        public int FPS { get; set; } = 60;

        public float FOV { get; set; } = 78.0f;

        public bool IsApplyCustomFOVToVehicles { get; set; } = false;

        public bool IsDynamicAspectRatio { get; set; } = false;

        public float ResolutionScale { get; set; } = 100.0f;

        public int PerformancePreset
        {
            get => _performancePreset;

            set
            {
                _performancePreset = value;

                switch ((EPerformancePreset)_performancePreset)
                {
                    case EPerformancePreset.High:
                        DrawDistanceScalar = 3.0f;
                        ObjectDetailScalar = 3.0f;
                        BSPGeometryDrawDistanceScalar = 3.0f;
                        EffectDrawDistanceScalar = 3.0f;
                        ParticleDrawDistanceScalar = 3.0f;
                        DecoratorDrawDistanceScalar = 3.0f;
                        IsToggleFog = true;
                        IsToggleWeather = true;
                        break;

                    case EPerformancePreset.Medium:
                        DrawDistanceScalar = 2.0f;
                        ObjectDetailScalar = 2.0f;
                        BSPGeometryDrawDistanceScalar = 2.0f;
                        EffectDrawDistanceScalar = 2.0f;
                        ParticleDrawDistanceScalar = 2.0f;
                        DecoratorDrawDistanceScalar = 2.0f;
                        IsToggleFog = true;
                        IsToggleWeather = true;
                        break;

                    case EPerformancePreset.Low:
                        DrawDistanceScalar = 1.0f;
                        ObjectDetailScalar = 1.0f;
                        BSPGeometryDrawDistanceScalar = 1.0f;
                        EffectDrawDistanceScalar = 1.0f;
                        ParticleDrawDistanceScalar = 1.0f;
                        DecoratorDrawDistanceScalar = 1.0f;
                        IsToggleFog = true;
                        IsToggleWeather = true;
                        break;

                    case EPerformancePreset.VeryLow:
                        DrawDistanceScalar = 0.75f;
                        ObjectDetailScalar = 0.75f;
                        BSPGeometryDrawDistanceScalar = 0.75f;
                        EffectDrawDistanceScalar = 0.0f;
                        ParticleDrawDistanceScalar = 0.0f;
                        DecoratorDrawDistanceScalar = 0.0f;
                        IsToggleFog = false;
                        IsToggleWeather = false;
                        break;
                }
            }
        }

        public float DrawDistanceScalar { get; set; } = 1.0f;

        public float ObjectDetailScalar { get; set; } = 1.0f;

        public float BSPGeometryDrawDistanceScalar { get; set; } = 1.0f;

        public float EffectDrawDistanceScalar { get; set; } = 1.0f;

        public float ParticleDrawDistanceScalar { get; set; } = 1.0f;

        public float DecoratorDrawDistanceScalar { get; set; } = 1.0f;

        public bool IsToggleFog { get; set; } = true;

        public bool IsToggleWeather { get; set; } = true;

        public bool IsToggleFrontend { get; set; } = true;

        public bool IsToggleNavigationPoints { get; set; } = true;

        public bool IsToggleRagdoll { get; set; } = true;

        public bool IsToggleSmallerCrosshairScale { get; set; } = false;

        public bool IsToggleThirdPersonCamera { get; set; } = false;

        public bool IsToggleWorldSpaceViewModel { get; set; } = false;

        #endregion

        #region Frontend Config

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

            return JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(_configPath));
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
