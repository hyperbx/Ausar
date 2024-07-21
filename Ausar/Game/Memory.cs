using Ausar.Enums;
using Ausar.Helpers;
using Ausar.Interop;
using Ausar.Logger;
using ProcessExtensions;
using System.Diagnostics;
using Vanara.PInvoke;

namespace Ausar.Game
{
    public class Memory : IDisposable
    {
        private const float _defaultAspectRatio = 1.777777777777778f;
        private const float _defaultFOV = 78.0f;
        private const float _maxCrosshairScale = 150.0f;
        private const float _minCrosshairScale = 50.0f;
        private const float _maxFOV = 150.0f;
        private const float _minFOV = 60.0f;

        private static bool _isScaleCrosshairToFOVInitialised = false;
        private static bool _isDynamicAspectRatioInitialised = false;

        private bool _isUpdating = true;

        private int[] _simPresentIntervals = [60, 30, 120];

        public Process Process { get; set; }

        public bool IsResolutionScaleUpdated { get; set; } = false;

        // Research from H5Tweak, ported from game version 1.114.4592.2
        public int FPS
        {
            get
            {
                var fps = Process.Read<int>(Process.ToASLR(0x143425078));

                if (fps <= 0)
                    return 60;

                return 1000000 / fps;
            }

            set
            {
                var fps = Convert.ToInt32(MathF.Floor(1.0f / value * 1000000.0f));

                for (int i = 0; i < 20; i += 10)
                    Process.WriteProtected(Process.ToASLR(0x143425078) + i, fps);
            }
        }

        public int SimPresentInterval
        {
            get => _simPresentIntervals[Process.Read<int>(Process.ToASLR(0x145EB08B4))];
        }

        // Research from Exuberant
        public float FOV
        {
            get => Process.Read<float>(Process.ToASLR(0x14590E210));

            set
            {
                if (value > _defaultFOV)
                {
                    // Fix for high FOV being zoomed in on spawn.
                    Process.WriteProtected<byte>(Process.ToASLR(0x14079A162), 0x20);
                }
                else
                {
                    // Restore original operand.
                    Process.RestoreMemory(Process.ToASLR(0x14079A162));
                }

                Process.WriteProtected(Process.ToASLR(0x14590E210), value);
            }
        }

        public float FOVZoomScalar
        {
            get => Process.Read<float>(Process.ToASLR(0x144857858));
        }

        // Research from H5Tweak, ported from game version 1.114.4592.2
        public float AspectRatio
        {
            get => Process.Read<float>(Process.ToASLR(0x14333E458));
            set => Process.WriteProtected(Process.ToASLR(0x14333E458), value);
        }

        public DisplayParameters DisplayParameters
        {
            get => Process.Read<DisplayParameters>(Process.ToASLR(0x144857610));

            set
            {
#if DEBUG
                if (value.Refresh)
                    LoggerService.Utility("Refreshing graphics device...");
#endif

                Process.WriteProtected(Process.ToASLR(0x144857610), value);
            }
        }

        // Research from Exuberant
        public float DrawDistanceScalar
        {
            get => 1.0f * Process.Read<float>(GetOffsetFromTLS(0x190) + 0x2BC);
            set => Process.WriteProtected(GetOffsetFromTLS(0x190) + 0x2BC, 1.0f / value);
        }

        // Research from Exuberant
        public float ObjectDetailScalar
        {
            get => Process.Read<float>(GetOffsetFromTLS(0x3050) + 0x7C);
            set => Process.WriteProtected(GetOffsetFromTLS(0x3050) + 0x7C, value);
        }

        // Research from Exuberant
        public float BSPGeometryDrawDistanceScalar
        {
            get => Process.Read<float>(GetOffsetFromTLS(0x3050) + 0x80);
            set => Process.WriteProtected(GetOffsetFromTLS(0x3050) + 0x80, value);
        }

        // Research from Exuberant
        public float EffectDrawDistanceScalar
        {
            get => Process.Read<float>(GetOffsetFromTLS(0x3050) + 0x38);
            set => Process.WriteProtected(GetOffsetFromTLS(0x3050) + 0x38, value);
        }

        // Research from Exuberant
        public float ParticleDrawDistanceScalar
        {
            get => Process.Read<float>(GetOffsetFromTLS(0x3050) + 0x44);
            set => Process.WriteProtected(GetOffsetFromTLS(0x3050) + 0x44, value);
        }

        // Research from Exuberant
        public float DecoratorDrawDistanceScalar
        {
            get => Process.Read<float>(GetOffsetFromTLS(0x3050) + 0x90);
            set => Process.WriteProtected(GetOffsetFromTLS(0x3050) + 0x90, value);
        }

        // Research from Exuberant
        public bool IsFog
        {
            get => Process.Read<bool>(GetOffsetFromTLS(0x2FE0));
            set => Process.WriteProtected(GetOffsetFromTLS(0x2FE0), value);
        }

        // Research from Exuberant
        public bool IsWeather
        {
            get => Process.Read<bool>(GetOffsetFromTLS(0x2FE0) + 2);
            set => Process.WriteProtected(GetOffsetFromTLS(0x2FE0) + 2, value);
        }

        // Research from Exuberant
        public bool IsRagdoll
        {
            get => Process.Read<int>(GetOffsetFromTLS(0x2FB0)) != -1;
            set => Process.WriteProtected(GetOffsetFromTLS(0x2FB0), value ? 0 : -1);
        }

        public bool IsSmallerCrosshairScale
        {
            get => Process.Read<bool>(Process.ToASLR(0x1463EFB35));
            set => Process.WriteProtected(Process.ToASLR(0x1463EFB35), value);
        }

        public bool IsWorldSpaceViewModel
        {
            get => Process.Read<bool>(Process.ToASLR(0x14485E554));
            set => Process.WriteProtected(Process.ToASLR(0x14485E554), value);
        }

        public string Map
        {
            get => Process.ReadStringNullTerminated(Process.ToASLR(0x145E58DC8));
        }

        public Memory(Process in_process)
        {
            Process = in_process;

            Task.Run(Update);

#if DEBUG
            LoggerService.Utility("Game connected.");
#endif
        }

        private void Update()
        {
            while (_isUpdating)
            {
                InstallPatches();

                Thread.Sleep(App.Settings.PatchFrequency);
            }

#if DEBUG
            LoggerService.Utility("Game disconnected.");
#endif
        }

        public nint GetThreadLocalStoragePointer()
        {
            Process.Refresh();

            foreach (ProcessThread thread in Process.Threads)
            {
                if (thread.ThreadState != System.Diagnostics.ThreadState.Running)
                    continue;

                var handle = Kernel32.OpenThread((int)Kernel32.ThreadAccess.THREAD_ALL_ACCESS, false, (uint)thread.Id);

                if (handle == 0)
                {
#if DEBUG
                    LoggerService.Error($"Failed to open thread {thread.Id}...");
#endif

                    continue;
                }

                var threadInfo = NtDllHelper.GetThreadInformation(handle);

                handle.Close();

                if (threadInfo.TebBaseAddress == 0)
                {
#if DEBUG
                    LoggerService.Error($"Invalid environment block in thread {thread.Id}...");
#endif

                    continue;
                }

                var tlsPtr = Process.Read<nint>(threadInfo.TebBaseAddress + 0x58);
                var tlsIndex = Process.Read<int>(Process.ToASLR(0x145F1D56C));

                return Process.Read<nint>(tlsPtr + (tlsIndex * 8));
            }
#if DEBUG
            LoggerService.Error("Could not find main thread...");
#endif
            return 0;
        }

        public nint GetOffsetFromTLS(int in_offset)
        {
            return Process.Read<nint>(GetThreadLocalStoragePointer() + in_offset);
        }

        // Research from Exuberant
        private void PatchApplyCustomFOVToVehicles(bool in_isEnabled)
        {
            if (in_isEnabled)
            {
                // Force condition to disable FOV change when driving vehicles.
                Process.WriteProtected<byte>(Process.ToASLR(0x1406EE8EA), 0xEB, true);
            }
            else
            {
                // Restore original opcode.
                Process.RestoreMemory(Process.ToASLR(0x1406EE8EA));
            }
        }

        private unsafe void PatchCrosshairScaleMode(ECrosshairScaleMode in_mode)
        {
            /* HACK: The mid-ASM hook is written in a function that is run
                     so frequently in-game that it can cause a crash if the
                     code is jumped to whilst still being written. */
            if (Map.Contains("mainmenu"))
            {
                App.Settings.IsCrosshairScaleModeAvailable = true;
            }
            else
            {
                App.Settings.IsCrosshairScaleModeAvailable = false;
                return;
            }

            var crosshairMatrixHookAddr = Process.ToASLR(0x1417147C7);
            var crosshairScaleModeAddr  = Process.Alloc("CrosshairScaleMode", 1);

            if (in_mode != ECrosshairScaleMode.Off)
            {
                Process.Write(crosshairScaleModeAddr, App.Settings.CrosshairScaleMode);

                if (!_isScaleCrosshairToFOVInitialised)
                {
                    // Hook matrix code for crosshair.
                    _isScaleCrosshairToFOVInitialised = Process.TryWriteAsmHook
                    (
                        $@"
                            mov    r11d, 0x42700000                         ; Store minimum FOV value (60.0f) in R11D.
                            movd   xmm0, r11d                               ; Copy R11D to XMM0.
                            mov    r11d, 0x43160000                         ; Store maximum FOV value (150.0f) in R11D.
                            movd   xmm1, r11d                               ; Copy R11D to XMM1.
                            mov    r11, {(long)Process.ToASLR(0x14590E210)} ; Store address to FOV value in R11.
                            movss  xmm2, dword ptr [r11]                    ; Copy FOV value to XMM2.
                            mov    r11, {(long)Process.ToASLR(0x144857858)} ; Store address to FOV zoom scalar value in R11.
                            movss  xmm3, dword ptr [r11]                    ; Copy FOV zoom scalar value to XMM3.
                            
                            mov    r11, {(long)crosshairScaleModeAddr}      ; Store address to crosshair scale mode in R11.
                            cmp    byte ptr [r11], 2                        ; Compare crosshair scale mode with 2 (ECrosshairScaleMode.ScaleSmartLink).
                            jne    ignoreZoom                               ; Jump if not 2; otherwise, apply FOV zoom scalar value.
                            divss  xmm2, xmm3                               ; XMM2 = FOV / FOVZoomScalar

                        ignoreZoom:
                            subss  xmm2, xmm0                               ; XMM2 -= _minFOV
                            subss  xmm1, xmm0                               ; XMM1 = _maxFOV - _minFOV
                            divss  xmm2, xmm1                               ; XMM2 /= XMM1
                                                                            
                            mov    r11d, 0x42480000                         ; Store minimum crosshair scale value (50.0f) in R11D.
                            movd   xmm3, r11d                               ; Copy R11D to XMM3.
                            mov    r11d, 0x43160000                         ; Store maximum crosshair scale value (150.0f) in R11D.
                            movd   xmm4, r11d                               ; Copy R11D to XMM4.
                                                                            
                            ; Linear interpolation routine.
                            subss  xmm4, xmm3                               ; XMM4 = _maxCrosshairScale - _minCrosshairScale
                            mulss  xmm2, xmm4                               ; XMM2 *= XMM4
                            addss  xmm2, xmm3                               ; XMM2 += _minCrosshairScale
                                                                            
                            mov    r11d, 0x41F00000                         ; Store crosshair scale remainder value (30.0f) in R11D.
                            movd   xmm5, r11d                               ; Copy R11D to XMM5.
                            addss  xmm2, xmm5                               ; XMM2 += 30.0f

                            ; Clamp routine.
                            comiss xmm2, xmm3                               ; Compare result with _minCrosshairScale.
                            jae    checkMax                                 ; Jump if result is greater than minimum.
                            movaps xmm2, xmm3                               ; XMM2 = _minCrosshairScale

                        checkMax:
                            mov    r11d, 0x43160000                         ; Store maximum crosshair scale value (150.0f) in R11D.
                            movd   xmm4, r11d                               ; Copy R11D to XMM4.
                            comiss xmm2, xmm4                               ; Compare result with _maxCrosshairScale.
                            jbe    end                                      ; Jump if result is less than maximum.
                            movaps xmm2, xmm4                               ; XMM2 = _maxCrosshairScale

                        end:
                            mov    r11d, 0x42C80000                         ; Store percentage max value (100.0f) in R11D.
                            movd   xmm3, r11d                               ; Copy R11D to XMM3.
                            divss  xmm2, xmm3                               ; XMM2 /= 100.0f
                            mov    r11d, 0x43B40000                         ; Store radius max value (360.0f) in R11D.
                            movd   xmm4, r11d                               ; Copy R11D to XMM4.
                            mulss  xmm2, xmm4                               ; XMM2 *= 360.0f
                            movss  dword ptr [rax + 0x30], xmm2             ; Copy XMM2 to matrix at scale offset.

                            ; Restore original code.
                            movups xmm0, xmmword ptr [rax]
                            lea    r11, qword ptr [rsp + 0xC0]
                            movups xmm1, xmmword ptr [rax + 0x10]
                        ",

                        crosshairMatrixHookAddr
                    );
                }
            }
            else
            {
                Process.RemoveAsmHook(crosshairMatrixHookAddr);
                Process.Free(crosshairScaleModeAddr);

                _isScaleCrosshairToFOVInitialised = false;
            }
        }

        private void PatchDynamicAspectRatio(bool in_isEnabled)
        {
            var smartLinkAspectRatioHookAddr = Process.ToASLR(0x14162D4BC);

            if (in_isEnabled)
            {
                if (!_isDynamicAspectRatioInitialised)
                {
                    // Hook aspect ratio code for Smart Link.
                    _isDynamicAspectRatioInitialised = Process.TryWriteAsmHook
                    (
                        $@"
                            mov    r8, {Process.ToASLR(0x14333E458)} ; Store pointer to real aspect ratio in R8.
                            movss  xmm6, dword ptr [r8]              ; Store real aspect ratio in XMM6.
                            mov    r9d, 0x3FE38E39                   ; Store default aspect ratio (1.777777777777778f) in R9D.
                            movd   xmm7, r9d                         ; Copy R9D to XMM7.
                            divss  xmm6, xmm7                        ; Divide real aspect ratio (XMM6) by default aspect ratio (XMM7).
                            mulss  xmm0, xmm6                        ; Multiply Smart Link aspect ratio (XMM0) by our divided value (XMM6).

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
                Process.RemoveAsmHook(smartLinkAspectRatioHookAddr);

                _isDynamicAspectRatioInitialised = false;
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
                else if (AspectRatio == newAspectRatio || User32Helper.IsKeyDown(EKeys.LButton))
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
                Process.WriteNop(Process.ToASLR(0x14155B111), 3);
                Process.WriteNop(Process.ToASLR(0x14155B11E), 4);

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
                Process.RestoreMemory(Process.ToASLR(0x14155B111));
                Process.RestoreMemory(Process.ToASLR(0x14155B11E));

                AspectRatio = _defaultAspectRatio;
            }
        }

        private void PatchNetworkIntegrity(bool in_isEnabled)
        {
            var addrs = new nint[] { Process.ToASLR(0x142297E41), Process.ToASLR(0x142297F34) };

            if (in_isEnabled)
            {
                var networkTickRate = SimPresentInterval;

                if (SimPresentInterval == 120)
                    networkTickRate = 60;

                foreach (var addr in addrs)
                {
                    Process.PreserveMemory(addr, 5);

                    // Patch networking tick rate to original rate.
                    Process.WriteProtected<byte>(addr, 0xB8);
                    Process.WriteProtected(addr + 1, networkTickRate);
                }
            }
            else
            {
                foreach (var addr in addrs)
                    Process.RestoreMemory(addr);
            }
        }

        private void PatchToggleFrontend(bool in_isEnabled)
        {
            // Patch instruction operand to force return value.
            Process.WriteProtected(Process.ToASLR(0x14136274D), Convert.ToByte(in_isEnabled));
        }

        private void PatchToggleNavigationPoints(bool in_isEnabled)
        {
            if (in_isEnabled)
            {
                // Restore original opcode.
                Process.RestoreMemory(Process.ToASLR(0x141A589FE));

                // Restore original fixed scale.
                Process.RestoreMemory(Process.ToASLR(0x141A58A18));
            }
            else
            {
                // Force condition to use fixed scale.
                Process.WriteProtected<byte>(Process.ToASLR(0x141A589FE), 0xEB, true);

                // Set fixed scale to zero.
                Process.WriteProtected(Process.ToASLR(0x141A58A18), 0.0f, true);
            }
        }

        // Research from Exuberant
        private void PatchToggleThirdPersonCamera(bool in_isEnabled)
        {
            if (in_isEnabled)
            {
                // Remove condition to force third-person camera.
                Process.WriteNop(Process.ToASLR(0x1406E6138), 2);
            }
            else
            {
                // Restore original instruction.
                Process.RestoreMemory(Process.ToASLR(0x1406E6138));
            }
        }

        public void InstallPatches()
        {
            try
            {
                // Prevent applying patches during boot sequence.
                if (string.IsNullOrEmpty(Map))
                    return;

                PatchApplyCustomFOVToVehicles(App.Settings.IsApplyCustomFOVToVehicles);
                PatchCrosshairScaleMode((ECrosshairScaleMode)App.Settings.CrosshairScaleMode);
                PatchDynamicAspectRatio(App.Settings.IsDynamicAspectRatio);
                PatchNetworkIntegrity(FPS > 60);
                PatchToggleFrontend(App.Settings.IsToggleFrontend);
                PatchToggleNavigationPoints(App.Settings.IsToggleNavigationPoints);
                PatchToggleThirdPersonCamera(App.Settings.IsToggleThirdPersonCamera);

                FPS = App.Settings.FPS;
                FOV = App.Settings.FOV;
                IsSmallerCrosshairScale = App.Settings.IsToggleSmallerCrosshairScale;
                IsWorldSpaceViewModel = !App.Settings.IsToggleWorldSpaceViewModel;

                DrawDistanceScalar = App.Settings.DrawDistanceScalar / 100.0f;
                ObjectDetailScalar = App.Settings.ObjectDetailScalar / 100.0f;
                BSPGeometryDrawDistanceScalar = App.Settings.BSPGeometryDrawDistanceScalar / 100.0f;
                EffectDrawDistanceScalar = App.Settings.EffectDrawDistanceScalar / 100.0f;
                ParticleDrawDistanceScalar = App.Settings.ParticleDrawDistanceScalar / 100.0f;
                DecoratorDrawDistanceScalar = App.Settings.DecoratorDrawDistanceScalar / 100.0f;
                IsFog = App.Settings.IsToggleFog;
                IsWeather = App.Settings.IsToggleWeather;
                IsRagdoll = App.Settings.IsToggleRagdoll;
            }
            catch (Exception out_ex)
            {
                LoggerService.Error($"An unhandled exception occurred whilst installing patches. Ignoring...\n{out_ex}\n");
            }
        }

        public void UninstallPatches()
        {
            try
            {
                /* FIXME: this will only work once, not a big
                          deal for its current use case though. */
                _isUpdating = false;

                PatchApplyCustomFOVToVehicles(false);
                PatchCrosshairScaleMode(ECrosshairScaleMode.Off);
                PatchDynamicAspectRatio(false);
                PatchNetworkIntegrity(false);
                PatchToggleFrontend(true);
                PatchToggleNavigationPoints(true);
                PatchToggleThirdPersonCamera(false);

                FPS = 60;
                FOV = 78;
                IsSmallerCrosshairScale = false;
                IsWorldSpaceViewModel = false;

                DrawDistanceScalar = 1.0f;
                ObjectDetailScalar = 1.0f;
                BSPGeometryDrawDistanceScalar = 1.0f;
                EffectDrawDistanceScalar = 1.0f;
                ParticleDrawDistanceScalar = 1.0f;
                DecoratorDrawDistanceScalar = 1.0f;
                IsFog = true;
                IsWeather = true;
                IsRagdoll = true;
            }
            catch (Exception out_ex)
            {
                LoggerService.Error($"An unhandled exception occurred whilst uninstalling patches. Ignoring...\n{out_ex}\n");
            }
        }

        public void Dispose()
        {
            _isUpdating = false;
        }
    }
}
