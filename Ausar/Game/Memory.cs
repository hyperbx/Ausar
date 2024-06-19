using Ausar.Enums;
using Ausar.Interop;
using Ausar.Services;
using System.Diagnostics;

namespace Ausar.Game
{
    public class Memory : IDisposable
    {
        private const float _defaultAspectRatio = 1.777777777777778f;
        private const float _defaultFOV = 78.0f;

        private ProcessMemoryService _pms;

        private CancellationTokenSource _cancellationTokenSource = new();

        private bool _isInitialised = false;

        public bool IsResolutionScaleUpdated { get; set; } = false;

        // Research from H5Tweak, ported from game version 1.114.4592.2
        public int FPS
        {
            get
            {
                var fps = _pms.Read<int>(_pms.ASLR(0x143425078));

                if (fps <= 0)
                    return 60;

                return 1000000 / fps;
            }

            set
            {
                var fps = Convert.ToInt32(MathF.Floor(1.0f / value * 1000000.0f));

                for (int i = 0; i < 20; i += 10)
                    _pms.WriteProtected(_pms.ASLR(0x143425078) + i, fps);
            }
        }

        // Research from Exuberant
        public float FOV
        {
            get => _pms.Read<float>(_pms.ASLR(0x14590E210));

            set
            {
                if (value > _defaultFOV)
                {
                    // Fix for high FOV being zoomed in on spawn.
                    _pms.WriteProtected<byte>(_pms.ASLR(0x14079A162), 0x20);
                }
                else
                {
                    // Restore original operand.
                    _pms.Restore(_pms.ASLR(0x14079A162));
                }

                _pms.WriteProtected(_pms.ASLR(0x14590E210), value);
            }
        }

        // Research from H5Tweak, ported from game version 1.114.4592.2
        public float AspectRatio
        {
            get => _pms.Read<float>(_pms.ASLR(0x14333E458));
            set => _pms.WriteProtected(_pms.ASLR(0x14333E458), value);
        }

        public DisplayParameters DisplayParameters
        {
            get => _pms.Read<DisplayParameters>(_pms.ASLR(0x144857610));

            set
            {
#if DEBUG
                if (value.Refresh)
                    Debug.WriteLine("Refreshing graphics device...");
#endif

                _pms.WriteProtected(_pms.ASLR(0x144857610), value);
            }
        }

        // Research from Exuberant
        public float DrawDistanceScalar
        {
            get => 1.0f * _pms.Read<float>(GetOffsetFromTLS(0x190) + 0x2BC);
            set => _pms.WriteProtected(GetOffsetFromTLS(0x190) + 0x2BC, 1.0f / value);
        }

        // Research from Exuberant
        public float ObjectDetailScalar
        {
            get => _pms.Read<float>(GetOffsetFromTLS(0x3050) + 0x7C);
            set => _pms.WriteProtected(GetOffsetFromTLS(0x3050) + 0x7C, value);
        }

        // Research from Exuberant
        public float BSPGeometryDrawDistanceScalar
        {
            get => _pms.Read<float>(GetOffsetFromTLS(0x3050) + 0x80);
            set => _pms.WriteProtected(GetOffsetFromTLS(0x3050) + 0x80, value);
        }

        // Research from Exuberant
        public float EffectDrawDistanceScalar
        {
            get => _pms.Read<float>(GetOffsetFromTLS(0x3050) + 0x38);
            set => _pms.WriteProtected(GetOffsetFromTLS(0x3050) + 0x38, value);
        }

        // Research from Exuberant
        public float ParticleDrawDistanceScalar
        {
            get => _pms.Read<float>(GetOffsetFromTLS(0x3050) + 0x44);
            set => _pms.WriteProtected(GetOffsetFromTLS(0x3050) + 0x44, value);
        }

        // Research from Exuberant
        public float DecoratorDrawDistanceScalar
        {
            get => _pms.Read<float>(GetOffsetFromTLS(0x3050) + 0x90);
            set => _pms.WriteProtected(GetOffsetFromTLS(0x3050) + 0x90, value);
        }

        // Research from Exuberant
        public bool IsFog
        {
            get => _pms.Read<bool>(GetOffsetFromTLS(0x2FE0));
            set => _pms.WriteProtected(GetOffsetFromTLS(0x2FE0), value);
        }

        // Research from Exuberant
        public bool IsWeather
        {
            get => _pms.Read<bool>(GetOffsetFromTLS(0x2FE0) + 2);
            set => _pms.WriteProtected(GetOffsetFromTLS(0x2FE0) + 2, value);
        }

        // Research from Exuberant
        public bool IsRagdoll
        {
            get => _pms.Read<int>(GetOffsetFromTLS(0x2FB0)) != -1;
            set => _pms.WriteProtected(GetOffsetFromTLS(0x2FB0), value ? 0 : -1);
        }

        public bool IsSmallerCrosshairScale
        {
            get => _pms.Read<bool>(_pms.ASLR(0x1463EFB35));
            set => _pms.WriteProtected(_pms.ASLR(0x1463EFB35), value);
        }

        public bool IsWorldSpaceViewModel
        {
            get => _pms.Read<bool>(_pms.ASLR(0x14485E554));
            set => _pms.WriteProtected(_pms.ASLR(0x14485E554), value);
        }

        public string Map
        {
            get => _pms.ReadStringNullTerminated(_pms.ASLR(0x145E58DC8));
        }

        public Memory(Process in_process)
        {
            _pms = new(in_process);

            Task.Run(Update);

#if DEBUG
            Debug.WriteLine("Game connected.");
#endif
        }

        private void Update()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                InstallPatches();

                Thread.Sleep(App.Settings.PatchFrequency);
            }

#if DEBUG
            Debug.WriteLine("Game disconnected.");
#endif
        }

        public nint GetThreadLocalStoragePointer()
        {
            _pms.Process.Refresh();

            foreach (ProcessThread thread in _pms.Process.Threads)
            {
                if (thread.ThreadState != System.Diagnostics.ThreadState.Running)
                    continue;

                var handle = Win32.OpenThread(Win32.THREAD_ALL_ACCESS, false, thread.Id);

                if (handle == 0)
                {
#if DEBUG
                    Debug.WriteLine($"Failed to open thread {thread.Id}...");
#endif

                    continue;
                }

                var threadInfo = Win32.GetThreadInformation(handle);

                Win32.CloseHandle(handle);

                if (threadInfo.TebBaseAddress == 0)
                {
#if DEBUG
                    Debug.WriteLine($"Invalid environment block in thread {thread.Id}...");
#endif

                    continue;
                }
#if DEBUG
                Debug.WriteLine($"Successfully retrieved info from thread {thread.Id}.");
#endif
                var tlsPtr = _pms.Read<nint>(threadInfo.TebBaseAddress + 0x58);
                var tlsIndex = _pms.Read<int>(_pms.ASLR(0x145F1D56C));

                return _pms.Read<nint>(tlsPtr + (tlsIndex * 8));
            }
#if DEBUG
            Debug.WriteLine("Could not find main thread...");
#endif
            return 0;
        }

        public nint GetOffsetFromTLS(int in_offset)
        {
            return _pms.Read<nint>(GetThreadLocalStoragePointer() + in_offset);
        }

        // Research from Exuberant
        public void PatchApplyCustomFOVToVehicles(bool in_isEnabled)
        {
            if (in_isEnabled)
            {
                // Force condition to disable FOV change when driving vehicles.
                _pms.WriteProtected<byte>(_pms.ASLR(0x1406EE8EA), 0xEB, true);
            }
            else
            {
                // Restore original opcode.
                _pms.Restore(_pms.ASLR(0x1406EE8EA));
            }
        }

        private void PatchDynamicAspectRatio(bool in_isEnabled)
        {
            var smartLinkAspectRatioHookAddr = _pms.ASLR(0x14162D4BC);

            if (in_isEnabled)
            {
                if (!_isInitialised)
                {
                    // Hook aspect ratio code for Smart Link.
                    _pms.WriteAsmHook
                    (
                        $@"
                            mov    r8, {_pms.ASLR(0x14333E458)} ; Store pointer to real aspect ratio in R8.
                            movss  xmm6, dword ptr [r8]         ; Store real aspect ratio in XMM6.
                            mov    r9d, 0x3FE38E39              ; Store 1.777777777777778f in R9D.
                            movd   xmm7, r9d                    ; Copy R9D to XMM7.
                            divss  xmm6, xmm7                   ; Divide real aspect ratio (XMM6) by default aspect ratio (XMM7).
                            mulss  xmm0, xmm6                   ; Multiply Smart Link aspect ratio (XMM0) by our divided value (XMM6).

                            ; Restore original code.
                            movups [rbx], xmm0
                            movups [rbx + 0x10], xmm1
                            movups [rbx + 0x20], xmm2
                            movups [rbx + 0x30], xmm3
                        ",

                        smartLinkAspectRatioHookAddr
                    );
                }
            }
            else
            {
                _pms.Restore(smartLinkAspectRatioHookAddr);
            }

            if (App.Settings.IsAllowDynamicAspectRatioInGame)
            {
                App.Settings.IsDynamicAspectRatioAvailable = true;
            }
            else
            {
                /* HACK: Don't update aspect ratio unless we're on the main
                         menu where we can safely refresh the graphics device
                         without destroying the fast HUD font renderer.

                         The base game already forces you to quit out to the
                         main menu just to change the resolution anyway. */
                if (Map.Contains("mainmenu"))
                {
                    App.Settings.IsDynamicAspectRatioAvailable = true;
                }
                else
                {
                    App.Settings.IsDynamicAspectRatioAvailable = false;
                    return;
                }
            }

            if (in_isEnabled)
            {
                var newAspectRatio = (float)DisplayParameters.WindowWidth / (float)DisplayParameters.WindowHeight;

                if (IsResolutionScaleUpdated)
                {
                    IsResolutionScaleUpdated = false;
                }
                else if (AspectRatio == newAspectRatio || User32.IsKeyDown(EKeys.LButton))
                {
                    return;
                }

                var newDisplayParams = DisplayParameters;
                {
                    var scale = App.Settings.ResolutionScale / 100.0f;

                    newDisplayParams.Refresh = true;
                    newDisplayParams.StoredWidth = Convert.ToInt32(newDisplayParams.WindowWidth * scale);
                    newDisplayParams.StoredHeight = Convert.ToInt32(newDisplayParams.WindowHeight * scale);

                    App.Settings.ResolutionString = $"{newDisplayParams.StoredWidth}x{newDisplayParams.StoredHeight}";
                }

                DisplayParameters = newDisplayParams;

                // Disable full screen width/height override.
                _pms.WriteNop(_pms.ASLR(0x14155B111), 3);
                _pms.WriteNop(_pms.ASLR(0x14155B11E), 4);

                AspectRatio = newAspectRatio;
            }
            else
            {
                if (AspectRatio == _defaultAspectRatio)
                    return;

                var newDisplayParams = DisplayParameters;
                {
                    newDisplayParams.Refresh = true;
                    newDisplayParams.StoredWidth = newDisplayParams.UIWidth;
                    newDisplayParams.StoredHeight = newDisplayParams.UIHeight;

                    App.Settings.ResolutionString = string.Empty;
                }

                DisplayParameters = newDisplayParams;

                // Restore full screen width/height override.
                _pms.Restore(_pms.ASLR(0x14155B111));
                _pms.Restore(_pms.ASLR(0x14155B11E));

                AspectRatio = _defaultAspectRatio;
            }
        }

        public void PatchToggleFrontend(bool in_isEnabled)
        {
            // Patch instruction operand to force return value.
            _pms.WriteProtected(_pms.ASLR(0x14136274D), Convert.ToByte(in_isEnabled));
        }

        public void PatchToggleNavigationPoints(bool in_isEnabled)
        {
            if (in_isEnabled)
            {
                // Restore original opcode.
                _pms.Restore(_pms.ASLR(0x141A589FE));

                // Restore original fixed scale.
                _pms.Restore(_pms.ASLR(0x141A58A18));
            }
            else
            {
                // Force condition to use fixed scale.
                _pms.WriteProtected<byte>(_pms.ASLR(0x141A589FE), 0xEB, true);

                // Set fixed scale to zero.
                _pms.WriteProtected(_pms.ASLR(0x141A58A18), 0.0f, true);
            }
        }

        // Research from Exuberant
        public void PatchToggleThirdPersonCamera(bool in_isEnabled)
        {
            if (in_isEnabled)
            {
                // Remove condition to force third-person camera.
                _pms.WriteNop(_pms.ASLR(0x1406E6138), 2);
            }
            else
            {
                // Restore original instruction.
                _pms.Restore(_pms.ASLR(0x1406E6138));
            }
        }

        public void InstallPatches()
        {
            try
            {
                // Prevent applying patches during boot sequence.
                if (string.IsNullOrEmpty(Map))
                    return;

                FPS = App.Settings.FPS;
                FOV = App.Settings.FOV;
                DrawDistanceScalar = App.Settings.DrawDistanceScalar;
                ObjectDetailScalar = App.Settings.ObjectDetailScalar;
                BSPGeometryDrawDistanceScalar = App.Settings.BSPGeometryDrawDistanceScalar;
                EffectDrawDistanceScalar = App.Settings.EffectDrawDistanceScalar;
                ParticleDrawDistanceScalar = App.Settings.ParticleDrawDistanceScalar;
                DecoratorDrawDistanceScalar = App.Settings.DecoratorDrawDistanceScalar;
                IsFog = App.Settings.IsToggleFog;
                IsWeather = App.Settings.IsToggleWeather;
                IsRagdoll = App.Settings.IsToggleRagdoll;
                IsSmallerCrosshairScale = App.Settings.IsToggleSmallerCrosshairScale;
                IsWorldSpaceViewModel = !App.Settings.IsToggleWorldSpaceViewModel;

                PatchApplyCustomFOVToVehicles(App.Settings.IsApplyCustomFOVToVehicles);
                PatchDynamicAspectRatio(App.Settings.IsDynamicAspectRatio);
                PatchToggleFrontend(App.Settings.IsToggleFrontend);
                PatchToggleNavigationPoints(App.Settings.IsToggleNavigationPoints);
                PatchToggleThirdPersonCamera(App.Settings.IsToggleThirdPersonCamera);
            }
            catch (Exception out_ex)
            {
                Debug.WriteLine($"An unhandled exception occurred whilst installing patches. Ignoring...\n{out_ex}\n");
            }

            _isInitialised = true;
        }

        public void UninstallPatches()
        {
            try
            {
                /* FIXME: this will only work once, not a big
                          deal for its current use case though. */
                _cancellationTokenSource.Cancel();

                FPS = 60;
                FOV = 78;
                DrawDistanceScalar = 1.0f;
                ObjectDetailScalar = 1.0f;
                BSPGeometryDrawDistanceScalar = 1.0f;
                EffectDrawDistanceScalar = 1.0f;
                ParticleDrawDistanceScalar = 1.0f;
                DecoratorDrawDistanceScalar = 1.0f;
                IsFog = true;
                IsWeather = true;
                IsRagdoll = true;
                IsSmallerCrosshairScale = false;
                IsWorldSpaceViewModel = false;

                PatchApplyCustomFOVToVehicles(false);
                PatchDynamicAspectRatio(false);
                PatchToggleFrontend(true);
                PatchToggleNavigationPoints(true);
                PatchToggleThirdPersonCamera(false);
            }
            catch (Exception out_ex)
            {
                Debug.WriteLine($"An unhandled exception occurred whilst uninstalling patches. Ignoring...\n{out_ex}\n");
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
        }
    }
}
